using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using D_IDE.CodeCompletion;
using D_IDE.Dialogs;
using D_Parser;
using D_IDE.Properties;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Util;
using WeifenLuo.WinFormsUI;
using WeifenLuo.WinFormsUI.Docking;
using System.Runtime.InteropServices;
using DebugEngineWrapper;
using D_IDE.Misc;

namespace D_IDE
{
    public partial class D_IDEForm
    {
        #region Properties
        public static string title = "D-IDE " + Application.ProductVersion;

        public StartPage startpage = new StartPage();
        public PropertyView propView = new PropertyView();
        public ProjectExplorer prjexplorer = new ProjectExplorer();
        public ClassHierarchy hierarchy = new ClassHierarchy();
        public BuildProcessWin bpw = new BuildProcessWin();
        public ErrorLog errlog = new ErrorLog();
        public OutputWin output = new OutputWin();
        public BreakpointWin dbgwin = new BreakpointWin();
        public CallStackWin callstackwin = new CallStackWin();
        public DebugLocals dbgLocalswin = new DebugLocals();
        public bool UseOutput = false;
        //public RibbonSetup RibbonSetup;

        public DProject prj
        {
            get
            {
                return D_IDE_Properties.GetProject(ProjectFile);
            }
            set
            {
                if (value == null)
                {
                    ProjectFile = "";
                    return;
                }
                ProjectFile = value.prjfn;
                D_IDE_Properties.Projects[value.prjfn] = value;
            }
        }
        public string ProjectFile = "";
        public SearchReplaceDlg searchDlg = new SearchReplaceDlg();
        public GoToLineDlg gotoDlg = new GoToLineDlg();
        public static D_IDEForm thisForm;
        protected Process exeProc;
        protected Thread updateTh;
        public static DocumentInstanceWindow SelectedTabPage
        {
            get
            {
                if (thisForm.dockPanel.ActiveDocument is DocumentInstanceWindow)
                    return (DocumentInstanceWindow)thisForm.dockPanel.ActiveDocument;
                else return null;
            }
        }
        #endregion

        public void RemoveFileFromPrj(string file)
        {
            if (prj == null) return;

            prj.Files.Remove(file);

            UpdateFiles();
        }

        public void UpdateLastFilesMenu()
        {
            /*if (RibbonSetup != null)
            {
                RibbonSetup.LastFiles.DropDownItems.Clear();
            }*/
            lastOpenProjects.DropDownItems.Clear();
            lastOpenFiles.DropDownItems.Clear();
            if (D_IDE_Properties.Default.lastProjects == null) D_IDE_Properties.Default.lastProjects = new List<string>();
            if (D_IDE_Properties.Default.lastFiles == null) D_IDE_Properties.Default.lastFiles = new List<string>();
            startpage.lastProjects.Items.Clear();
            startpage.lastFiles.Items.Clear();
            foreach (string prjfile in D_IDE_Properties.Default.lastProjects)
            {
                if (File.Exists(prjfile))
                {
                    /*if (RibbonSetup != null)
                    {
                        RibbonButton tb = new RibbonButton();
                        tb.Tag = prjfile;
                        tb.Text = Path.GetFileName(prjfile);
                        tb.Click += new EventHandler(LastProjectsItemClick);
                        RibbonSetup.LastFiles.DropDownItems.Add(tb);
                    }*/

                    ToolStripMenuItem tsm = new ToolStripMenuItem();
                    tsm.Tag = prjfile;
                    tsm.Text = Path.GetFileName(prjfile);
                    tsm.Click += new EventHandler(LastProjectsItemClick);
                    lastOpenProjects.DropDownItems.Add(tsm);
                    startpage.lastProjects.Items.Add(tsm.Text);
                }
            }
            //if(RibbonSetup!=null) RibbonSetup.LastFiles.DropDownItems.Add(new RibbonSeparator());
            foreach (string file in D_IDE_Properties.Default.lastFiles)
            {
                if (File.Exists(file))
                {
                    /*if (RibbonSetup != null)
                    {
                        RibbonButton tb = new RibbonButton();
                        tb.Tag = file;
                        tb.Text = Path.GetFileName(file);
                        tb.Click += new EventHandler(LastProjectsItemClick);
                        RibbonSetup.LastFiles.DropDownItems.Add(tb);
                    }*/

                    ToolStripMenuItem tsm = new ToolStripMenuItem();
                    tsm.Tag = file;
                    tsm.Text = Path.GetFileName(file);
                    tsm.Click += new EventHandler(LastProjectsItemClick);
                    lastOpenFiles.DropDownItems.Add(tsm);
                    startpage.lastFiles.Items.Add(Path.GetFileName(file));
                }
            }
        }

