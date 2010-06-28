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
                if (diw.fileData.mod_file == fn) return diw;
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
            DModule.ClearErrorLogBeforeParsing = false;
            List<DModule> ret = new List<DModule>();

            if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = true;
            //bpw.Clear();
            if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Reparse all directories");

            foreach (string dir in cc.ImportDirectories)
            {
                DProject dirProject = new DProject();
                dirProject.basedir = dir;

                if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Parse directory " + dir);
                if (!Directory.Exists(dir))
                {
                    if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log("Directory \"" + dir + "\" does not exist!");
                    continue;
                }
                string[] files = Directory.GetFiles(dir, "*.d?", SearchOption.AllDirectories);
                foreach (string tf in files)
                {
                    if (tf.EndsWith("phobos.d")) continue; // Skip phobos.d

                    if (D_IDE_Properties.HasModule(ret, tf)) { if (D_IDEForm.thisForm != null)D_IDEForm.thisForm.Log(tf + " already parsed!"); continue; }

                    try
                    {
                        string tmodule = Path.ChangeExtension(tf, null).Remove(0, dir.Length + 1).Replace('\\', '.');
                        DModule gpf = new DModule(dirProject, tf);
                        gpf.ModuleName = tmodule;

                        D_IDE_Properties.AddFileData(ret, gpf);
                    }
                    catch (Exception ex)
                    {
                        //if (Debugger.IsAttached) throw ex;
                        if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.Log(tf);
                        if (MessageBox.Show(ex.Message + "\n\nStop parsing process?+\n" + ex.Source, "Error at " + tf, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (D_IDEForm.thisForm != null) D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = false;
                            return;
                        }
                    }
                }
            }
            if (D_IDEForm.thisForm != null)
            {
                D_IDEForm.thisForm.Log(D_IDEForm.thisForm.ProgressStatusLabel.Text = "Parsing done!");
                D_IDEForm.thisForm.stopParsingToolStripMenuItem.Enabled = false;
            }
            DModule.ClearErrorLogBeforeParsing = true;
            lock (cc.GlobalModules)
            {
                cc.GlobalModules = ret;

                List<ICompletionData> ilist = new List<ICompletionData>();
                DCodeCompletionProvider.AddGlobalSpaceContent(cc, ref ilist);
                cc.GlobalCompletionList = ilist;
            }
        }

        #region Project relevated things
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
						if (diw.fileData.mod_file == file)
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
            return CreateNewSourceFile(MessageBox.Show("Do you want to add the file to the current project?", "New File",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? prj : null, OpenAfterCreating);
        }

        public string CreateNewSourceFile(DProject Project, bool OpenAfterCreating)
        {
            string tfn = "Untitled.d";

            sF.Filter = "All Files (*.*)|*.*";
            sF.FileName = tfn;
            if (Project != null) sF.InitialDirectory = Project.basedir;
            if (sF.ShowDialog() == DialogResult.OK)
            {
                tfn = sF.FileName;
            }
            else return null;

            if (OpenAfterCreating) Open(tfn, Project != null ? Project.prjfn : "");
            if (Project != null)
            {
                Project.AddSrc(tfn);
                Project.Save();
            }
            prjexplorer.Refresh();

            return tfn;
        }

        #endregion


        /// <summary>
        /// Central accessing method to open files or projects. Use this method only to open files!
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [DebuggerStepThrough()]
        public DocumentInstanceWindow Open(string file)
        {
            if (prj != null && prj.resourceFiles.Contains(prj.GetRelFilePath(file)))
                return Open(prj.GetPhysFilePath(file), ProjectFile);
            return Open(file, "");
        }
        public DocumentInstanceWindow Open(string file, string owner)
        {
            if (prj != null && file == prj.prjfn) return null; // Don't reopen the current project

            DocumentInstanceWindow ret = null;

            foreach (DockContent dc in dockPanel.Documents)
            {
                if (!(dc is DocumentInstanceWindow)) continue;
                DocumentInstanceWindow diw = (DocumentInstanceWindow)dc;
                if (diw.fileData.FileName == file)
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
                if (prj != null)
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

                foreach (string f in prj.lastopen)
                {
                    Open(f, ProjectFile);
                }
                prj.lastopen.Clear();
                ret = SelectedTabPage;
                UpdateFiles();
            }
            else
            {
                DocumentInstanceWindow mtp = new DocumentInstanceWindow(file, owner);

                if (D_IDE_Properties.Default.lastFiles.Contains(file))
                {
                    D_IDE_Properties.Default.lastFiles.Remove(file);
                }
                D_IDE_Properties.Default.lastFiles.Insert(0, file);
                if (D_IDE_Properties.Default.lastFiles.Count > 10) D_IDE_Properties.Default.lastFiles.RemoveAt(10);

                mtp.Show(dockPanel);
                ret = mtp;
            }
            RefreshClassHierarchy();
            UpdateLastFilesMenu();
            if (this.dockPanel.ActiveDocumentPane != null)
                this.dockPanel.ActiveDocumentPane.ContextMenuStrip = this.contextMenuStrip1; // Set Tab selection bars context menu to ours

            // Important: set Read-Only flag if Debugger is running currently
            if (ret != null && ret.txt != null)
                ret.txt.IsReadOnly = IsDebugging;

            UpdateBreakPointsForDocWin(ret);

            return ret;
        }
    }
}