using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;

namespace D_IDE
{
	public partial class ProjectExplorer : DockContent
	{
		public ProjectExplorer()
		{
			InitializeComponent();
		}

		private void prjFiles_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if(prjFiles.SelectedNode == null) return;

			if(prjFiles.SelectedNode is ProjectNode)
			{
				DProject prj = (prjFiles.SelectedNode as ProjectNode).Project;
				Form1.thisForm.Open(prj.prjfn);
			}
			else if(prjFiles.SelectedNode is FileTreeNode)
			{
				Form1.thisForm.Open((prjFiles.SelectedNode as FileTreeNode).FileName, (prjFiles.SelectedNode as FileTreeNode).Project);
			}
		}

		private void prjFiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if(e.Button == MouseButtons.Right)
			{
				if(e.Node is ProjectNode)
				{
					DSourceMenu.Close();
					ProjectMenu.Show(prjFiles, e.Location);
					ProjectMenu.Tag = e.Location;
				}
				else if(e.Node is FileTreeNode)
				{
					ProjectMenu.Close();
					DSourceMenu.Show(prjFiles, e.Location);
					DSourceMenu.Tag = e.Location;
				}
			}
		}

		private void RemoveFile(object sender, EventArgs e)
		{
			Point tp = (Point)DSourceMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(tn == null || !(tn is FileTreeNode)) return;

			string f = ((FileTreeNode)tn).FileName;
			if(File.Exists(f))
			{
				DialogResult dr = MessageBox.Show("Do you want to remove \"" + Path.GetFileName(f) + "\" physically?", "Remove File",
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

				if(dr == DialogResult.Yes)
					File.Delete(f);
				else if(dr == DialogResult.Cancel) return;
			}
			else
			{
				if(MessageBox.Show("Do you want to remove \"" + Path.GetFileName(f) + "\" from project?", "Remove File",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
			}

			((FileTreeNode)tn).Project.resourceFiles.Remove(f);
			((FileTreeNode)tn).Project.Save();
			Form1.thisForm.UpdateFiles();


		}

		private void addNewFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Form1.thisForm.NewSourceFile(sender, e);
		}

		private void addExistingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point tp = (Point)ProjectMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(tn == null) return;

			if(Form1.thisForm.oF.ShowDialog() == DialogResult.OK)
			{
				foreach(string file in Form1.thisForm.oF.FileNames)
				{
					if(Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

					((ProjectNode)tn).Project.AddSrc(file);
				}
				Form1.thisForm.UpdateFiles();
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point tp = (Point)DSourceMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(tn == null) return;

			if(tn is ProjectNode)
				Form1.thisForm.Open((tn as ProjectNode).Project.prjfn);
			if(tn is FileTreeNode)
				Form1.thisForm.Open((tn as FileTreeNode).FileName, (tn as FileTreeNode).Project);
		}

		private void AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			if(e.Node == null) { e.CancelEdit = true; return; }

			if(e.Node is ProjectNode)
			{
				DProject mpr = (e.Node as ProjectNode).Project;

				mpr.name = e.Label;
				mpr.Save();
				Form1.thisForm.UpdateLastFilesMenu();
			}
			else if(e.Node is DirectoryTreeNode)
			{
				DirectoryTreeNode dtn = e.Node as DirectoryTreeNode;
				string ndir = Directory.GetParent(dtn.Path) + "\\" + e.Label;

				if(Directory.Exists(ndir))
				{
					MessageBox.Show(ndir + " already exists - Choose another name!");
					e.CancelEdit = true;
					return;
				}

				List<string> nfiles = new List<string>(dtn.Project.resourceFiles);
				for(int i = 0; i < dtn.Project.resourceFiles.Count; i++)
				{
					string file = dtn.Project.resourceFiles[i];
					if(file.StartsWith(dtn.Path))
						nfiles[i] = ndir + file.Substring(dtn.Path.Length);
				}

				try
				{
					if(Directory.Exists(dtn.Path))
						Directory.Move(dtn.Path, ndir);
					else
						Directory.CreateDirectory(ndir);
				}
				catch(Exception ex)
				{
					MessageBox.Show(ex.Message);
					e.CancelEdit = true;
					return;
				}
				dtn.Project.resourceFiles = nfiles;

				return;
			}

			e.CancelEdit = true;
		}

		private void setAsActiveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point tp = (Point)ProjectMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(!(tn is ProjectNode)) return;

			Form1.thisForm.Open(((ProjectNode)tn).Project.prjfn);
		}

		private void createNewDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point tp = (Point)ProjectMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(tn == null || !(tn is ProjectNode)) return;

			string ndir = (tn as ProjectNode).Project.basedir + "\\New Directory";

			DirectoryTreeNode ntn = new DirectoryTreeNode((tn as ProjectNode).Project, ndir);
			tn.Nodes.Add(ntn);
			ntn.BeginEdit();
		}

		private void prjFiles_AfterCollapse(object sender, TreeViewEventArgs e)
		{
			if(e.Node is DirectoryTreeNode)
				(e.Node as DirectoryTreeNode).ImageKey = (e.Node as DirectoryTreeNode).SelectedImageKey = "dir";
		}

		private void prjFiles_AfterExpand(object sender, TreeViewEventArgs e)
		{
			if(e.Node is DirectoryTreeNode)
				(e.Node as DirectoryTreeNode).ImageKey = (e.Node as DirectoryTreeNode).SelectedImageKey = "dir_open";
		}

		#region Drag&Drop
		private void prjFiles_DragEnter(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Link;
			}
			else
				e.Effect = DragDropEffects.None;
		}

		private void prjFiles_DragDrop(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				TreeNode tn = prjFiles.GetNodeAt(prjFiles.PointToClient(new Point(e.X, e.Y)));
				if(tn == null)
				{
					foreach(string file in files)	Form1.thisForm.Open(file);
				}
				if(tn is ProjectNode)
				{
					DProject prj=(tn as ProjectNode).Project;
					if(MessageBox.Show(this,"Do you want to add the file(s) to the "+prj.name+" project?","Add files to project",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button1)==DialogResult.Yes)
					{
						foreach(string file in files)
						{
							prj.AddSrc(file);
						}
						prj.Save();
						Form1.thisForm.UpdateFiles();
					}
					else
						foreach(string file in files)	Form1.thisForm.Open(file);
				}
				
				
			}
		}
	

		private void prjFiles_DragOver(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				TreeNode tn = prjFiles.GetNodeAt(prjFiles.PointToClient(new Point(e.X, e.Y)));
				if(tn == null) e.Effect = DragDropEffects.Link;
				if(tn is ProjectNode) e.Effect = DragDropEffects.Copy;
				if(tn is FileTreeNode) e.Effect = DragDropEffects.None;
				prjFiles.SelectedNode = tn;
				prjFiles.Update();
			}
			else
				e.Effect = DragDropEffects.None;
		}
		#endregion

		private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point tp = (Point)ProjectMenu.Tag;
			if(tp == null) return;
			TreeNode tn = prjFiles.GetNodeAt(tp);
			if(tn == null || !(tn is ProjectNode)) return;
			DProject prj=(tn as ProjectNode).Project;
			foreach(DockContent dc in Form1.thisForm.dockPanel.Documents)
			{
				if(dc is ProjectPropertyPage)
				{
					if((dc as ProjectPropertyPage).project.prjfn == prj.prjfn) return;
				}
			}
			ProjectPropertyPage ppp = new ProjectPropertyPage(prj);
			if(ppp != null)
				ppp.Show(Form1.thisForm.dockPanel);
		}
	}

	#region Nodes
	public class ProjectNode : TreeNode
	{
		public DProject Project;
		public ProjectNode(DProject project)
		{
			if(!String.IsNullOrEmpty(project.name)) Text = project.name;
			Project = project;
		}
	}

	public class FileTreeNode : TreeNode
	{
		public DProject Project;
		public string FileName;
		public FileTreeNode(DProject Project, string FileName)
		{
			this.Project = Project;
			if(DModule.Parsable(FileName))this.ImageKey = "d_src";
			if(!String.IsNullOrEmpty(FileName)) Text = Path.GetFileName(FileName);
			this.FileName = FileName;
		}
	}

	public class DirectoryTreeNode : TreeNode
	{
		public DProject Project;
		public string Path;
		public DirectoryTreeNode(DProject Project, string Path)
		{
			this.Project = Project;
			if(!String.IsNullOrEmpty(Path)) Text = System.IO.Path.GetFileName(Path);
			this.ImageKey = this.SelectedImageKey = "dir";
			this.Path = Path;
		}
	}
	#endregion
}