        public DocumentInstanceWindow FileDataByFile(string fn)
        {
            foreach (DockContent dc in dockPanel.Documents)
            {
                if (!(dc is DocumentInstanceWindow)) continue;
                DocumentInstanceWindow diw = dc as DocumentInstanceWindow;
                if (diw.Module.ModuleFileName == fn) return diw;
            }
            return null;
        }
        public FXFormsDesigner FXFormDesignerByFile(string fn)
        {
            foreach (DockContent dc in dockPanel.Documents)
            {
                if (!(dc is FXFormsDesigner)) continue;
                FXFormsDesigner diw = dc as FXFormsDesigner;
                if (diw.FileName == fn) return diw;
            }
            return null;
        }

        void SaveAllTabs()
        {
            if (prj != null) prj.Save();
            foreach (DockContent tp in dockPanel.Documents)
            {
                if (tp is DocumentInstanceWindow)
                {
                    DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
                    //mtp.txt.Document.CustomLineManager.Clear();
                    mtp.Save();
                }
            }
        }

        public static void UpdateChacheThread(CompilerConfiguration cc)
        {
            CodeModule.ClearErrorLogBeforeParsing = false;
            var ret = new List<CodeModule>();

            if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = true;
            //bpw.Clear();
            if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Reparse all directories");

            foreach (string dir in cc.ImportDirectories)
            {
                var dirProject = new DProject();
                dirProject.basedir = dir;

                if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Parse directory " + dir);
                if (!Directory.Exists(dir))
                {
                    if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Directory \"" + dir + "\" does not exist!");
                    continue;
                }
                var files = Directory.GetFiles(dir, "*.d?", SearchOption.AllDirectories);
                foreach (string tf in files)
                {
                    if (tf.EndsWith("phobos.d")) continue; // Skip phobos.d

                    if (D_IDE_Properties.HasModule(ret, tf)) { if (D_IDEForm.thisForm != null)D_IDEForm.thisForm.Log(tf + " already parsed!"); continue; }

                    string tmodule = Path.ChangeExtension(tf, null).Remove(0, dir.Length + 1).Replace('\\', '.');
                    CodeModule gpf = null;
                    if (Debugger.IsAttached)
                        gpf = new CodeModule(dirProject, tf);
                    else
                    {
                        try
                        {
                            gpf = new CodeModule(dirProject, tf);
                        }
                        catch (Exception ex)
                        {
                            if (Debugger.IsAttached) throw ex;
                            try
                            {
                                if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log(tf);
                            }
                            catch { }
                            if (MessageBox.Show(ex.Message + "\n\nStop parsing process?\n\n" + ex.Source, "Error at " + tf, MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = false;
                                return;
                            }
                        }
                    }
                    if (gpf == null) continue;
                    gpf.ModuleName = tmodule;
                    D_IDE_Properties.AddFileData(ret, gpf);
                }
            }
            if (D_IDEForm.thisForm != null)
            {
                D_IDEForm.thisForm.Log(D_IDEForm.thisForm.ProgressStatusLabel.Text = "Parsing done!");
                D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = false;
            }
            CodeModule.ClearErrorLogBeforeParsing = true;
            lock (cc.GlobalModules)
            {
                cc.GlobalModules = ret;
            }
        }

