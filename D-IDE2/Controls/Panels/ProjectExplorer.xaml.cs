using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using D_IDE.Core;
using System.Windows.Forms;
using System.IO;
using D_IDE.Dialogs;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für ProjectExplorer.xaml
	/// </summary>
	public partial class ProjectExplorer : AvalonDock.DockableContent
	{
		#region Properties
		System.Windows.Forms.TreeView MainTree = new System.Windows.Forms.TreeView();
		static readonly ImageList TreeIcons = new ImageList();

		bool IsCut = false;
		TreeNode CutCopyNode =null;
		#endregion

		public ProjectExplorer()
		{
			InitializeComponent();

			#region Manual WinForms Code
			winFormsHost.Child = MainTree;

			MainTree.ImageList = TreeIcons;
			MainTree.StateImageList = TreeIcons;

			MainTree.BeforeExpand += delegate(object sender, TreeViewCancelEventArgs e)
			{
				if (e.Node is DirectoryNode)
					e.Node.ImageKey = e.Node.SelectedImageKey = "dir_open";
			};

			MainTree.BeforeCollapse += delegate(object sender, TreeViewCancelEventArgs e)
			{
				if (e.Node is DirectoryNode)
					e.Node.ImageKey = e.Node.SelectedImageKey = "dir";
			};

			MainTree.MouseClick += new System.Windows.Forms.MouseEventHandler(MainTree_MouseClick);

			MainTree.AllowDrop = true;
			MainTree.ItemDrag += new ItemDragEventHandler(MainTree_ItemDrag);
			MainTree.DragOver += new System.Windows.Forms.DragEventHandler(MainTree_DragOver);
			MainTree.DragDrop += new System.Windows.Forms.DragEventHandler(MainTree_DragDrop);

			MainTree.BorderStyle = BorderStyle.None;
			MainTree.HideSelection = false;
			#endregion
		}

		#region Drag'n'Drop
		void MainTree_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			bool LastState = IsCut;

			if (e.Effect!=System.Windows.Forms.DragDropEffects.None && e.Data.GetDataPresent(typeof(TreeNode)))
			{
				var n = e.Data.GetData(typeof(TreeNode)) as TreeNode;
				IsCut = e.Effect == System.Windows.Forms.DragDropEffects.Move;

				DoPaste(n, MainTree.GetNodeAt(new System.Drawing.Point(e.X, e.Y)));
			}

			IsCut = LastState;
		}

		void MainTree_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
		{
			var n = e.Data.GetData(e.Data.GetFormats()[0]) as TreeNode;

			if (n!=null)
			{
				if (IsDropAllowed(n, MainTree.GetNodeAt(new System.Drawing.Point(e.X, e.Y))))
					if ((e.KeyState & 8) == 8) // If ctrl got pressed
						e.Effect = System.Windows.Forms.DragDropEffects.Copy;
					else
						e.Effect = System.Windows.Forms.DragDropEffects.Move; // Move file/dir by default
				else 
					e.Effect = System.Windows.Forms.DragDropEffects.None;
			}
		}

		void MainTree_ItemDrag(object sender, ItemDragEventArgs e)
		{
			var n = e.Item as TreeNode;
			if (n!=null)
			{
				MainTree.SelectedNode = n;

				if (e.Button == MouseButtons.Left && !(e.Item is SolutionNode))
					MainTree.DoDragDrop(new System.Windows.Forms.DataObject( n as object), System.Windows.Forms.DragDropEffects.All);
			}
		}
		#endregion

		#region Node copying & moving
		public static bool IsDropAllowed(TreeNode src, TreeNode dropNode)
		{
			// A directory node can be dropped on an other directory node or a project node
			if (src is DirectoryNode &&
				src != dropNode &&(
				(dropNode is DirectoryNode && 
					!(dropNode as DirectoryNode).RelativePath // Ensure that the drop node is no subdirectory of our source node
						.Contains((src as DirectoryNode).RelativePath)) || // e.g. src - FolderA, dropNode - FolderA/FolderB/FolderC  ->> dropNode path contains src path ->> return false;
				(dropNode is ProjectNode && 
					src.Parent!=dropNode) // Ensure that the directory's root is not the original
				))
				return true;

			if (src is FileNode &&
				(dropNode is DirectoryNode || dropNode is ProjectNode) &&
				src.Parent!=dropNode.Parent) // Ensure the file gets moved either to an other directory or project
				return true;

			if (src is ProjectNode &&
				dropNode is SolutionNode &&
				src.Parent != dropNode)
				return true;

			return false;
		}

		public void DoPaste(TreeNode src, TreeNode dropNode)
		{
			if (src is DirectoryNode)
			{
				var src_dn = src as DirectoryNode;
				var src_path = src_dn.RelativePath;
				var src_prj = src_dn.ParentProjectNode.Project;

				var dest_path = "";
				Project dest_prj = null;

				if (dropNode is ProjectNode)
					dest_prj = (dropNode as ProjectNode).Project;
				else if (dropNode is DirectoryNode)
				{
					dest_path = (dropNode as DirectoryNode).RelativePath;
					dest_prj = (dropNode as DirectoryNode).ParentProjectNode.Project;
				}

				if (dest_prj != null)
					if (IsCut) IDEManager.FileManagement.MoveDirectory(src_prj, src_path, dest_prj, dest_path);
					else IDEManager.FileManagement.CopyDirectory(src_prj,src_path,dest_prj,dest_path);
			}

			else if (src is FileNode)
			{
				var src_fn = src as FileNode;
				var src_path = src_fn.FileName;
				var src_prj = src_fn.ParentProjectNode.Project;

				var dest_path = "";
				Project dest_prj = null;

				if (dropNode is ProjectNode)
					dest_prj = (dropNode as ProjectNode).Project;
				else if (dropNode is DirectoryNode)
				{
					dest_path = (dropNode as DirectoryNode).RelativePath;
					dest_prj = (dropNode as DirectoryNode).ParentProjectNode.Project;
				}

				if (dest_prj != null)
					if (IsCut) IDEManager.FileManagement.MoveFile(src_prj, src_path, dest_prj, dest_path);
					else IDEManager.FileManagement.CopyFile(src_prj, src_path, dest_prj, dest_path);
			}

			else if (src is ProjectNode && dropNode is SolutionNode && src.Parent!=dropNode)
				IDEManager.ProjectManagement.ReassignProject((src as ProjectNode).Project,(dropNode as SolutionNode).Solution);
		}

		void AddCutCopyPasteButtons(ContextMenuStrip cm, TreeNode node, bool CutAllowed, bool CopyAllowed, bool PasteAllowed)
		{
			if (CutAllowed || CopyAllowed || (PasteAllowed && IsDropAllowed(CutCopyNode, node)))
				cm.Items.Add(new ToolStripSeparator());


			if (CutAllowed)
				cm.Items.Add("Cut", CommonIcons.Icons_16x16_CutIcon, delegate(Object o, EventArgs _e)
				{
					IsCut = true;
					CutCopyNode = node;
				});

			if (CopyAllowed)
				cm.Items.Add("Copy", CommonIcons.Icons_16x16_CopyIcon, delegate(Object o, EventArgs _e)
				{
					IsCut = false;
					CutCopyNode = node;
				});

			if (PasteAllowed && IsDropAllowed(CutCopyNode, node))
				cm.Items.Add("Paste", CommonIcons.Icons_16x16_PasteIcon, delegate(Object o, EventArgs _e)
				{
					DoPaste(CutCopyNode, node);
				});
		}
		#endregion
		/// <summary>
		/// Handles context menu creation, contains click event handlers
		/// </summary>
		void MainTree_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				// Get hovered node
				var n = MainTree.GetNodeAt(e.Location);
				if (n == null) return;
				MainTree.SelectedNode = n;

				var cm = new ContextMenuStrip();
				// Set node tag to our node
				cm.Tag = n;

				#region Build context menu

				#region File Node
				if (n is FileNode)
				{
					var fn=n as FileNode;
					Project prj = fn.ParentProjectNode.Project;

					cm.Items.Add("Open", CommonIcons.open16,delegate(Object o, EventArgs _e)
					{
						IDEManager.EditingManagement.OpenFile(prj,fn.FileName);
					});

					cm.Items.Add(new ToolStripSeparator());

					AddCutCopyPasteButtons(cm, n, true, true, false);

					cm.Items.Add("Exlude", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.ExludeFileFromProject(prj, fn.FileName);
					});

					cm.Items.Add("Delete", CommonIcons.delete16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.RemoveFileFromProject(prj, fn.FileName);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer",System.IO.Path.GetDirectoryName( fn.FileName));
					});
				}
				#endregion

				#region Directory Node
				else if (n is DirectoryNode)
				{
					var dn = n as DirectoryNode;

					cm.Items.Add("Add File", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.AddNewSourceToProject(dn.ParentProjectNode.Project, dn.RelativePath);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Exclude", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.ExcludeDirectoryFromProject(
							dn.ParentProjectNode.Project, dn.RelativePath);
					});

					cm.Items.Add("Delete", CommonIcons.delete16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.RemoveDirectoryFromProject(dn.ParentProjectNode.Project, dn.RelativePath);
					});

					AddCutCopyPasteButtons(cm, n, true, true, true);

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer", dn.AbsolutePath);
					});
				}
				#endregion

				#region Solution Node
				else if (n is SolutionNode)
				{
					var sln = (n as SolutionNode).Solution;

					cm.Items.Add("Build", CommonIcons.Icons_16x16_BuildCurrentSelectedProject, delegate(Object o, EventArgs _e)
					{
						sln.Build();
					});

					cm.Items.Add("Rebuild",null, delegate(Object o, EventArgs _e)
					{
						sln.Rebuild();
					});

					cm.Items.Add("CleanUp", null, delegate(Object o, EventArgs _e)
					{
						sln.CleanUpOutput();
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Add Project", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{
						var pdlg = new NewProjectDlg(NewProjectDlg.DialogMode.Add)
							{
								ProjectDir=sln.BaseDir
							};

						if (pdlg.ShowDialog().Value)
							IDEManager.ProjectManagement.AddNewProjectToSolution(
								sln,
								pdlg.SelectedLanguageBinding,
								pdlg.SelectedProjectType,
								pdlg.ProjectName,
								pdlg.ProjectDir);
					});

					cm.Items.Add("Project Dependencies", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.ProjectManagement.ShowProjectDependenciesDialog(sln);
					});

					AddCutCopyPasteButtons(cm, n, false, false, true);

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer",System.IO.Path.GetDirectoryName(sln.FileName));
					});
				}
				#endregion

				#region Project Node
				else if (n is ProjectNode)
				{
					var prj = (n as ProjectNode).Project;

					cm.Items.Add("Build", CommonIcons.Icons_16x16_BuildCurrentSelectedProject, delegate(Object o, EventArgs _e)
					{
						IDEManager.BuildManagement.Build(prj,true);
					});

					cm.Items.Add("Rebuild", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.BuildManagement.Build(prj,false);
					});

					cm.Items.Add("CleanUp", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.BuildManagement.CleanUpOutput(prj);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Add File", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.AddNewSourceToProject(prj,".");
					});

					cm.Items.Add("Add Existing", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.AddExistingSourceToProject(prj,"");
					});

					cm.Items.Add("Project Dependencies", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.ProjectManagement.ShowProjectDependenciesDialog(prj);
					});

					AddCutCopyPasteButtons(cm, n, true, false, true);

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer",prj.BaseDirectory);
					});

					cm.Items.Add("Properties", CommonIcons.properties16, delegate(Object o, EventArgs _e)
					{
						IDEManager.ProjectManagement.ShowProjectPropertiesDialog(prj);
					});
				}
				#endregion

				#endregion

				// Show it
				cm.Show(MainTree, e.Location);
			}
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			Update();
		}

		public void Update()
		{
			MainTree.BeginUpdate();
			SetupTreeIcons();

			MainTree.Nodes.Clear();

			if (IDEManager.CurrentSolution != null)
				MainTree.Nodes.Add(new SolutionNode(IDEManager.CurrentSolution));

			MainTree.EndUpdate();
		}

		#region Icons
		public static void SetupTreeIcons()
		{
			TreeIcons.Images.Clear();

			TreeIcons.Images.Add("solution", CommonIcons.Icons_16x16_CombineIcon);
			TreeIcons.Images.Add("dir", CommonIcons.dir);
			TreeIcons.Images.Add("dir_open", CommonIcons.dir_open);

			foreach (var lang in LanguageLoader.Bindings)
			{
				if (lang.ProjectsSupported)
					foreach (var pt in lang.ProjectTemplates)
						if (pt.SmallImage != null && pt.Extensions != null)
							foreach (var ext in pt.Extensions)
								Util.AddGDIImageToImageList(TreeIcons, ext, pt.SmallImage);

				foreach (var mt in lang.ModuleTemplates)
					if (mt.SmallImage != null && mt.Extensions != null)
						foreach (var ext in mt.Extensions)
							Util.AddGDIImageToImageList(TreeIcons, ext, mt.SmallImage);
			}
		}

		static string GetFileIconKey(string FileName)
		{
			var ext = System.IO.Path.GetExtension(FileName);

			if (!TreeIcons.Images.ContainsKey(ext))
			{
				TreeIcons.Images.Add(ext, Win32.GetIcon(FileName, true));
			}

			return ext;
		}
		#endregion

		#region Node classes
		public class SolutionNode : TreeNode
		{
			public Solution Solution;

			public SolutionNode() { }
			public SolutionNode(Solution solution)
			{
				this.Solution = solution;
				Text = "Solution "+solution.Name;

				SelectedImageKey= ImageKey = "solution";

				UpdateChildren();
			}

			public void UpdateChildren()
			{
				Nodes.Clear();

				foreach (var p in Solution)
					Nodes.Add(new ProjectNode(p));
			}
		}

		public class ProjectNode : TreeNode
		{
			public Project Project;

			public ProjectNode() { }
			public ProjectNode(Project prj):base(prj.Name)
			{
				Project = prj;
				SelectedImageKey= ImageKey = GetFileIconKey(prj.FileName);

				UpdateChildren();
			}

			public void UpdateChildren()
			{
				Nodes.Clear();

				var baseDir = System.IO.Path.GetDirectoryName(Project.FileName);

				// First add observed directories
				foreach (var d in Project.SubDirectories)
					DirectoryNode.CheckIfSubDirExists(this,d.StartsWith(baseDir)?d.Substring(baseDir.Length):d);

				// Then add modules/files
				foreach (var f in Project.Files)
				{
					// Create directory node
					var fDir=System.IO.Path.GetDirectoryName(f);
					var dirNode = DirectoryNode.CheckIfSubDirExists(this,fDir);

					var fnode = new FileNode() { FileName = f };
					fnode.SelectedImageKey = fnode.ImageKey = GetFileIconKey(f);

					dirNode.Nodes.Add(fnode);
				}
			}
		}

		public class DirectoryNode : TreeNode
		{
			#region Properties
			public string DirectoryName
			{
				get { return Text; }
				set { Text = value; }
			}
			/// <summary>
			/// The path is relative to the project's base dir
			/// </summary>
			public string RelativePath
			{
				get
				{
					string ret = "";

					var n = this;
					while (n!=null)
					{
						ret = n.DirectoryName + "\\" + ret;
						n = n.Parent as DirectoryNode;
					}

					return ret.Trim(' ','\\');
				}
			}
			public ProjectNode ParentProjectNode
			{
				get
				{
					var curNode = Parent;

					while (curNode != null)
					{
						if (curNode is ProjectNode)
							return curNode as ProjectNode;
						curNode = curNode.Parent;
					}
					return null;
				}
			}

			public string AbsolutePath
			{
				get
				{
					return ParentProjectNode.Project.ToAbsoluteFileName(RelativePath);
				}
			}
			#endregion

			/// <summary>
			/// Check if a node that represents the Directory called relativeDir or create that node recursively
			/// </summary>
			/// <param name="relativeDir"></param>
			/// <returns></returns>
			public static TreeNode CheckIfSubDirExists(TreeNode ThisNode,string relativeDir)
			{
				if (String.IsNullOrEmpty(relativeDir) || relativeDir == ".")
					return ThisNode;

				var pathparts=relativeDir.Split('\\');

				int i = 0;
				var CurNode = ThisNode;

				// move deeper along the path
				i++;
				while (i < pathparts.Length && CurNode!=null)
				{
					// A bit buggy but should work in many situations
					if(pathparts[i]=="..")
					{
						CurNode=CurNode.Parent;
						i++;
						continue;
					}

					// scan for existing subdirectories
					bool matchFound = false;
					foreach (var n in CurNode.Nodes)
						if (n is DirectoryNode)
						{
							var dn = n as DirectoryNode;
							if (dn.DirectoryName == pathparts[i])
							{
								CurNode = dn;
								matchFound = true;
								break;
							}
						}

					// if no match was found, create a node
					if (!matchFound)
						CurNode.Nodes.Add(CurNode = new DirectoryNode() { DirectoryName = pathparts[i] });

					i++;
				}

				return CurNode;
			}

			public DirectoryNode()
			{
				ImageKey =SelectedImageKey = "dir";
			}
		}

		public class FileNode : TreeNode
		{
			public string FileName{get;set;}

			public ProjectNode ParentProjectNode
			{
				get
				{
					var curNode = Parent;

					while (curNode != null)
					{
						if (curNode is ProjectNode)
							return curNode as ProjectNode;
						curNode = curNode.Parent;
					}
					return null;
				}
			}
		}
		#endregion
	}
}
