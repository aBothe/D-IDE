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

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für ProjectExplorer.xaml
	/// </summary>
	public partial class ProjectExplorer : AvalonDock.DockableContent
	{
		public ProjectExplorer()
		{
			InitializeComponent();

			winFormsHost.Child = MainTree;

			MainTree.ImageList = TreeIcons;
			MainTree.StateImageList = TreeIcons;

			MainTree.BeforeCollapse += new TreeViewCancelEventHandler(MainTree_BeforeCollapse);
			MainTree.BeforeExpand += new TreeViewCancelEventHandler(MainTree_BeforeExpand);

			MainTree.BorderStyle = BorderStyle.None;
		}

		void MainTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node is DirectoryNode)
				e.Node.ImageKey = e.Node.SelectedImageKey = "dir_open";
		}

		void MainTree_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node is DirectoryNode)
				e.Node.ImageKey = e.Node.SelectedImageKey = "dir";
		}

		System.Windows.Forms.TreeView MainTree = new System.Windows.Forms.TreeView();
		static readonly ImageList TreeIcons = new ImageList();

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
						if(pt.SmallImage!=null && pt.Extensions!=null)
							foreach(var ext in pt.Extensions)
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
				TreeIcons.Images.Add(ext, Win32.GetIcon(FileName,true));
			}

			return ext;
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
		}

		public class ModuleNode : FileNode
		{
			public IModule Module;
			public new string FileName
			{
				get { return Module.FileName; }
			}
		}
	}
}
