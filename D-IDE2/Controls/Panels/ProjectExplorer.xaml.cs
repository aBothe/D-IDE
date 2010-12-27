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
		#endregion

		public ProjectExplorer()
		{
			InitializeComponent();

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

			MainTree.BorderStyle = BorderStyle.None;
		}

		/// <summary>
		/// Handles context menu
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MainTree_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				// Get hovered node
				var n = MainTree.GetNodeAt(e.Location);
				if (n == null) return;
				
				// Build context menu
				var cm = new ContextMenuStrip();
				// Set node tag to our node
				cm.Tag = n;

				#region At first add node specific items
				if (n is FileNode)
				{
					var fn=n as FileNode;
					IProject prj = fn.ParentProjectNode.Project;

					cm.Items.Add("Open", CommonIcons.open16,delegate(Object o, EventArgs _e)
					{
						IDEManager.OpenFile(prj,fn.FileName);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Exlude", CommonIcons.open16, delegate(Object o, EventArgs _e)
					{
						prj.Remove(fn.FileName);
					});
				}
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
							IDEManager.AddNewProjectToSolution(
								sln,
								pdlg.SelectedLanguageBinding,
								pdlg.SelectedProjectType,
								pdlg.ProjectName,
								pdlg.ProjectDir);
					});
				}
				else if (n is ProjectNode)
				{
					var prj = (n as ProjectNode).Project;

					cm.Items.Add("Build", CommonIcons.Icons_16x16_BuildCurrentSelectedProject, delegate(Object o, EventArgs _e)
					{
						prj.Build();
					});

					cm.Items.Add("Rebuild", null, delegate(Object o, EventArgs _e)
					{
						prj.Rebuild();
					});

					cm.Items.Add("CleanUp", null, delegate(Object o, EventArgs _e)
					{
						prj.CleanUpOutput();
					});

					cm.Items.Add("Project Dependencies", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.ShowProjectDepsDialogue(prj);
					});
				}

				if (n is ProjectNode || n is DirectoryNode)
				{
					if (cm.Items.Count > 0)
						cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Add File", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{

					}
				}
				#endregion

				// Then add general items
				if (cm.Items.Count > 0)
					cm.Items.Add(new ToolStripSeparator());

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
					foreach (var pt in lang.ProjectTypes)
						if (pt.SmallImage != null && pt.Extensions != null)
							foreach (var ext in pt.Extensions)
								Util.AddGDIImageToImageList(TreeIcons, ext, pt.SmallImage);

				foreach (var mt in lang.ModuleTypes)
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
			public IProject Project;

			public ProjectNode() { }
			public ProjectNode(IProject prj):base(prj.Name)
			{
				Project = prj;

				var pt = Project.ProjectType;
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

					var mod=Project[f];
					FileNode fnode = null;
					// If it's managed, add a module node
					// otherwise create filenode only
					if (mod != null)
						fnode = new ModuleNode() { Module = mod };
					else fnode = new FileNode() { FileName = f };

					fnode.SelectedImageKey = fnode.ImageKey = GetFileIconKey(f);
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
					string ret = DirectoryName;

					var n = this;
					while (n!=null)
					{
						ret = n.DirectoryName + "\\" + ret;
						n = n.Parent as DirectoryNode;
					}

					return ret;
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
					// get project node
					var prj = ParentProjectNode.Project;
					return System.IO.Path.GetDirectoryName(prj.FileName)+"\\"+RelativePath;
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

		public class ModuleNode : FileNode
		{
			public IModule Module;
			public new string FileName
			{
				get { return Module.FileName; }
			}
		}
		#endregion
	}
}