        /*
		/// <summary>
		/// TODO: Fix this
		/// </summary>
		/// <param name="file"></param>
		/// <param name="newfile"></param>
		/// <returns></returns>
		public bool RenameFile(string file, string newfile)
		{
			if (prj == null || String.IsNullOrEmpty(file) || String.IsNullOrEmpty(newfile)) return false;

			DocumentInstanceWindow diw = null;
			// Update current tab view
			if (dockPanel.DocumentsCount > 0)
				foreach (DockContent dc in dockPanel.Documents)
				{
					if (dc is DocumentInstanceWindow)
					{
						diw = (DocumentInstanceWindow)dc;
						if (diw.fileData.ModuleFileName == file)
						{
							return false;
						}
					}
				}

			// Update project
			if (DModule.Parsable(file) && DModule.Parsable(newfile))
			{
				if (diw == null)
				{
					prj.files.Remove(prj.FileDataByFile(file));
					prj.AddSrc(newfile);
				}
				else
				{
					prj.files.Remove(diw.fileData);
					prj.files.Add(diw.fileData);
				}
			}
			else if (DModule.Parsable(file) && !DModule.Parsable(newfile))
			{
				prj.files.Remove(prj.FileDataByFile(file));
				prj.resourceFiles.Add(newfile);
			}
			else if (!DModule.Parsable(file) && !DModule.Parsable(newfile))
			{
				prj.resourceFiles.Remove(file);
				prj.resourceFiles.Add(newfile);
			}
			else if (!DModule.Parsable(file) && DModule.Parsable(newfile))
			{
				prj.resourceFiles.Remove(file);
				prj.files.Add(new DModule(newfile));
			}

			try
			{
				// Move physical file AFTER saving it
				File.Move(file, newfile);
			}
			catch { }

			UpdateFiles();
			return true;
		}*/

        public void UpdateFiles()
        {
            if (prj != null) Text = prj.name + " - " + title;
            prjexplorer.UpdateFiles();
        }

        public string CreateNewSourceFile(bool OpenAfterCreating)
        {
            return CreateNewSourceFile((prj!=null && MessageBox.Show("Do you want to add the file to the current project?", "New File",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) ? prj : null, OpenAfterCreating);
        }

        public string CreateNewSourceFile(DProject Project, bool OpenAfterCreating, string initialdir = null)
        {
            string tfn = "Untitled.d";

            sF.Filter = "All Files (*.*)|*.*";
            sF.FileName = tfn;
            if (Project != null) sF.InitialDirectory = initialdir != null ? initialdir : Project.basedir;
            if (sF.ShowDialog() == DialogResult.OK)
            {
                tfn = sF.FileName;
            }
            else return null;
            try
            {
                File.WriteAllText(tfn, "");
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); return null; }
            if (OpenAfterCreating) Open(tfn, Project != null ? Project.prjfn : "");
            if (Project != null)
            {
                Project.AddSrc(tfn);
                Project.Save();
            }
            prjexplorer.Refresh();

            return tfn;
        }

