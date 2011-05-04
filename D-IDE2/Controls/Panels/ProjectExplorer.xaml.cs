using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
		public System.Windows.Forms.TreeView MainTree = new System.Windows.Forms.TreeView();
		static readonly ImageList TreeIcons = new ImageList();

		bool IsAddingDirectory = false;

		bool IsCut = false;
		PrjExplorerNode CutCopyNode = null;
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
			MainTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MainTree_MouseDoubleClick);

			MainTree.AllowDrop = true;
			MainTree.ItemDrag += new ItemDragEventHandler(MainTree_ItemDrag);
			MainTree.DragOver += new System.Windows.Forms.DragEventHandler(MainTree_DragOver);
			MainTree.DragDrop += new System.Windows.Forms.DragEventHandler(MainTree_DragDrop);

			MainTree.BorderStyle = BorderStyle.None;
			MainTree.HideSelection = false;
			MainTree.TreeViewNodeSorter = new PrjExplorerNodeSorter();

			MainTree.KeyDown += new System.Windows.Forms.KeyEventHandler(MainTree_KeyDown);
			MainTree.LabelEdit = true;
			MainTree.BeforeLabelEdit += new NodeLabelEditEventHandler(MainTree_BeforeLabelEdit);
			MainTree.AfterLabelEdit += new NodeLabelEditEventHandler(MainTree_AfterLabelEdit);
			#endregion
		}

		void MainTree_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			var n = e.Node as PrjExplorerNode;
			if (_IsRefreshing || (n is ProjectNode &&(n as ProjectNode).IsUnloaded))
				e.CancelEdit = true;
		}

		void MainTree_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F2)
			{
				if (MainTree.SelectedNode != null)
					MainTree.SelectedNode.BeginEdit();
			}
		}

		/// <summary>
		/// Renaming
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MainTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			if (!IsAddingDirectory && e.Label == null)
				return;

			if (e.Node is SolutionNode)
				e.CancelEdit = !IDEManager.ProjectManagement.Rename((e.Node as SolutionNode).Solution, e.Label);
			else if (e.Node is ProjectNode)
				e.CancelEdit = (e.Node as ProjectNode).IsUnloaded || !IDEManager.ProjectManagement.Rename((e.Node as ProjectNode).Project, e.Label);
			else if (e.Node is DirectoryNode)
			{
				var dn = e.Node as DirectoryNode;
				if (IsAddingDirectory)
				{
					IsAddingDirectory = false;
					e.CancelEdit = !IDEManager.FileManagement.AddNewDirectoryToProject(dn.ParentProjectNode.Project, dn.RelativePath, e.Label);
					if (e.CancelEdit) // We don't want to keep our empty directory node
					{
						dn.Remove();
						return;
					}
				}
				else
					e.CancelEdit = !IDEManager.FileManagement.RenameDirectory(dn.ParentProjectNode.Project, dn.RelativePath, e.Label);
				
				if (!e.CancelEdit) // If successful, apply the new (purified) name
					dn.Text = Util.PurifyDirName(e.Label);
			}
			else if (e.Node is FileNode)
			{
				var n = e.Node as FileNode;
				e.CancelEdit = !IDEManager.FileManagement.RenameFile(n.ParentProjectNode.Project, n.AbsolutePath, e.Label);
				
				if (!e.CancelEdit) 
					n.Text = Util.PurifyFileName(e.Label);
			}

			if (!e.CancelEdit)
				Update();
		}

		void MainTree_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			// Get hovered node
			var n = MainTree.GetNodeAt(e.Location);
			if (n == null) return;
			MainTree.SelectedNode = n;

			if (n is FileNode)
				IDEManager.EditingManagement.OpenFile((n as FileNode).AbsolutePath);
			
			// Set to start project
			else if (n is ProjectNode)
			{
				var pn=n as ProjectNode;
				if(pn.IsUnloaded)
					return;
				var sln = pn.Project.Solution;
				sln.StartProject = pn.Project;
				sln.Save();

				Update();
			}
		}

		#region Drag'n'Drop
		void MainTree_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			if (e.Effect == DragDropEffects.None) return;

			var targetNode = new DnDData(MainTree.GetNodeAt(MainTree.PointToClient(new System.Drawing.Point(e.X, e.Y))) as PrjExplorerNode);
			MainTree.SelectedNode = targetNode.Node;
			bool LastState = IsCut;
			IsCut = e.Effect == System.Windows.Forms.DragDropEffects.Move;

			if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
				foreach (var file in e.Data.GetData(DataFormats.FileDrop) as string[])
					DoPaste(file,targetNode);
			else if (e.Data.GetDataPresent(typeof(DnDData)))
				DoPaste(e.Data.GetData(typeof(DnDData)) as DnDData, targetNode);
			IsCut = LastState;
		}

		void MainTree_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
		{
			e.Effect = System.Windows.Forms.DragDropEffects.None;
			var targetNode = new DnDData( MainTree.GetNodeAt(MainTree.PointToClient(new System.Drawing.Point(e.X, e.Y))) as PrjExplorerNode);
			MainTree.SelectedNode = targetNode.Node;

			if (targetNode.Node != null)
				targetNode.Node.Expand();

			if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
			{
				var files = e.Data.GetData(DataFormats.FileDrop) as string[];

				// Force all files to be droppable
				foreach (var file in files)
					if (!IsDropAllowed(file, targetNode.Node))	
						return;

				e.Effect = System.Windows.Forms.DragDropEffects.Copy;
			}
			else if (e.Data.GetDataPresent(typeof(DnDData)))
			{
				var d = e.Data.GetData(typeof(DnDData)) as DnDData;
				if(!IsDropAllowed(d.Path,targetNode.Node))
					return;

				// If src prj and dest prj are equal, set default action to 'move'
				bool Move=d.Project==targetNode.Project;
				
				// If ctrl is pressed anyway, turn action
				if((e.KeyState & 8) == 8)	Move=!Move;

				if (!Move) // If ctrl has been pressed
					e.Effect = System.Windows.Forms.DragDropEffects.Copy;
				else
					e.Effect = System.Windows.Forms.DragDropEffects.Move; // Move file/dir by default
			}
		}

		void MainTree_ItemDrag(object sender, ItemDragEventArgs e)
		{
			var n = e.Item as PrjExplorerNode;
			if (n != null)
			{
				MainTree.SelectedNode = n;

				if (e.Button == MouseButtons.Left && !(e.Item is SolutionNode))
					MainTree.DoDragDrop(new System.Windows.Forms.DataObject(new DnDData(n)), System.Windows.Forms.DragDropEffects.All);
			}
		}

		public class DnDData
		{
			public DnDData(PrjExplorerNode n) { Node = n; }
			public readonly PrjExplorerNode Node;
			public string Path { get { return Node.AbsolutePath; } }

			public bool IsSln { get { return Node is SolutionNode; } }
			public bool IsPrj { get { return Node is ProjectNode; } }
			public bool IsDir { get { return Node is DirectoryNode; } }
			public bool IsFile { get { return Node is FileNode; } }

			public Project Project
			{
				get
				{
					if (IsPrj) return (Node as ProjectNode).Project;
					if (IsDir) return (Node as DirectoryNode).ParentProjectNode.Project;
					if (IsFile) return (Node as FileNode).ParentProjectNode.Project;
					return null;
				}
			}

			public Solution Solution
			{
				get
				{
					if (IsSln) return (Node as SolutionNode).Solution;
					if (Project != null) return Project.Solution;
					return null;
				}
			}
		}
		#endregion

		#region Node copying & moving
		public static bool IsDropAllowed(string file, PrjExplorerNode dropNode)
		{
			if (string.IsNullOrEmpty(file))	return false;

			if (file.EndsWith(Solution.SolutionExtension))
				return true;

			if (dropNode == null) return false;

			bool isPrj = false;
			AbstractLanguageBinding.SearchBinding(file,out isPrj);

			if (isPrj && dropNode is SolutionNode && !(dropNode as SolutionNode).Solution.ContainsProject(file))
				return true;
			else if (isPrj) // Ignore already existing projects
				return false;

			if (dropNode is SolutionNode)
				return false;

			/*
			 * After checking for solution or project extension, check if 'file' is file or directory
			 */
			bool IsDir = Directory.Exists(file);
			var fileDir = Path.GetDirectoryName(file);

			var dropFile=dropNode.AbsolutePath;
			var dropDir = dropFile;
			if (!(dropNode is DirectoryNode)) 
				dropDir = Path.GetDirectoryName(dropFile);

			if (IsDir && file!=dropDir && !dropDir.Contains(file))
				return true;
			else if (fileDir!=dropDir) // Ensure the file gets moved either to an other directory or project
				return true;

			return false;
		}

		public void DoPaste(DnDData data, DnDData dropNode)
		{
			// 'Pre-expand' our drop node - when the tree gets updated
			if (!dropNode.IsPrj)
				_ExpandedNodes.Add(dropNode.Node.FullPath);

			if (data.IsDir)
			{
				var src_path = (data.Node as DirectoryNode).RelativePath;
				var src_prj = data.Project;

				var dest_path = dropNode.IsDir?(dropNode.Node as DirectoryNode).RelativePath: "";
				if (dropNode.IsFile) dest_path = Path.GetDirectoryName(dropNode.Path);
				var dest_prj = dropNode.Project;
				dest_path = dest_prj.ToRelativeFileName(dest_path);

				if (dest_prj != null)
					if (IsCut) IDEManager.FileManagement.MoveDirectory(src_prj, src_path, dest_prj, dest_path);
					else IDEManager.FileManagement.CopyDirectory(src_prj, src_path, dest_prj, dest_path);
			}

			else if (data.IsFile)
			{
				var src_path = (data.Node as FileNode).RelativeFilePath;
				var src_prj = data.Project;

				var dest_path = dropNode.IsDir ? (dropNode.Node as DirectoryNode).RelativePath : "";
				if (dropNode.IsFile) dest_path = Path.GetDirectoryName(dropNode.Path);
				var dest_prj = dropNode.Project;

				dest_path = dest_prj.ToRelativeFileName(dest_path);

				if (dest_prj != null)
					if (IsCut) IDEManager.FileManagement.MoveFile(src_prj, src_path, dest_prj, dest_path);
					else IDEManager.FileManagement.CopyFile(src_prj, src_path, dest_prj, dest_path);
			}
		}

		/// <summary>
		/// Note: We will be only allowed to COPY files, not move them
		/// </summary>
		/// <param name="file"></param>
		/// <param name="dropNode"></param>
		public void DoPaste(string file,DnDData dropNode)
		{
			// Solution
			if (file.EndsWith(Solution.SolutionExtension))
			{
				IDEManager.EditingManagement.OpenFile(file);
				return;
			}

			// Project
			bool isPrj = false;
			AbstractLanguageBinding.SearchBinding(file, out isPrj);

			if (isPrj && dropNode.IsSln)
				IDEManager.ProjectManagement.AddExistingProjectToSolution(dropNode.Solution, file);
			else if (isPrj) // Ignore already existing projects
				return;

			bool IsDir = Directory.Exists(file);
			var dropFile = dropNode.Path;

			// 'Pre-expand' our drop node - when the tree gets updated
			if(!dropNode.IsPrj)
				_ExpandedNodes.Add(dropNode.Node.FullPath);

			var dropDir = "";
			if (dropNode.IsDir)
				dropDir = (dropNode.Node as DirectoryNode).RelativePath;
			else if (dropNode.IsFile)
				dropDir = Path.GetDirectoryName( (dropNode.Node as FileNode).RelativeFilePath);

			if (IsDir)
					IDEManager.FileManagement.AddExistingDirectoryToProject(file, dropNode.Project, dropDir);
			else
					IDEManager.FileManagement.AddExistingSourceToProject(dropNode.Project, dropDir, file);
		}

		void AddCutCopyPasteButtons(ContextMenuStrip cm, PrjExplorerNode node, bool CutAllowed, bool CopyAllowed, bool PasteAllowed)
		{
			if (CutAllowed || CopyAllowed || (PasteAllowed && CutCopyNode!=null && IsDropAllowed(CutCopyNode.AbsolutePath, node)))
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

			if (PasteAllowed && CutCopyNode!=null && IsDropAllowed(CutCopyNode.AbsolutePath, node))
				cm.Items.Add("Paste", CommonIcons.Icons_16x16_PasteIcon, delegate(Object o, EventArgs _e)
				{
					DoPaste(CutCopyNode.AbsolutePath,new DnDData(node));
				});
		}
		#endregion

		#region Context Menus
		void MainTree_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				// Get hovered node
				var n = MainTree.GetNodeAt(e.Location);
				if (n == null) return;
				MainTree.SelectedNode = n;

				var cm = new System.Windows.Forms.ContextMenuStrip();
				// Set node tag to our node
				cm.Tag = n;

				#region Build context menu

				#region File Node
				if (n is FileNode)
				{
					var fn = n as FileNode;
					Project prj = fn.ParentProjectNode.Project;

					cm.Items.Add("Open", CommonIcons.open16, delegate(Object o, EventArgs _e)
					{
						IDEManager.EditingManagement.OpenFile(fn.AbsolutePath);
					});

					AddCutCopyPasteButtons(cm, fn, true, true, false);

					cm.Items.Add("Exlude", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.ExludeFileFromProject(prj, fn.RelativeFilePath);
					});

					cm.Items.Add("Delete", CommonIcons.delete16, delegate(Object o, EventArgs _e)
					{
						if(Util.ShowDeleteFileDialog(fn.FileName))
							IDEManager.FileManagement.RemoveFileFromProject(prj, fn.RelativeFilePath);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer",System.IO.Path.GetDirectoryName( fn.AbsolutePath));
					});
				}
				#endregion

				#region Directory Node
				else if (n is DirectoryNode)
				{
					var dn = n as DirectoryNode;

					var subMenu = new ToolStripMenuItem("Add");
					cm.Items.Add(subMenu);

					subMenu.DropDownItems.Add("File", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.AddNewSourceToProject(
							dn.ParentProjectNode.Project, dn.RelativePath);
					});

					subMenu.DropDownItems.Add("Directory", CommonIcons.dir, delegate(Object o, EventArgs _e)
					{
						IsAddingDirectory = true;
						var nd = new DirectoryNode("");
						dn.Nodes.Add(nd);
						dn.Expand();
						nd.BeginEdit();
					});

					subMenu.DropDownItems.Add("Existing File", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.AddExistingSourceToProject(
							dn.ParentProjectNode.Project, dn.RelativePath);
					});

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Exclude", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.FileManagement.ExcludeDirectoryFromProject(
							dn.ParentProjectNode.Project, dn.RelativePath);
					});

					cm.Items.Add("Delete", CommonIcons.delete16, delegate(Object o, EventArgs _e)
					{
						if (Util.ShowDeleteFileDialog(dn.DirectoryName))
						IDEManager.FileManagement.RemoveDirectoryFromProject(
							dn.ParentProjectNode.Project, dn.RelativePath);
					});

					AddCutCopyPasteButtons(cm, dn, true, true, true);

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
						IDEManager.BuildManagement.Build(sln, true);
					});

					cm.Items.Add("Rebuild", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.BuildManagement.Build(sln, false);
					});

					cm.Items.Add("CleanUp", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.BuildManagement.CleanUpOutput(sln);
					});

					cm.Items.Add(new ToolStripSeparator());

					var subMenu = new ToolStripMenuItem("Add", CommonIcons.addfile16);
					cm.Items.Add(subMenu);

					subMenu.DropDownItems.Add("New Project", CommonIcons.prj_16, delegate(Object o, EventArgs _e)
					{
						var pdlg = new NewProjectDlg(NewProjectDlg.DialogMode.Add) { ProjectDir = sln.BaseDirectory };

						if (pdlg.ShowDialog().Value)
							IDEManager.ProjectManagement.AddNewProjectToSolution(
							sln,
							pdlg.SelectedLanguageBinding,
							pdlg.SelectedProjectType,
							pdlg.ProjectName,
							pdlg.ProjectDir);
					});

					subMenu.DropDownItems.Add("Existing Project", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.ProjectManagement.AddExistingProjectToSolution(sln);
					});

					cm.Items.Add("Project Dependencies", null, delegate(Object o, EventArgs _e)
					{
						IDEManager.ProjectManagement.ShowProjectDependenciesDialog(sln);
					});

					AddCutCopyPasteButtons(cm, n as PrjExplorerNode, false, false, true);

					cm.Items.Add(new ToolStripSeparator());

					cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
					{
						System.Diagnostics.Process.Start("explorer", System.IO.Path.GetDirectoryName(sln.FileName));
					});
				}
				#endregion

				#region Project Node
				else if (n is ProjectNode)
				{
					var pn = n as ProjectNode;

					if (pn.IsUnloaded)
					{
						cm.Items.Add("Reload", CommonIcons.Refresh, delegate(Object o, EventArgs _e)
						{
							var prj =Project.LoadProjectFromFile(  pn.Solution, pn.Text);
							if (prj!=null)
									Update();
						});

						AddCutCopyPasteButtons(cm, pn, true, false, false);

						cm.Items.Add(new ToolStripSeparator());

						cm.Items.Add("Exclude", CommonIcons.delete16, delegate(Object o, EventArgs _e)
						{
							if(MessageBox.Show("Continue with excluding project?","Excluding project",MessageBoxButtons.YesNo,MessageBoxIcon.Asterisk,MessageBoxDefaultButton.Button2)==DialogResult.Yes)
								IDEManager.ProjectManagement.ExcludeProject(pn.Solution,pn.Text);
						});
					}
					else
					{

						var prj = pn.Project;

						cm.Items.Add("Build", CommonIcons.Icons_16x16_BuildCurrentSelectedProject, delegate(Object o, EventArgs _e)
						{
							IDEManager.BuildManagement.Build(prj, true);
						});

						cm.Items.Add("Rebuild", null, delegate(Object o, EventArgs _e)
						{
							IDEManager.BuildManagement.Build(prj, false);
						});

						cm.Items.Add("CleanUp", null, delegate(Object o, EventArgs _e)
						{
							IDEManager.BuildManagement.CleanUpOutput(prj);
						});

						cm.Items.Add(new ToolStripSeparator());

						var subMenu = new ToolStripMenuItem("Add");
						cm.Items.Add(subMenu);

						subMenu.DropDownItems.Add("File", CommonIcons.addfile16, delegate(Object o, EventArgs _e)
						{
							IDEManager.FileManagement.AddNewSourceToProject(prj, "");
						});

						subMenu.DropDownItems.Add("Directory", CommonIcons.dir, delegate(Object o, EventArgs _e)
						{
							IsAddingDirectory = true;
							var nd = new DirectoryNode("");
							n.Nodes.Add(nd);
							n.Expand();
							nd.BeginEdit();
						});

						subMenu.DropDownItems.Add("Existing File", null, delegate(Object o, EventArgs _e)
						{
							IDEManager.FileManagement.AddExistingSourceToProject(prj, "");
						});

						cm.Items.Add("Project Dependencies", null, delegate(Object o, EventArgs _e)
						{
							IDEManager.ProjectManagement.ShowProjectDependenciesDialog(prj);
						});

						AddCutCopyPasteButtons(cm, pn, true, false, true);

						cm.Items.Add(new ToolStripSeparator());

						cm.Items.Add("Exclude", CommonIcons.delete16, delegate(Object o, EventArgs _e)
						{
							if (MessageBox.Show("Continue with excluding project?", "Excluding project", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
							IDEManager.ProjectManagement.ExcludeProject(prj);
						});

						cm.Items.Add(new ToolStripSeparator());

						cm.Items.Add("Open File Path", null, delegate(Object o, EventArgs _e)
						{
							System.Diagnostics.Process.Start("explorer", prj.BaseDirectory);
						});

						cm.Items.Add("Properties", CommonIcons.properties16, delegate(Object o, EventArgs _e)
						{
							IDEManager.ProjectManagement.ShowProjectPropertiesDialog(prj);
						});
					}
				}
				#endregion

				#endregion

				// Show it
				cm.Show(MainTree, e.Location);
			}
		}
		#endregion

		private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Update();
		}

		bool _IsRefreshing= false;
		List<string> _ExpandedNodes = new List<string>();
		string _lastSelectedPath=null;
		public void Update()
		{
			_IsRefreshing = true;
			MainTree.BeginUpdate();
			
			SetupTreeIcons();

			if (MainTree.SelectedNode != null)
			{
				var n = MainTree.SelectedNode as PrjExplorerNode;
				_lastSelectedPath = n.AbsolutePath;
			}

			foreach (TreeNode n in MainTree.Nodes)
				_CheckForExpansionStates(n, ref _ExpandedNodes);

			MainTree.Nodes.Clear();

			if (IDEManager.CurrentSolution != null)
				MainTree.Nodes.Add(new SolutionNode(IDEManager.CurrentSolution));

			foreach (TreeNode n in MainTree.Nodes)
				_ApplyExpansionStates(n, _ExpandedNodes);

			MainTree.Sort();

			MainTree.EndUpdate();
			_ExpandedNodes.Clear();
			_IsRefreshing = false;
		}

		void _CheckForExpansionStates(TreeNode node, ref List<string> lst)
		{
			if ((node is ProjectNode && node.GetNodeCount(false) > 0) ? !node.IsExpanded : node.IsExpanded)
				lst.Add(node.FullPath);

			foreach (TreeNode n in node.Nodes)
				_CheckForExpansionStates(n, ref lst);
		}

		void _ApplyExpansionStates(TreeNode node, List<string> lst)
		{
			if (node is PrjExplorerNode && (node as PrjExplorerNode).AbsolutePath == _lastSelectedPath)
				MainTree.SelectedNode=node;

			// Expand project nodes by default
			if (node is ProjectNode ? !lst.Contains(node.FullPath) : lst.Contains(node.FullPath))
				node.Expand();

			foreach (TreeNode n in node.Nodes)
				_ApplyExpansionStates(n, lst);
		}

		#region Icons
		public static void SetupTreeIcons()
		{
			TreeIcons.Images.Clear();

			TreeIcons.Images.Add("solution", CommonIcons.solution16);
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
		public abstract class PrjExplorerNode : TreeNode
		{
			public abstract string AbsolutePath { get; }

			public bool IsExternalFile { get; protected set; }

			public PrjExplorerNode() {}
			public PrjExplorerNode(string text) : base(text) { }
		}

		public class SolutionNode : PrjExplorerNode
		{
			public Solution Solution;

			public SolutionNode() { SelectedImageKey = ImageKey = "solution"; }
			public SolutionNode(Solution solution)
			{
				this.Solution = solution;
				Text = solution.Name;

				SelectedImageKey = ImageKey = "solution";

				UpdateChildren();
			}

			public void UpdateChildren()
			{
				Nodes.Clear();

				foreach (var file in Solution.ProjectFiles)
				{
					var absFile = Solution.ToAbsoluteFileName(file);
					var p = Solution[file];
					ProjectNode pn=null;
					if (p != null)
					{
						pn = new ProjectNode(absFile, p.Name);
						pn.Tag = absFile;
						Nodes.Add(pn);
						// Add project node first before refreshing its children 
						// - the 'Project'-property requires an existing parent node, which is in our case 'this' node
						pn.UpdateChildren();

						if (Solution.StartProject == p)
							pn.NodeFont = new System.Drawing.Font("Arial",10f,System.Drawing.FontStyle.Bold);
					}
					else
					{
						// If a project is there but not found, create a 'disabled' project node
						pn = new ProjectNode(absFile,Path.GetFileName( file));
						pn.Tag = absFile;
						pn.IsUnloaded = true;
						Nodes.Add(pn);
					}
				}
				Expand();
			}

			public override string AbsolutePath
			{
				get { return Solution.FileName; }
			}
		}

		public class ProjectNode : PrjExplorerNode
		{
			public Solution Solution
			{
				get
				{
					if (Parent is SolutionNode)
						return (Parent as SolutionNode).Solution;
					return null;
				}
			}

			public Project Project
			{
				get
				{
					if(Solution!=null)
						return IsExternalFile?Solution[AbsolutePath] :Solution.ByName(Text);
					return null;
				}
			}

			bool _IsNotloaded = false;
			public bool IsUnloaded
			{
				get { return _IsNotloaded; }
				set
				{
					if (_IsNotloaded = value)
						ForeColor = System.Drawing.Color.Gray;
					else
						ForeColor = System.Drawing.Color.Black;
				}
			}

			public ProjectNode() { }
			public ProjectNode(string prjfile,string Name)
				: base(Name)
			{
				SelectedImageKey = ImageKey = GetFileIconKey(prjfile);
			}

			public void UpdateChildren()
			{
				Nodes.Clear();

				var baseDir = Path.GetDirectoryName(Project.FileName);

				// First add observed directories
				foreach (var d in Project.SubDirectories)
					DirectoryNode.CheckIfSubDirExists(this, d.StartsWith(baseDir) ? d.Substring(baseDir.Length) : d);

				// Then add modules/files
				foreach (var f in from m in Project.Files select m.FileName)
				{
					// Create directory node
					var fDir = Path.IsPathRooted(f)?"": Path.GetDirectoryName(f);
					var dirNode = DirectoryNode.CheckIfSubDirExists(this, fDir);

					var fnode = new FileNode() { FileName = Path.GetFileName( f) };
					fnode.Tag = f;
					fnode.SelectedImageKey = fnode.ImageKey = GetFileIconKey(f);

					dirNode.Nodes.Add(fnode);
				}
			}

			public override string AbsolutePath
			{
				get {
					if (IsExternalFile) 
						return Tag as string;
					return Project.FileName; 
				}
			}
		}

		public class DirectoryNode : PrjExplorerNode
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
					if (IsExternalFile)
						return Tag as string;

					string ret = "";

					var n = this;
					while (n != null)
					{
						ret = n.DirectoryName + "\\" + ret;
						n = n.Parent as DirectoryNode;
					}

					return ret.Trim(' ', '\\');
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

			public override string AbsolutePath
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
			public static TreeNode CheckIfSubDirExists(TreeNode ThisNode, string relativeDir)
			{
				if (String.IsNullOrEmpty(relativeDir) || relativeDir == ".")
					return ThisNode;

				var pathparts = relativeDir.Split('\\');

				int i = 0;
				var CurNode = ThisNode;

				// move deeper along the path
				while (i < pathparts.Length && CurNode != null)
				{
					// A bit buggy but should work in many situations
					/*if (pathparts[i] == "..")
					{
						CurNode = CurNode.Parent;
						i++;
						continue;
					}*/

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
					{
						var nn = new DirectoryNode(pathparts[i]);
						CurNode.Nodes.Add(CurNode = nn);
					}

					i++;
				}

				return CurNode;
			}

			public DirectoryNode(string DirName)
			{
				DirectoryName = DirName;
				ImageKey = SelectedImageKey = "dir";
			}
		}

		public class FileNode : PrjExplorerNode
		{
			public string FileName
			{
				get { return Text; }
				set { Text = value; }
			}

			public string RelativeFilePath
			{
				get
				{
					if (IsExternalFile)
						return Tag as string;

					string ret = FileName;

					var n = this as TreeNode;
					while (n is FileNode || n is DirectoryNode)
					{
						if (n is FileNode)
						{
							n = n.Parent;
							continue;
						}

						ret = (n as DirectoryNode).DirectoryName + "\\" + ret;
						n = n.Parent;
					}

					return ret.Trim(' ', '\\');
				}
			}

			public override string AbsolutePath
			{
				get
				{
					return ParentProjectNode.Project.ToAbsoluteFileName(RelativeFilePath);
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
		}
		#endregion

		public class PrjExplorerNodeSorter : System.Collections.IComparer
		{
			public int Compare(object x, object y)
			{
				if (y is DirectoryNode && !(x is DirectoryNode))
					return 1;

				if (x is TreeNode && y is TreeNode)
				{
					var r= string.Compare((x as TreeNode).Text,(y as TreeNode).Text);
					return r;
				}
				return 0;
			}
		}
	}
}
