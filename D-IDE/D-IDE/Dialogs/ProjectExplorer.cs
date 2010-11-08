using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace D_IDE
{
    public partial class ProjectExplorer : DockContent
    {
        public ProjectExplorer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Read a project's file structure into a TreeNode
        /// </summary>
        /// <param name="RootNode"></param>
        /// <param name="prj"></param>
        public void ReadStructure(ref TreeNode RootNode, DProject prj)
        {
            string ext = "";
            foreach (string fn in prj.Files)
            {
                // add file icon if it's not in the image array
                ext = Path.GetExtension(fn);
                if (!fileIcons.Images.ContainsKey(ext))
                {
                    Icon tico = ExtractIcon.GetIcon(fn, true);
                    fileIcons.Images.Add(ext, tico);
                }

                #region Add file to treeview
                string file = prj.GetRelFilePath(fn);

                FileTreeNode TargetFileNode = new FileTreeNode(prj, file);
                TargetFileNode.ImageKey = TargetFileNode.SelectedImageKey = Path.GetExtension(file);
                TargetFileNode.ToolTipText = prj.GetPhysFilePath(fn);

                // if its not in the project path or if it's in the projects root directory
                if (Path.IsPathRooted(file) || String.IsNullOrEmpty(Path.GetDirectoryName(file)))
                {
                    RootNode.Nodes.Add(TargetFileNode);
                }
                else // if it's in a subdirectory
                {
                    string[] DirectoriesToCheck = Path.GetDirectoryName(file).Split('\\');

                    TreeNode CurDirNode = RootNode;
                    string tdir = "";
                    foreach (string d in DirectoriesToCheck)
                    {
                        tdir += d + "\\";
                        if (!CurDirNode.Nodes.ContainsKey(d))
                        {
                            DirectoryTreeNode ndir = new DirectoryTreeNode(prj, tdir);
                            ndir.ImageKey = "dir";
                            ndir.SelectedImageKey = "dir";
                            ndir.Text = d;
                            ndir.Name = d;
                            CurDirNode.Nodes.Add(ndir);
                            CurDirNode = ndir;
                            //CurDirNode = CurDirNode.Nodes.Add(d, d, "dir", "dir");
                            //CurDirNode.Tag = new DirectoryTreeNode(prj, tdir);
                        }
                        else
                        {
                            CurDirNode = CurDirNode.Nodes[d];
                        }
                    }
                    CurDirNode.Nodes.Add(TargetFileNode);
                }
                #endregion
            }
        }

        public void ExpandToFile(DProject prj, string fn)
        {
            if (prj == null || String.IsNullOrEmpty(fn)) return;

            string file = prj.GetRelFilePath(fn);

            TreeNode ttn = null;
            foreach (TreeNode tn in prjFiles.Nodes)
            {
                if (!(tn is ProjectNode)) continue;
                if ((tn as ProjectNode).ProjectFile == prj.prjfn)
                {
                    ttn = tn;
                    break;
                }
            }

            if (ttn == null) return;
            if (ttn.Nodes.Count == 1 && ttn.Nodes[0].Text == "::Dummy")
            {
                ReadStructure(ref ttn, D_IDE_Properties.GetProject((ttn as ProjectNode).ProjectFile));
            }

            foreach (string d in file.Split('\\'))
            {
                if (ttn == null) return;
                foreach (TreeNode tn in ttn.Nodes)
                {
                    if (tn.Text == d)
                    {
                        ttn = tn;
                        break;
                    }
                }
            }
            ttn.EnsureVisible();
            prjFiles.SelectedNode = ttn;
        }

        public void ExpandToCurrentFile()
        {
            DocumentInstanceWindow diw = D_IDEForm.SelectedTabPage;
            if (diw != null)
            {
                ExpandToFile(diw.OwnerProject, diw.Module.ModuleFileName);
            }
        }

        public void UpdateFiles()
        {
            BeginInvoke(new EventHandler(delegate(object sender, EventArgs e)
            {
                Cursor.Current = Cursors.WaitCursor;
                prjFiles.BeginUpdate();
                prjFiles.Nodes.Clear();
                foreach (string prjfn in D_IDE_Properties.Default.lastProjects)
                {
                    string ext = Path.GetExtension(prjfn);
                    if (!fileIcons.Images.ContainsKey(ext))
                    {
                        Icon tico = ExtractIcon.GetIcon(prjfn, true);
                        fileIcons.Images.Add(ext, tico);
                    }

                    // if the drawn project is the current one loaded in D-IDE take it then
                    DProject LoadedPrj = D_IDEForm.thisForm.ProjectFile == prjfn ? D_IDEForm.thisForm.prj : DProject.LoadFrom(prjfn);
                    if (LoadedPrj == null) continue;
                    D_IDE_Properties.Projects[prjfn] = LoadedPrj;

                    TreeNode CurPrjNode = (TreeNode)new DedicatedProjectNode(LoadedPrj);
                    CurPrjNode.ImageKey = CurPrjNode.SelectedImageKey = ext;

                    // Add a dummy node so it's possible to open the project which are still empty
                    //CurPrjNode.Nodes.Add("::Dummy");
                    ReadStructure(ref CurPrjNode, LoadedPrj);

                    // if this project is the currently open one paint the font bold
                    if (D_IDEForm.thisForm.prj != null && prjfn == D_IDEForm.thisForm.prj.prjfn)
                    {
                        CurPrjNode.NodeFont = new Font(DefaultFont, FontStyle.Bold);
                        CurPrjNode.ExpandAll();
                    }

                    prjFiles.Nodes.Add(CurPrjNode);
                }
                prjFiles.ExpandAll();

                ExpandToCurrentFile();

                prjFiles.EndUpdate();
                Cursor.Current = Cursors.Default;
            }), null, EventArgs.Empty);
        }

        private void prjFiles_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == null) return;

            if (e.Node is DedicatedProjectNode)
            {
                D_IDEForm.thisForm.Open((e.Node as ProjectNode).ProjectFile, true);
            }
            else if (e.Node is FileTreeNode)
            {
                string fn = (e.Node as FileTreeNode).AbsolutePath;
                DProject prj = D_IDE_Properties.GetProject((e.Node as FileTreeNode).ProjectFile);
                if (prj == null) return;

                if (!prj.FileExists(fn))
                {
                    if (MessageBox.Show(fn + " doesn't exist. Do you want to remove it from the project?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        prj.Files.Remove((e.Node as FileTreeNode).FileOrPath);
                        e.Node.Remove();
                        prj.Save();
                    }
                    else return;
                }

                D_IDEForm.thisForm.Open(fn, prj.prjfn);
            }
        }

        private void prjFiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node is FileTreeNode)
                {
                    prjFiles.SelectedNode = e.Node;
                    ProjectMenu.Close();
                    FolderMenu.Close();
                    DSourceMenu.Show(prjFiles, e.Location);
                    DSourceMenu.Tag = e.Location;
                }
                else if (e.Node is DirectoryTreeNode)
                {
                    prjFiles.SelectedNode = e.Node;
                    ProjectMenu.Close();
                    DSourceMenu.Close();
                    FolderMenu.Show(prjFiles, e.Location);
                    FolderMenu.Tag = e.Location;
                }
                else if (e.Node is ProjectNode)
                {
                    prjFiles.SelectedNode = e.Node;
                    DSourceMenu.Close();
                    FolderMenu.Close();
                    ProjectMenu.Show(prjFiles, e.Location);
                    ProjectMenu.Tag = e.Location;
                }
            }
            else if (e.Button == MouseButtons.Left && e.Clicks < 2 && e.X > e.Node.Bounds.Left - 16)
            {
                if (e.Node is DedicatedProjectNode)
                    e.Node.Toggle();
            }
        }

        private void RemoveFile(object sender, EventArgs e)
        {
            Point tp = (Point)DSourceMenu.Tag;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null || !(tn is FileTreeNode)) return;

            string f = ((FileTreeNode)tn).FileOrPath;
            string phys_f = ((FileTreeNode)tn).AbsolutePath;

            DProject tprj = D_IDE_Properties.GetProject((tn as FileTreeNode).ProjectFile);
            if (tprj == null) return;

            if (tprj.FileExists(f))
            {
                DialogResult dr = MessageBox.Show("Do you want to remove \"" + Path.GetFileName(f) + "\" physically?", "Remove File",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                try
                {
                    // Close tab that may contains deleted file
                    D_IDEForm.thisForm.FileDataByFile(phys_f).Close();
                }
                catch { }

                if (dr == DialogResult.Yes)
                    File.Delete(phys_f);
                else if (dr == DialogResult.Cancel) return;
            }
            else
            {
                if (MessageBox.Show("Do you want to remove \"" + Path.GetFileName(f) + "\" from project?", "Remove File",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            tprj.Files.Remove(f);
            tprj.Save();
            D_IDEForm.thisForm.UpdateFiles();
        }

        private void addNewFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)(sender as ToolStripItem).Owner.Tag;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            DProject tprj = (tn as ProjectNode).Project;
            if (tprj == null) return;

            string initialdir = null;
            if (tn is DirectoryTreeNode)
                initialdir = (tn as DirectoryTreeNode).AbsolutePath;
            D_IDEForm.thisForm.CreateNewSourceFile(tprj, true, initialdir);

            UpdateFiles();
        }

        private void addExistingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)(sender as ToolStripItem).Owner.Tag;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            DProject tprj = (tn as ProjectNode).Project;
            if (tprj == null) return;

            D_IDEForm.thisForm.oF.InitialDirectory = tprj.basedir;
            if (D_IDEForm.thisForm.oF.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in D_IDEForm.thisForm.oF.FileNames)
                {
                    if (Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

                    tprj.AddSrc(file);
                }
                D_IDEForm.thisForm.UpdateFiles();
            }
        }

        private void addDClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)(sender as ToolStripItem).Owner.Tag;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            DProject tprj = (tn as ProjectNode).Project;
            if (tprj == null) return;

            string initialdir = null;
            if (tn is DirectoryTreeNode)
                initialdir = (tn as DirectoryTreeNode).AbsolutePath;
            string fn = D_IDEForm.thisForm.CreateNewSourceFile(tprj, false, initialdir);

            //File.WriteAllText(fn, "\r\nclass " + Path.GetFileNameWithoutExtension(fn) + "\r\n{\r\n\r\n}");
            File.WriteAllText(fn, CodeTemplate.InstantiateTemplate("_module", tprj, tprj.GetRelFilePath(fn)));

            D_IDEForm.thisForm.Open(fn);
            UpdateFiles();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)DSourceMenu.Tag;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            if (tn is FileTreeNode)
                D_IDEForm.thisForm.Open((tn as FileTreeNode).AbsolutePath, (tn as FileTreeNode).ProjectFile);
            else if (tn is ProjectNode)
                D_IDEForm.thisForm.Open((tn as ProjectNode).FileOrPath);
        }

        private void AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                if (e.Node == null) { e.CancelEdit = true; return; }

                if (e.Node is DirectoryTreeNode)
                {
                    DirectoryTreeNode dtn = e.Node as DirectoryTreeNode;
                    string ndir = Directory.GetParent(dtn.AbsolutePath) + "\\" + e.Label;

                    if (Directory.Exists(ndir))
                    {
                        MessageBox.Show(ndir + " already exists - Choose another name!");
                        e.CancelEdit = true;
                        return;
                    }

                    List<string> nfiles = new List<string>(dtn.Project.Files);
                    for (int i = 0; i < dtn.Project.Files.Count; i++)
                    {
                        string file = dtn.Project.Files[i];
                        if (file.StartsWith(dtn.AbsolutePath))
                            nfiles[i] = ndir + file.Substring(dtn.AbsolutePath.Length);
                    }

                    try
                    {
                        if (Directory.Exists(dtn.AbsolutePath))
                            Directory.Move(dtn.AbsolutePath, ndir);
                        else
                            Directory.CreateDirectory(ndir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        e.CancelEdit = true;
                        return;
                    }
                    dtn.Project.Files = nfiles;
                    dtn.FileOrPath = ndir;

                    return;
                }
                else if (e.Node is DedicatedProjectNode)
                {
                    DProject mpr = (e.Node as ProjectNode).Project;

                    mpr.name = e.Label;
                    mpr.Save();
                    D_IDEForm.thisForm.UpdateLastFilesMenu();

                    return;
                }
                else if (e.Node is FileTreeNode)
                {
                    FileTreeNode fn = e.Node as FileTreeNode;
                    string filename = new FileInfo(fn.AbsolutePath).DirectoryName + "\\" + e.Label;
                    if (File.Exists(filename))
                    {
                        MessageBox.Show(filename + " already exists - Choose another name!");
                        e.CancelEdit = true;
                        return;
                    }
                    if (!fn.Project.FileExists(fn.AbsolutePath))
                    {
                        if (MessageBox.Show(fn.AbsolutePath + " doesn't exist. Do you want to remove it from the project?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            fn.Project.Files.Remove(fn.AbsolutePath);
                            e.Node.Remove();
                            fn.Project.Save();
                        }
                        e.CancelEdit = true;
                        return;
                    }

                    string relfile = fn.AbsolutePath.Replace(fn.Project.basedir, "").TrimStart('\\');
                    List<string> nfiles = new List<string>(fn.Project.Files);
                    for (int i = 0; i < fn.Project.Files.Count; i++)
                    {
                        string file = fn.Project.Files[i];
                        if (file == relfile)
                            nfiles[i] = (relfile.Substring(0, Math.Max(0, relfile.LastIndexOf('\\'))) + "\\" + e.Label).TrimStart('\\');
                    }
                    try
                    {
                        File.Move(fn.AbsolutePath, filename);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        e.CancelEdit = true;
                        return;
                    }
                    fn.Project.Files = nfiles;
                    fn.FileOrPath = filename;

                    return;
                }

                e.CancelEdit = true;
            }
            finally
            {
                prjFiles.LabelEdit = false;
            }
        }

        private void setAsActiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)ProjectMenu.Tag;
            if (tp == null) return;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (!(tn is ProjectNode)) return;

            D_IDEForm.thisForm.Open(((ProjectNode)tn).Project.prjfn, true);
        }

        private void createNewDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void prjFiles_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node is DirectoryTreeNode)
                (e.Node as DirectoryTreeNode).ImageKey = (e.Node as DirectoryTreeNode).SelectedImageKey = "dir";
        }

        private void prjFiles_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node is DirectoryTreeNode)
                (e.Node as DirectoryTreeNode).ImageKey = (e.Node as DirectoryTreeNode).SelectedImageKey = "dir_open";
        }

        #region Drag&Drop
        private void prjFiles_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                TreeNode tn = prjFiles.GetNodeAt(prjFiles.PointToClient(new Point(e.X, e.Y)));
                if (tn == null)
                {
                    foreach (string file in files) D_IDEForm.thisForm.Open(file);
                }
                else if (tn.Tag is ProjectNode) // Take the tag because the node data is stored there
                {
                    string local = (tn.Tag as ProjectNode).AbsolutePath;
                    if (tn.Tag is DirectoryTreeNode) local += "\\";
                    DProject prj = (tn.Tag as ProjectNode).Project;

                    foreach (string file in files)
                    {
                        string tar = Path.GetDirectoryName(local) + "\\" + Path.GetFileName(file);
                        if (file != tar)
                        {
                            // Copy the file
                            if (e.Effect == DragDropEffects.Copy && file != tar)
                            {
                                File.Copy(file, tar);
                            }
                            // Move the file
                            else if (e.Effect == DragDropEffects.Move)
                            {
                                try
                                {
                                    // Update tab that may contains moved file
                                    D_IDEForm.thisForm.FileDataByFile(file).Module.ModuleFileName = tar;
                                }
                                catch { }

                                prj.Files.Remove(prj.GetRelFilePath(file));
                                File.Move(file, tar);
                            }

                            if (prj.AddSrc(tar)) prj.Save();
                        }
                        // Duplicate file
                        else if (e.Effect == DragDropEffects.Copy)
                        {
                            tar = Path.GetDirectoryName(local) + "\\Copy of " + Path.GetFileName(file);
                            File.Copy(file, tar);
                            if (prj.AddSrc(tar)) prj.Save();
                        }
                    }
                    UpdateFiles();
                }
            }
        }

        private void prjFiles_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                TreeNode tn = prjFiles.GetNodeAt(prjFiles.PointToClient(new Point(e.X, e.Y)));
                if (tn == null)
                    e.Effect = DragDropEffects.Link;
                else
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if ((tn.Tag as ProjectNode).Project.Contains(files[0]))
                    {
                        if ((e.KeyState & 8) == 8)// CTRL pressed
                            e.Effect = DragDropEffects.Copy;
                        else
                            e.Effect = DragDropEffects.Move; // The main effect if project files get moved
                    }
                    else
                        e.Effect = DragDropEffects.Copy;
                    prjFiles.SelectedNode = tn;
                    prjFiles.Update();
                }
            }
            else
                e.Effect = DragDropEffects.None;
        }
        #endregion

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)ProjectMenu.Tag;
            if (tp == null) return;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null || !(tn is ProjectNode)) return;
            DProject prj = (tn as ProjectNode).Project;
            foreach (DockContent dc in D_IDEForm.thisForm.dockPanel.Documents)
            {
                if (dc is ProjectPropertyPage)
                {
                    if ((dc as ProjectPropertyPage).project.prjfn == prj.prjfn) return;
                }
            }
            ProjectPropertyPage ppp = new ProjectPropertyPage(prj);
            if (ppp != null)
                ppp.Show(D_IDEForm.thisForm.dockPanel);
        }

        private void prjFiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is FileTreeNode)
            {
                System.Collections.Specialized.StringCollection files = new System.Collections.Specialized.StringCollection();
                files.Add((e.Item as ProjectNode).AbsolutePath);

                DataObject dto = new DataObject();
                dto.SetFileDropList(files);
                //dto.SetData(e.Item);
                prjFiles.DoDragDrop(dto, DragDropEffects.All);
            }
        }

        private void openInFormsEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!D_IDE_Properties.Default.EnableFXFormsDesigner)
            {
                MessageBox.Show("This feature will be implemented veeery soon ;-)");
                return;
            }

            Point tp = (Point)DSourceMenu.Tag;
            if (tp == null) return;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            if (tn is FileTreeNode)
                D_IDEForm.thisForm.OpenFormsDesigner((tn as FileTreeNode).AbsolutePath);
        }

        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point tp = (Point)(sender as ToolStripItem).Owner.Tag;
            if (tp == null) return;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            DProject tprj = (tn as ProjectNode).Project;
            if (tprj == null) return;

            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = tprj.basedir;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                DialogResult dr = MessageBox.Show("Also scan subdirectories?", "Add folder", MessageBoxButtons.YesNoCancel);

                if (dr == DialogResult.Cancel)
                    return;

                tprj.AddDirectory(fb.SelectedPath, dr == DialogResult.Yes);
            }
            UpdateFiles();
        }

        private void RemoveProject_Click(object sender, EventArgs e)
        {
            Point tp = (Point)ProjectMenu.Tag;
            if (tp == null) return;
            TreeNode tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            string fn = (tn as ProjectNode).ProjectFile;

            var dr = MessageBox.Show("Remove project directory?", "Remove project", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.Cancel) return;

            if (dr == DialogResult.Yes)
            {
                DProject tprj = D_IDE_Properties.Projects[fn];
                if (tprj == null) return;

                foreach (DockContent dc in D_IDEForm.thisForm.dockPanel.Documents)
                {
                    if (!(dc is DocumentInstanceWindow)) continue;

                    DocumentInstanceWindow diw = dc as DocumentInstanceWindow;
                    if (diw.ProjectFile == fn) diw.Close();
                }

                try
                {
                    Directory.Delete(tprj.basedir, true);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            try
            {
                if (File.Exists(fn))
                    File.Delete(fn);
                D_IDE_Properties.Projects.Remove(fn);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            UpdateFiles();
        }

        /// <summary>
        /// Opens project basedir in explorer
        /// </summary>
        private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tp = (Point)(sender as ToolStripItem).Owner.Tag;
            if (tp == null) return;
            var tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            var tprj = (tn as ProjectNode).Project;
            if (tprj == null) return;

            string dir = '"' + tprj.basedir + '"';
            if (tn is DirectoryTreeNode)
                dir = '"' + (tn as DirectoryTreeNode).AbsolutePath + '"';
            else if (tn is FileTreeNode)
                dir = "/select,\"" + (tn as FileTreeNode).AbsolutePath + '"';
            Process.Start("explorer", dir);
        }

        /// <summary>
        /// Creates a new folder for the project
        /// </summary>
        private void addFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tp = (Point)(sender as ToolStripItem).Owner.Tag;
            var tn = prjFiles.GetNodeAt(tp);
            if (tn == null) return;

            //DProject tprj = D_IDE_Properties.GetProject((tn as ProjectNode).ProjectFile);
            ProjectNode pn = (tn as ProjectNode);
            DProject tprj = pn.Project;
            if (tprj == null) return;

            string dirname = pn.FileOrPath;
            if (tn is FileTreeNode) dirname = Path.GetDirectoryName(dirname);
            else if (tn is DedicatedProjectNode) dirname = "";
            if (Directory.Exists(tprj.basedir + (dirname == "" ? "" : "\\" + dirname) + "\\New Folder"))
            {
                int i = 2;
                while (Directory.Exists(tprj.basedir + (dirname == "" ? "" : "\\" + dirname) + "\\New Folder (" + i + ")")) ++i;
                dirname = dirname + "\\New Folder (" + i + ")";
            }
            else dirname += "\\New Folder";
            dirname = dirname.TrimStart('\\');
            Directory.CreateDirectory(tprj.basedir + "\\" + dirname);
            var dnode = new DirectoryTreeNode(tprj, dirname);
            if (tn is FileTreeNode)
                tn.Parent.Nodes.Add(dnode);
            else tn.Nodes.Add(dnode);
            prjFiles.LabelEdit = true;
            dnode.BeginEdit();
        }

        /// <summary>
        /// Key up events
        /// </summary>
        private void prjFiles_KeyUp(object sender, KeyEventArgs e)
        {
            if (prjFiles.SelectedNode != null)
            {
                //Edit
                if (e.KeyCode == Keys.F2)
                {
                    var node = prjFiles.SelectedNode;
                    if (node is DirectoryTreeNode ||
                        node is FileTreeNode ||
                        node is DedicatedProjectNode)
                    {
                        prjFiles.LabelEdit = true;
                        node.BeginEdit();
                        return;
                    }
                }
            }
        }
    }

    #region Nodes
    public class ProjectNode : TreeNode
    {
        public string ProjectFile;
        public DProject Project
        {
            get { return D_IDE_Properties.GetProject(ProjectFile); }
        }
        public string FileOrPath;
        public string AbsolutePath
        {
            get { return Project.GetPhysFilePath(FileOrPath); }
        }
        public ProjectNode(DProject project)
        {
            if (!String.IsNullOrEmpty(project.name)) Text = project.name;
            FileOrPath = ProjectFile = project.prjfn;
            Tag = this;
        }
    }

    public class DedicatedProjectNode : ProjectNode
    {
        public DedicatedProjectNode(DProject prj)
            : base(prj)
        {
        }
    }

    public class FileTreeNode : ProjectNode
    {
        public FileTreeNode(DProject Project, string FileName)
            : base(Project)
        {
            if (CodeModule.Parsable(FileName)) this.ImageKey = "d_src";
            if (!String.IsNullOrEmpty(FileName)) Text = Path.GetFileName(FileName);
            FileOrPath = FileName;
        }
    }

    public class DirectoryTreeNode : ProjectNode
    {
        public DirectoryTreeNode(DProject Project, string Path)
            : base(Project)
        {
            if (!String.IsNullOrEmpty(Path)) Text = System.IO.Path.GetFileName(Path.TrimEnd('\\'));
            this.ImageKey = this.SelectedImageKey = "dir";
            FileOrPath = Path.TrimEnd('\\');
        }
    }
    #endregion
}