        /// <summary>
        /// Central accessing method to open files or projects. Use this method only to open files!
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [DebuggerStepThrough()]
        public DocumentInstanceWindow Open(string file, bool silent = false)
        {
            if (prj != null && prj.Files.Contains(prj.GetRelFilePath(file)))
                return Open(prj.GetPhysFilePath(file), ProjectFile);
            return Open(file, "", silent);
        }
        public DocumentInstanceWindow Open(string file, string owner, bool silent = false)
        {
            if (prj != null && file == prj.prjfn) return null; // Don't reopen the current project

            DocumentInstanceWindow ret = null;

            foreach (var dc in dockPanel.Documents)
            {
                if (!(dc is DocumentInstanceWindow)) continue;
                var diw = dc as DocumentInstanceWindow;
                if (diw.Module.ModuleFileName == file)
                {
                    diw.Activate();
                    Application.DoEvents();
                    return diw;
                }
            }

            if (!File.Exists(file))
            {
                Log(ProgressStatusLabel.Text = ("File " + file + " doesn't exist!"));
                return null;
            }

            if (Path.GetExtension(file) == DProject.prjext)
            {
                if (prj != null && !silent)
                {
                    if (MessageBox.Show("Do you want to open another project?", "Open new project", MessageBoxButtons.YesNo) == DialogResult.No) return null;
                }
                prj = DProject.LoadFrom(file);
                if (prj == null) { MessageBox.Show("Failed to load project! Perhaps the projects version differs from the current version."); return null; }

                if (D_IDE_Properties.Default.lastProjects.Contains(file)) D_IDE_Properties.Default.lastProjects.Remove(file);
                D_IDE_Properties.Default.lastProjects.Insert(0, file);
                if (D_IDE_Properties.Default.lastProjects.Count > 10) D_IDE_Properties.Default.lastProjects.RemoveAt(10);

                if (prj.basedir == ".") prj.basedir = Path.GetDirectoryName(file);

                if (String.IsNullOrEmpty(prj.basedir) || !Directory.Exists(prj.basedir))
                {
                    if (MessageBox.Show("The projects base directory doesn't exist anymore! Shall D-IDE set it to the project file path?", "Notice", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        prj.basedir = Path.GetDirectoryName(file);
                }

                prj.ParseAll();

                foreach (string f in prj.LastOpenedFiles)
                {
                    Open(f, ProjectFile);
                }
                prj.LastOpenedFiles.Clear();
                ret = SelectedTabPage;
                UpdateFiles();
            }
            else
            {
                var mtp = new DocumentInstanceWindow(file, owner);

                if (D_IDE_Properties.Default.lastFiles.Contains(file))
                {
                    D_IDE_Properties.Default.lastFiles.Remove(file);
                }
                D_IDE_Properties.Default.lastFiles.Insert(0, file);
                if (D_IDE_Properties.Default.lastFiles.Count > 10) D_IDE_Properties.Default.lastFiles.RemoveAt(10);

                mtp.TabPageContextMenuStrip = DocumentWindowContextMenu;
                mtp.DrawBreakPoints();
                mtp.Show(dockPanel);
                ret = mtp;
            }
            //RefreshClassHierarchy();
            UpdateLastFilesMenu();

            // Important: set Read-Only flag if Debugger is running currently
            if (ret != null && ret.txt != null)
                ret.txt.IsReadOnly = IsDebugging;

            return ret;
        }

        public static ImageList InitCodeCompletionIcons()
        {

            System.Resources.ResourceManager rm = new System.Resources.ResourceManager("D_IDE.Icons", Assembly.GetAssembly(typeof(D_IDEForm)));
            icons = new ImageList();
            // 
            // icons
            // 
            icons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(rm.GetObject("icons.ImageStream")));
            icons.TransparentColor = System.Drawing.Color.Transparent;
            icons.Images.SetKeyName(0, "Icons.16x16.Enum.png");
            icons.Images.SetKeyName(1, "Icons.16x16.Field.png");
            icons.Images.SetKeyName(2, "Icons.16x16.Interface.png");
            icons.Images.SetKeyName(3, "Icons.16x16.InternalClass.png");
            icons.Images.SetKeyName(4, "Icons.16x16.InternalDelegate.png");
            icons.Images.SetKeyName(5, "Icons.16x16.InternalEnum.png");
            icons.Images.SetKeyName(6, "Icons.16x16.InternalEvent.png");
            icons.Images.SetKeyName(7, "Icons.16x16.InternalField.png");
            icons.Images.SetKeyName(8, "Icons.16x16.InternalIndexer.png");
            icons.Images.SetKeyName(9, "Icons.16x16.InternalInterface.png");
            icons.Images.SetKeyName(10, "Icons.16x16.InternalMethod.png");
            icons.Images.SetKeyName(11, "Icons.16x16.InternalProperty.png");
            icons.Images.SetKeyName(12, "Icons.16x16.InternalStruct.png");
            icons.Images.SetKeyName(13, "Icons.16x16.Literal.png");
            icons.Images.SetKeyName(14, "Icons.16x16.Method.png");
            icons.Images.SetKeyName(15, "Icons.16x16.Parameter.png");
            icons.Images.SetKeyName(16, "Icons.16x16.PrivateClass.png");
            icons.Images.SetKeyName(17, "Icons.16x16.PrivateDelegate.png");
            icons.Images.SetKeyName(18, "Icons.16x16.PrivateEnum.png");
            icons.Images.SetKeyName(19, "Icons.16x16.PrivateEvent.png");
            icons.Images.SetKeyName(20, "Icons.16x16.PrivateField.png");
            icons.Images.SetKeyName(21, "Icons.16x16.PrivateIndexer.png");
            icons.Images.SetKeyName(22, "Icons.16x16.PrivateInterface.png");
            icons.Images.SetKeyName(23, "Icons.16x16.PrivateMethod.png");
            icons.Images.SetKeyName(24, "Icons.16x16.PrivateProperty.png");
            icons.Images.SetKeyName(25, "Icons.16x16.PrivateStruct.png");
            icons.Images.SetKeyName(26, "Icons.16x16.Property.png");
            icons.Images.SetKeyName(27, "Icons.16x16.ProtectedClass.png");
            icons.Images.SetKeyName(28, "Icons.16x16.ProtectedDelegate.png");
            icons.Images.SetKeyName(29, "Icons.16x16.ProtectedEnum.png");
            icons.Images.SetKeyName(30, "Icons.16x16.ProtectedEvent.png");
            icons.Images.SetKeyName(31, "Icons.16x16.ProtectedField.png");
            icons.Images.SetKeyName(32, "Icons.16x16.ProtectedIndexer.png");
            icons.Images.SetKeyName(33, "Icons.16x16.ProtectedInterface.png");
            icons.Images.SetKeyName(34, "Icons.16x16.ProtectedMethod.png");
            icons.Images.SetKeyName(35, "Icons.16x16.ProtectedProperty.png");
            icons.Images.SetKeyName(36, "Icons.16x16.ProtectedStruct.png");
            icons.Images.SetKeyName(37, "Icons.16x16.Struct.png");
            icons.Images.SetKeyName(38, "Icons.16x16.Local.png");
            icons.Images.SetKeyName(39, "Icons.16x16.Class.png");
            icons.Images.SetKeyName(40, "Icons.16x16.Delegate.png");
            icons.Images.SetKeyName(41, "code");
            icons.Images.SetKeyName(42, "namespace");

            return icons;
        }

        private void RefreshClassHierarchy()
        {
            var mtp = SelectedTabPage;
            if (mtp == null) return;
            TreeNode oldNode = null;
            if (hierarchy.hierarchy.Nodes.Count > 0) oldNode = hierarchy.hierarchy.Nodes[0];
            hierarchy.hierarchy.Nodes.Clear();

            hierarchy.hierarchy.BeginUpdate();
            var tn = new TreeNode(mtp.Module.ModuleName);
            tn.SelectedImageKey = tn.ImageKey = "namespace";
            int i = 0;
            foreach (var ch in mtp.Module)
            {
                TreeNode ctn = GenerateHierarchyData(mtp.Module, ch,
                    (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == ch.Name) ?
                    oldNode.Nodes[i] : null);
                ctn.ToolTipText = DCompletionData.BuildDescriptionString(ch);
                ctn.Tag = ch;
                tn.Nodes.Add(ctn);
                i++;
            }
            tn.Expand();
            hierarchy.hierarchy.Nodes.Add(tn);
            hierarchy.hierarchy.EndUpdate();
        }

        public FXFormsDesigner OpenFormsDesigner(string file)
        {
            FXFormsDesigner ret = null;

            foreach (DockContent dc in dockPanel.Documents)
            {
                if (!(dc is FXFormsDesigner)) continue;
                FXFormsDesigner fd = dc as FXFormsDesigner;
                if (fd.FileName == file) return fd;
            }

            if (!File.Exists(file))
            {
                Log(ProgressStatusLabel.Text = ("File " + file + " doesn't exist!"));
                return null;
            }

            ret = new FXFormsDesigner(file);
            ret.Show(dockPanel, DockState.Document);
            if (this.dockPanel.ActiveDocumentPane != null) this.dockPanel.ActiveDocumentPane.ContextMenuStrip = this.DocumentWindowContextMenu; // Set Tab selection bars context menu to ours
            return ret;
        }

        TreeNode GenerateHierarchyData(DNode env, DNode ch, TreeNode oldNode)
        {
            if (ch == null) return null;
            int ii = DCompletionData.GetImageIndex(icons, ch);

            TreeNode ret = new TreeNode(ch.Name, ii, ii);
            ret.Tag = ch;
            ret.SelectedImageIndex = ret.ImageIndex = ii;

            ii = icons.Images.IndexOfKey("Icons.16x16.Parameter.png");
            int i = 0;

            if(ch.TemplateParameters!=null)
                foreach (DNode dt in ch.TemplateParameters)
                {
                    TreeNode tn = GenerateHierarchyData(ch, dt,
                        (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == dt.Name) ?
                        oldNode.Nodes[i] : null);
                    tn.SelectedImageIndex = tn.ImageIndex = ii;
                    tn.ToolTipText = DCompletionData.BuildDescriptionString(dt);

                    ret.Nodes.Add(tn);
                    i++;
                }

            if(ch is DMethod)
                foreach (DNode dt in (ch as DMethod).Parameters)
                {
                    TreeNode tn = GenerateHierarchyData(ch, dt,
                        (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == dt.Name) ?
                        oldNode.Nodes[i] : null);
                    tn.SelectedImageIndex = tn.ImageIndex = ii;
                    tn.ToolTipText = DCompletionData.BuildDescriptionString(dt);

                    ret.Nodes.Add(tn);
                    i++;
                }
            i = 0;
            if(ch is DBlockStatement)
                foreach (var dt in (ch as DBlockStatement))
                {
                    ii = DCompletionData.GetImageIndex(icons, dt);

                    var tn = GenerateHierarchyData(ch, dt,
                        (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == dt.Name) ?
                        oldNode.Nodes[i] : null);

                    tn.ToolTipText = DCompletionData.BuildDescriptionString(dt);
                    // if it is a statement block only
                    if (dt is DBlockStatement && !(dt is DMethod || dt is DClassLike || dt is DEnum))
                    {
                        foreach(TreeNode stn in tn.Nodes)
                            ret.Nodes.Add(stn);
                    }
                    else
                        ret.Nodes.Add(tn);
                    i++;
                }
            if (oldNode != null && oldNode.IsExpanded && oldNode.Text == ch.Name)
                ret.Expand();
            return ret;
        }

        public void Log(string m)
        {
            if (!UseOutput) bpw.Log(m);
            else output.Log(m);
        }

        /// <summary>
        /// Add an error notification to the error list and set this to the current selection in the editor. If needed, the specific file gets opened automatically
        /// </summary>
        /// <param name="file"></param>
        /// <param name="lineNumber"></param>
        void AddHighlightedBuildError(string file, int lineNumber, string errmsg, Color color)
        {
            errlog.AddBuildError(file, lineNumber, errmsg);
        }
    }
}