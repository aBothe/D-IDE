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
    partial class Form1 : Form
    {
        public delegate void BuildErrorDelegate(string file, int lineNumber, string errmsg, Color color);

        public Form1(string[] args)
        {
            thisForm = this;

            Form.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            /*if (D_IDE_Properties.Default.UseRibbonMenu)
            {
                this.MainMenuStrip.Visible = false;
                this.TBS.Visible = false;
                this.dockPanel.Top = 0;
                RibbonSetup=new RibbonSetup(this);
            }*/

            this.WindowState = D_IDE_Properties.Default.lastFormState;
            if (D_IDE_Properties.Default.lastFormState != FormWindowState.Maximized)
            {
                if (D_IDE_Properties.Default.lastFormSize != null)
                    this.Size = D_IDE_Properties.Default.lastFormSize;
                if (D_IDE_Properties.Default.lastFormLocation != null)
                    this.Location = D_IDE_Properties.Default.lastFormLocation;
            }

            #region Load Panel Layout
            dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

            try
            {
                if (File.Exists(Program.cfgDir + "\\" + Program.LayoutFile))
                    dockPanel.LoadFromXml(Program.cfgDir + "\\" + Program.LayoutFile, new DeserializeDockContent(delegate(string s)
                    {
                        if (s == typeof(BuildProcessWin).ToString()) return bpw;
                        else if (s == typeof(OutputWin).ToString()) return output;
                        else if (s == typeof(BreakpointWin).ToString()) return dbgwin;
                        else if (s == typeof(ClassHierarchy).ToString()) return hierarchy;
                        else if (s == typeof(ProjectExplorer).ToString()) return prjexplorer;
                        else if (s == typeof(CallStackWin).ToString()) return callstackwin;
                        else if (s == typeof(ErrorLog).ToString()) return errlog;
                        else if (s == typeof(DebugLocals).ToString()) return dbgLocalswin;
                        else if (s == typeof(PropertyView).ToString() && D_IDE_Properties.Default.EnableFXFormsDesigner) return propView;
                        return null;
                    }));
                else
                {
                    setDefaultPanelLayout(null, EventArgs.Empty);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            output.Text = "Program output";
            output.TabText = "Output";

            hierarchy.hierarchy.ImageList = icons;
            #endregion
            startpage.Show(dockPanel);
            this.Text = title;

            oF.Filter = "All Files|*.*|All Supported (*." + DProject.prjext + ";*.d;*.rc)|" + DProject.prjext +
            ";*.d;*.rc|D-IDE Project files (*" +
            DProject.prjext + ")|*" + DProject.prjext +
            "|D Source file|*.d|Resourcefile|*.rc";

            #region Callbacks
            HostCallbackImplementation.Register();
            HighlightingManager.Manager.AddSyntaxModeFileProvider(new SyntaxFileProvider());

            //DLexer.OnError += DLexer_OnError;

            DParser.OnError += DParser_OnError;
            DParser.OnSemanticError += DParser_OnSemanticError;

            webclient.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);
            webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);

            DBuilder.OnOutput += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnOutput);
            DBuilder.OnError += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnError);
            DBuilder.OnExit += new EventHandler(DBuilder_OnExit);
            CodeViewToPDB.CodeViewToPDBConverter.Message += new CodeViewToPDB.CodeViewToPDBConverter.MsgHandler(CodeViewToPDBConverter_Message);
            DBuilder.OnMessage += new DBuilder.OutputHandler(delegate(DProject p, string file, string m) { Log(m); });
            #endregion

            oF.InitialDirectory = sF.InitialDirectory = D_IDE_Properties.Default.DefaultProjectDirectory;

            #region Open last files/projects

            if (args.Length < 1 && D_IDE_Properties.Default.OpenLastPrj && D_IDE_Properties.Default.lastProjects.Count > 0 && File.Exists(D_IDE_Properties.Default.lastProjects[0]))
                Open(D_IDE_Properties.Default.lastProjects[0]);

            if (D_IDE_Properties.Default.OpenLastFiles)
            {
                foreach (string f in D_IDE_Properties.Default.lastOpenFiles)
                    if (File.Exists(f)) Open(f);
            }

            for (int i = 0; i < args.Length; i++)
                Open(args[i]);

            UpdateLastFilesMenu();
            UpdateFiles();
            #endregion

            // After having loaded and shown everything, close the start screen
            if (!Program.StartScreen.IsDisposed) Program.StartScreen.Close();
            TimeSpan tspan = DateTime.Now - Program.tdate;
            ProgressStatusLabel.Text = "D-IDE launched in " + tspan.TotalSeconds.ToString() + " seconds";
            if (D_IDE_Properties.Default.WatchForUpdates)
            {
                CheckForUpdates();
            }
        }

        void CodeViewToPDBConverter_Message(string Message)
        {
            Log("cv2pdb error: " + Message);
        }

        void DBuilder_OnExit(object sender, EventArgs e)
        {
            Process proc = (Process)sender;
            Log(ProgressStatusLabel.Text = "Process exited with code " + proc.ExitCode.ToString());
        }

        void DLexer_OnError(int line, int col, string message)
        {
            //Log("Text Error in Line "+line.ToString()+", Col "+col.ToString()+": "+message);
        }

        public static string title = "D-IDE " + Application.ProductVersion;

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

        void LastProjectsItemClick(object sender, EventArgs e)
        {
            string file = "";
            if (sender is ToolStripMenuItem)
                file = (string)((ToolStripMenuItem)sender).Tag;
            /*else if (sender is RibbonButton)
                file = (string)((RibbonButton)sender).Tag;*/
            Open(file);
        }

        #region Properties
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
        public static Form1 thisForm;
        protected Process exeProc;
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

        #region Parsing Procedures

        void DParser_OnError(string file, string module, int line, int col, int kindOf, string msg)
        {
            try
            {
                errlog.AddParserError(file, line, col, msg);
                DocumentInstanceWindow mtp;
                foreach (IDockContent dc in dockPanel.Documents)
                {
                    if (dc is DocumentInstanceWindow)
                    {
                        mtp = (DocumentInstanceWindow)dc;
                        if (mtp.fileData.ModuleName == module || mtp.fileData.mod_file == file)
                        {
                            int offset = mtp.txt.Document.PositionToOffset(new TextLocation(col - 1, line - 1));
                            mtp.txt.Document.MarkerStrategy.AddMarker(new TextMarker(offset, 1, TextMarkerType.WaveLine, Color.Red));
                            mtp.txt.ActiveTextAreaControl.Refresh();
                            break;
                        }

                    }
                }
            }
            catch { }
        }
        void DParser_OnSemanticError(string file, string module, int line, int col, int kindOf, string msg)
        {
            try
            {
                errlog.AddParserError(file, line, col, msg);
                DocumentInstanceWindow mtp;
                foreach (IDockContent dc in dockPanel.Documents)
                {
                    if (dc is DocumentInstanceWindow)
                    {
                        mtp = (DocumentInstanceWindow)dc;
                        if (mtp.fileData.ModuleName == module)
                        {
                            int offset = mtp.txt.Document.PositionToOffset(new TextLocation(col - 1, line - 1));
                            mtp.txt.Document.MarkerStrategy.AddMarker(new TextMarker(offset, 1, TextMarkerType.WaveLine, Color.Blue));
                            mtp.txt.ActiveTextAreaControl.Refresh();
                            break;
                        }
                    }
                }
            }
            catch { }
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

            if (Form1.thisForm != null) Form1.thisForm.stopParsingToolStripMenuItem.Enabled = true;
            //bpw.Clear();
            if (Form1.thisForm != null) Form1.thisForm.Log("Reparse all directories");

            foreach (string dir in cc.ImportDirectories)
            {
                DProject dirProject = new DProject();
                dirProject.basedir = dir;

                if (Form1.thisForm != null) Form1.thisForm.Log("Parse directory " + dir);
                if (!Directory.Exists(dir))
                {
                    if (Form1.thisForm != null) Form1.thisForm.Log("Directory \"" + dir + "\" does not exist!");
                    continue;
                }
                string[] files = Directory.GetFiles(dir, "*.d?", SearchOption.AllDirectories);
                foreach (string tf in files)
                {
                    if (tf.EndsWith("phobos.d")) continue; // Skip phobos.d

                    if (D_IDE_Properties.HasModule(ret, tf)) { if (Form1.thisForm != null)Form1.thisForm.Log(tf + " already parsed!"); continue; }

                    try
                    {
                        string tmodule = Path.ChangeExtension(tf, null).Remove(0, dir.Length + 1).Replace('\\', '.');
                        DModule gpf = new DModule(dirProject, tf);
                        gpf.ModuleName = tmodule;

                        D_IDE_Properties.AddFileData(ret, gpf);
                    }
                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached) throw ex;
                        if (Form1.thisForm != null) Form1.thisForm.Log(tf);
                        if (MessageBox.Show(ex.Message + "\n\nStop parsing process?+\n\n\n" + ex.StackTrace, "Error at " + tf, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (Form1.thisForm != null) Form1.thisForm.stopParsingToolStripMenuItem.Enabled = false;
                            return;
                        }
                    }
                }
            }
            if (Form1.thisForm != null)
            {
                Form1.thisForm.Log(Form1.thisForm.ProgressStatusLabel.Text = "Parsing done!");
                Form1.thisForm.stopParsingToolStripMenuItem.Enabled = false;
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

        Thread updateTh;
        private void updateCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            D_IDE_Properties.Default.dmd1.GlobalModules.Clear();
            D_IDE_Properties.Default.dmd2.GlobalModules.Clear();
            if (updateTh != null && updateTh.ThreadState == System.Threading.ThreadState.Running) return;
            updateTh = new Thread(delegate()
            {
                UpdateChacheThread(D_IDE_Properties.Default.dmd1);
                UpdateChacheThread(D_IDE_Properties.Default.dmd2);
            });
            updateTh.Start();
        }

        private void stopParsingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (updateTh != null && updateTh.IsAlive == true)
            {
                updateTh.Abort();
                stopParsingToolStripMenuItem.Enabled = false;
                DModule.ClearErrorLogBeforeParsing = true;
            }
        }

        #endregion

        #region Building Procedures

        public void BuildSingle(object sender, EventArgs e)
        {
            BuildSingle();
        }

        public string BuildSingle()
        {
            UseOutput = false;
            CompilerConfiguration cc = D_IDE_Properties.Default.DefaultCompiler;
            if (SelectedTabPage != null)
            {
                bpw.Show();
                DocumentInstanceWindow tp = SelectedTabPage;
                if (!DModule.Parsable(tp.fileData.mod_file))
                {
                    if (tp.fileData.mod_file.EndsWith(".rc"))
                    {
                        DBuilder.BuildResFile(
                            tp.fileData.mod_file, cc,
                            Path.ChangeExtension(tp.fileData.mod_file, ".res"),
                            Path.GetDirectoryName(tp.fileData.mod_file)
                            );
                        return Path.ChangeExtension(tp.fileData.mod_file, ".res");
                    }
                    MessageBox.Show("Can only build .d or .rc source files!");
                    return null;
                }
                string exe = Path.ChangeExtension(tp.fileData.mod_file, ".exe");

                Log("Build single " + tp.fileData.mod_file + " to " + exe);
                string args = D_IDE_Properties.Default.DefaultCompiler.ExeLinkerDebugArgs;
                args = args.Replace("$objs", tp.fileData.mod_file);
                args = args.Replace("$libs", "");
                args = args.Replace("$exe", exe);
                
				try{
				Process p= DBuilder.Exec(Path.IsPathRooted(cc.ExeLinker) ? cc.ExeLinker : (cc.BinDirectory + "\\" + cc.ExeLinker), args, Path.GetDirectoryName(tp.fileData.mod_file), true);
				if (p != null && !p.WaitForExit(10000))
				{
					Log("Execeeded 10 seconds execution time!");
					p.Kill();
					return null;
				}
				}catch(Exception ex){Log(ex.Message);}

                DBuilder.CreatePDBFromExe(null, exe);
                return exe;
            }
            return null;
        }

        /// <summary>
        /// Builds the current project and returns the path of the target file
        /// </summary>
        /// <returns></returns>
        public string Build()
        {
            bpw.Clear();
            errlog.buildErrors.Clear();
            errlog.Update();
            if (D_IDE_Properties.Default.DoAutoSaveOnBuilding) SaveAllTabs();
            /*
            foreach (DockContent tp in dockPanel.Documents)
            {
                if (tp is DocumentInstanceWindow)
                {
                    DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
                    //mtp.txt.Document.CustomLineManager.Clear();
                    mtp.txt.ActiveTextAreaControl.Refresh();
                }
            }*/

            UseOutput = false;

            if (SelectedTabPage != null && (prj == null || prj.prjfn == ""))
            {
                return BuildSingle();
            }
            if (prj == null)
            {
                MessageBox.Show("Create project first!");
                return null;
            }

            if (D_IDE_Properties.Default.LogBuildProgress) bpw.Show();

            return DBuilder.BuildProject(prj);
        }

        bool Run()
        {
            ForceExitDebugging();

            output.Clear();
            UseOutput = true;
            if (prj == null)
            {
                if (SelectedTabPage != null)
                {
                    string single_bin = Path.ChangeExtension(SelectedTabPage.fileData.mod_file, ".exe");
                    if (File.Exists(single_bin))
                    {
                        output.Show();
                        exeProc = DBuilder.Exec(single_bin, "", Path.GetDirectoryName(single_bin), true);
                        exeProc.Exited += delegate(object se, EventArgs ev)
                        {
                            dbgStopButtonTS.Enabled = false;
                            Log("Process exited with code " + exeProc.ExitCode.ToString());
                        };
                        exeProc.EnableRaisingEvents = true;
                        dbgStopButtonTS.Enabled = true;
                        return true;
                    }
                }
                MessageBox.Show("Create project first!");
                return false;
            }
            if (prj.type != DProject.PrjType.ConsoleApp && prj.type != DProject.PrjType.WindowsApp)
            {
                MessageBox.Show("Unable to execute a library!");
                return false;
            }
            string bin = prj.LastBuiltTarget;

            if (File.Exists(bin))
            {
                if (prj.type == DProject.PrjType.ConsoleApp) output.Show();

                output.Log("Executing " + prj.targetfilename);
                exeProc = DBuilder.Exec(bin, prj.execargs, prj.basedir, true); //prj.type == DProject.PrjType.ConsoleApp
                if (exeProc == null) return false;

                exeProc.Exited += delegate(object se, EventArgs ev)
                {
                    dbgStopButtonTS.Enabled = false;
                    DBuilder_OnExit(se, ev);
                };
                exeProc.EnableRaisingEvents = true;
                dbgStopButtonTS.Enabled = true;


                return true;
            }
            else
            {
                output.Log("File " + bin + " not exists!");
            }
            return false;
        }

        private void SendInputToExeProc(object sender, EventArgs e)
        {
            if (exeProc == null || exeProc.HasExited) return;

            InputDlg dlg = new InputDlg();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                exeProc.StandardInput.WriteLine(dlg.InputString);
            }
        }

        public void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (prj != null)
            {
                prj.LastModifyingDates.Clear();
            }
            Build();
        }

        private void toggleBuildLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!bpw.Visible) bpw.Show();
            else bpw.Visible = false;
        }

        public void buildRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Build()))
                Run();
        }

        public void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run();
        }

        void DBuilder_OnOutput(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            Log(e.Data);
        }

        void DBuilder_OnError(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (!UseOutput) { errlog.Show(); }
            else output.Show();

            int to = e.Data.IndexOf(".d(");
            if (to < 0)
            {
                Log("Error: " + e.Data);
            }
            else
            {
                to += 2;
                string mod_file = e.Data.Substring(0, to);
                to += 1;
                int to2 = e.Data.IndexOf("):", to);
                if (to2 < 0) return;
                int lineNumber = Convert.ToInt32(e.Data.Substring(to, to2 - to));
                string errmsg = e.Data.Substring(to2 + 2);

                Invoke(
                    new BuildErrorDelegate(
                        AddHighlightedBuildError),
                        new Object[] { mod_file, lineNumber, errmsg, Color.LightPink });
            }
        }


        /// <summary>
        /// Add an error notification to the error list and set this to the current selection in the editor. If needed, the specific file gets opened automatically
        /// </summary>
        /// <param name="file"></param>
        /// <param name="lineNumber"></param>
        void AddHighlightedBuildError(string file, int lineNumber, string errmsg, Color color)
        {
            /*if (SelectedTabPage != null)
            {
                foreach (DockContent tp in dockPanel.Documents)
                {
                    if (tp is DocumentInstanceWindow)
                    {
                        DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
                        if (mtp.fileData.mod_file != file) continue;
                        //mtp.txt.Document.CustomLineManager.AddCustomLine(lineNumber - 1, lineNumber - 1, color, false);
                        mtp.txt.ActiveTextAreaControl.Refresh();
                    }
                }
            }*/
            errlog.AddBuildError(file, lineNumber, errmsg);
        }

        #endregion

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

        public void NewSourceFile(object sender, EventArgs e)
        {
            string tfn = "Untitled.d";
            bool add = sender is ProjectExplorer || (prj != null ? (MessageBox.Show("Do you want to add the file to the current project?", "New File",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes) : false);

            sF.Filter = "All Files (*.*)|*.*";
            sF.FileName = tfn;
            if (prj != null && add) sF.InitialDirectory = prj.basedir;
            if (sF.ShowDialog() == DialogResult.OK)
            {
                tfn = sF.FileName;
            }
            else return;

            DocumentInstanceWindow mtp = new DocumentInstanceWindow(tfn,
            DModule.Parsable(tfn) ? "import std.stdio;\r\n\r\nvoid main(string[] args)\r\n{\r\n\twriteln(\"Hello World\");\r\n}" : "",
            add ? prj.prjfn : "");
            mtp.Modified = true;
            mtp.Save();
            if (add) prj.AddSrc(tfn);

            mtp.Show(dockPanel);

            if (D_IDE_Properties.Default.lastFiles.Contains(tfn))
            {
                D_IDE_Properties.Default.lastFiles.Remove(tfn);
            }
            D_IDE_Properties.Default.lastFiles.Insert(0, tfn);
            if (D_IDE_Properties.Default.lastFiles.Count > 10) D_IDE_Properties.Default.lastFiles.RemoveAt(10);

            if (prj != null) prj.Save();
            UpdateLastFilesMenu();
            UpdateFiles();
            prjexplorer.Refresh();
        }

        public void NewProject(object sender, EventArgs e)
        {
            SaveAllTabs();

            NewPrj np = new NewPrj();
            if (np.ShowDialog() == DialogResult.OK)
            {
                ProjectFile = np.prj.prjfn;
                D_IDE_Properties.Projects[ProjectFile] = np.prj;
                Text = prj.name + " - " + title;
                SaveAllTabs();

                if (D_IDE_Properties.Default.lastProjects.Contains(ProjectFile))
                    D_IDE_Properties.Default.lastProjects.Remove(ProjectFile);
                D_IDE_Properties.Default.lastProjects.Insert(0, ProjectFile);
                if (D_IDE_Properties.Default.lastProjects.Count > 10)
                    D_IDE_Properties.Default.lastProjects.RemoveAt(10);

                UpdateLastFilesMenu();
                UpdateFiles();
            }
        }

        public void SaveFile(object sender, EventArgs e)
        {
            DocumentInstanceWindow mtp = SelectedTabPage;
            if (mtp == null)
            {
                if (thisForm.dockPanel.ActiveDocument is FXFormsDesigner)
                {
                    FXFormsDesigner fd = dockPanel.ActiveDocument as FXFormsDesigner;
                    if (!fd.Save()) return;

                    mtp = FileDataByFile(fd.FileName);
                    if (mtp != null) mtp.Reload();
                }
                else
                    return;
            }
            else
            {
                mtp.Save();
                if (!mtp.fileData.IsParsable) return;
                mtp.ParseFromText(); // Reparse after save

                FXFormsDesigner fd = FXFormDesignerByFile(mtp.fileData.FileName);
                if (fd != null)
                {
                    fd.Reload();
                }
            }

            if (mtp == null) return;

            CompilerConfiguration cc = D_IDE_Properties.Default.dmd1;
        goonwithcc:
            foreach (string dir in cc.ImportDirectories)
            {
                if (mtp.fileData.mod_file.StartsWith(dir))
                {
                    D_IDE_Properties.AddFileData(cc, mtp.fileData);

                    List<ICompletionData> ilist = new List<ICompletionData>();
                    DCodeCompletionProvider.AddGlobalSpaceContent(cc, ref ilist);
                    cc.GlobalCompletionList = ilist;

                    break;
                }
            }
            bool tb = cc == D_IDE_Properties.Default.dmd2;
            cc = D_IDE_Properties.Default.dmd2;
            if (!tb) goto goonwithcc;

            RefreshClassHierarchy();
        }

        private void RefreshClassHierarchy()
        {
            DocumentInstanceWindow mtp = SelectedTabPage;
            if (mtp == null) return;
            TreeNode oldNode = null;
            if (hierarchy.hierarchy.Nodes.Count > 0) oldNode = hierarchy.hierarchy.Nodes[0];
            hierarchy.hierarchy.Nodes.Clear();

            hierarchy.hierarchy.BeginUpdate();
            TreeNode tn = new TreeNode(mtp.fileData.ModuleName);
            tn.SelectedImageKey = tn.ImageKey = "namespace";
            int i = 0;
            foreach (DataType ch in mtp.fileData.dom)
            {
                TreeNode ctn = GenerateHierarchyData(mtp.fileData.dom, ch,
                    (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == ch.name) ?
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

        public void OpenFile(object sender, EventArgs e)
        {
            SaveAllTabs();

            if (oF.ShowDialog() == DialogResult.OK)
            {
                foreach (string fn in oF.FileNames) Open(fn);
                UpdateFiles();
            }
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
            if (this.dockPanel.ActiveDocumentPane != null) this.dockPanel.ActiveDocumentPane.ContextMenuStrip = this.contextMenuStrip1; // Set Tab selection bars context menu to ours
            return ret;
        }

        #endregion

        TreeNode GenerateHierarchyData(DataType env, DataType ch, TreeNode oldNode)
        {
            if (ch == null) return null;
            int ii = DCompletionData.GetImageIndex(icons, env, ch);

            TreeNode ret = new TreeNode(ch.name, ii, ii);
            ret.Tag = ch;
            ret.SelectedImageIndex = ret.ImageIndex = ii;

            ii = icons.Images.IndexOfKey("Icons.16x16.Parameter.png");
            int i = 0;
            foreach (DataType dt in ch.param)
            {
                TreeNode tn = GenerateHierarchyData(ch, dt,
                    (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == dt.name) ?
                    oldNode.Nodes[i] : null);
                tn.SelectedImageIndex = tn.ImageIndex = ii;
                tn.ToolTipText = DCompletionData.BuildDescriptionString(dt);

                ret.Nodes.Add(tn);
                i++;
            }
            i = 0;
            foreach (DataType dt in ch)
            {
                ii = DCompletionData.GetImageIndex(icons, ch, dt);
                TreeNode tn = GenerateHierarchyData(ch, dt,
                    (oldNode != null && oldNode.Nodes.Count >= i + 1 && oldNode.Nodes[i].Text == dt.name) ?
                    oldNode.Nodes[i] : null);

                tn.ToolTipText = DCompletionData.BuildDescriptionString(dt);
                ret.Nodes.Add(tn);
                i++;
            }
            if (oldNode != null && oldNode.IsExpanded && oldNode.Text == ch.name)
                ret.Expand();
            return ret;
        }

        public void Log(string m)
        {
            if (!UseOutput) bpw.Log(m);
            else output.Log(m);
        }

        #region GUI actions

        public static ImageList InitCodeCompletionIcons()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            icons = new ImageList();
            // 
            // icons
            // 
            icons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("icons.ImageStream")));
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

        private void setDefaultPanelLayout(object sender, EventArgs e)
        {
            hierarchy.Show(dockPanel, DockState.DockRight);
            prjexplorer.Show(dockPanel, DockState.DockLeft);
            dbgwin.Show(dockPanel, DockState.DockBottomAutoHide);
            bpw.Show(dockPanel, DockState.DockBottomAutoHide);
            output.Show(dockPanel, DockState.DockBottomAutoHide);
            errlog.Show(dockPanel, DockState.DockBottom);
            callstackwin.Show(dockPanel, DockState.DockBottomAutoHide);
            dbgLocalswin.Show(dockPanel, DockState.DockBottomAutoHide);
            if (D_IDE_Properties.Default.EnableFXFormsDesigner) propView.Show(dockPanel, DockState.DockRight);
        }

        private void TabSelectionChanged(object sender, EventArgs e)
        {
            DocumentInstanceWindow mtp = SelectedTabPage;
            if (mtp == null)
            {
                hierarchy.hierarchy.Nodes.Clear();
                return;
            }
            searchDlg.currentOffset = 0;
            mtp.ParseFolds();
            RefreshClassHierarchy();
        }

        private void CloseForm(object sender, FormClosingEventArgs e)
        {
            if (updateTh != null && updateTh.IsAlive)
                updateTh.Abort();

            ForceExitDebugging();

            if (!Directory.Exists(Program.cfgDir))
                Directory.CreateDirectory(Program.cfgDir);

            #region Save all edited projects and store all opened files into an array
            D_IDE_Properties.Default.lastOpenFiles.Clear();

            List<DProject> changedPrjs = new List<DProject>();
            foreach (DockContent tp in dockPanel.Documents)
            {
                if (tp is DocumentInstanceWindow)
                {
                    DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;

                    #region Add file to last open files
                    string physfile = mtp.fileData.FileName;
                    if (mtp.project != null) mtp.project.GetPhysFilePath(physfile);
                    D_IDE_Properties.Default.lastOpenFiles.Add(physfile);
                    #endregion

                    if (mtp.project != null)
                    {
                        mtp.project.lastopen.Add(mtp.fileData.FileName);
                        if (!changedPrjs.Contains(mtp.project))
                            changedPrjs.Add(mtp.project);
                    }
                }
            }

            foreach (DProject p in changedPrjs)
            {
                try
                {
                    p.Save();
                }
                catch { }
            }
            #endregion

            if (!File.Exists(Program.UserDocStorageFile))
            {
                Program.cfgDir = Application.StartupPath + "\\" + Program.cfgDirName;
            }
            else
            {
                Program.cfgDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Program.cfgDirName;
            }

            if (!Directory.Exists(Program.cfgDir))
                DBuilder.CreateDirectoryRecursively(Program.cfgDir);

            dockPanel.SaveAsXml(Program.cfgDir + "\\" + Program.LayoutFile);

            D_IDE_Properties.Default.lastFormState = this.WindowState;
            D_IDE_Properties.Default.lastFormLocation = this.Location;
            D_IDE_Properties.Default.lastFormSize = this.Size;

            D_IDE_Properties.Save(Program.cfgDir + "\\" + Program.prop_file);
            D_IDE_Properties.SaveGlobalCache(D_IDE_Properties.Default.dmd1, Program.cfgDir + "\\" + Program.D1ModuleCacheFile);
            D_IDE_Properties.SaveGlobalCache(D_IDE_Properties.Default.dmd2, Program.cfgDir + "\\" + Program.D2ModuleCacheFile);
        }

        public void SaveAs(object sender, EventArgs e)
        {
            DocumentInstanceWindow tp = SelectedTabPage;
            if (tp == null) return;
            string bef = tp.fileData.FileName;
            sF.FileName = bef;

            if (sF.ShowDialog() == DialogResult.OK)
            {
                if (prj != null)
                {
                    if (DModule.Parsable(tp.fileData.FileName))
                    {
                        if (prj.files.Contains(tp.fileData))
                            prj.files.Remove(tp.fileData);
                    }

                    prj.resourceFiles.Remove(prj.GetRelFilePath(tp.fileData.FileName));
                    prj.AddSrc(sF.FileName);
                }
                tp.fileData.FileName = sF.FileName;
                tp.Update();
                tp.Save();
            }
        }

        private void ShowProjectProperties(object sender, EventArgs e)
        {
            if (prj == null)
            {
                MessageBox.Show("Create project first!");
                return;
            }
            foreach (DockContent dc in dockPanel.Documents)
            {
                if (dc is ProjectPropertyPage)
                {
                    if ((dc as ProjectPropertyPage).project.prjfn == prj.prjfn) return;
                }
            }
            ProjectPropertyPage ppp = new ProjectPropertyPage(prj);
            if (ppp != null)
                ppp.Show(dockPanel);
        }

        public void AddExistingFile(object sender, EventArgs e)
        {
            if (prj == null) return;

            if (oF.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in oF.FileNames)
                {
                    if (Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

                    prj.AddSrc(file);
                }
                UpdateFiles();
            }
        }

        private void AddExistingDirectory(object sender, EventArgs e)
        {
            if (prj == null) return;

            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = prj.basedir;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                DialogResult dr = MessageBox.Show("Also scan subdirectories?", "Add folder", MessageBoxButtons.YesNoCancel);

                if (dr == DialogResult.Cancel) 
                    return;

                prj.AddDirectory(fb.SelectedPath, dr == DialogResult.Yes);
            }
        }

        private void CloseTab(object sender, EventArgs e)
        {
            DocumentInstanceWindow mtp = SelectedTabPage;
            if (mtp == null) return;

            mtp.Save();
            mtp.Close();
        }

        private void CloseAllOtherTabs(object sender, EventArgs e)
        {
            SaveAllTabs();
            foreach (DockContent tp in dockPanel.Documents)
            {
                if (tp == SelectedTabPage) continue;
                tp.Close();
            }
        }

        private void propertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dockPanel.DocumentsCount > 0)
                foreach (DockContent dc in dockPanel.Documents)
                    if (dc is IDESettings)
                    {
                        dc.Activate();
                        return;
                    }
            (new IDESettings()).Show(dockPanel);
        }

        public void RemoveFileFromPrj(string file)
        {
            if (prj == null) return;

            prj.resourceFiles.Remove(file);

            UpdateFiles();
        }

        #region Search & Replace
        public void searchReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedTabPage != null)
            {
                if (SelectedTabPage.txt.ActiveTextAreaControl.SelectionManager.SelectedText.Length > 0)
                    searchDlg.searchText = SelectedTabPage.txt.ActiveTextAreaControl.SelectionManager.SelectedText;
                searchDlg.Visible = true;
            }
        }

        public void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchDlg.FindNextClick(sender, e);
        }

        public void searchTool_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && SelectedTabPage != null && searchTool.TextBox.Text.Length > 0)
            {
                searchDlg.Search(searchTool.TextBox.Text);
            }
        }
        #endregion

        public void GotoLine(object sender, EventArgs e)
        {
            if (SelectedTabPage != null) gotoDlg.Visible = true;
        }

        private void tc_DragDrop(object sender, DragEventArgs e)
        {
            foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop))
                Open(file);
        }

        private void tc_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void formatFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedTabPage == null) return;
            for (int i = 1; i < SelectedTabPage.txt.Document.TotalNumberOfLines; i++)
                SelectedTabPage.txt.Document.FormattingStrategy.IndentLine(SelectedTabPage.txt.ActiveTextAreaControl.TextArea, i);

            SelectedTabPage.ParseFolds();
        }
        #endregion

        #region Updates

        public static WebClient webclient = new WebClient();
        public Thread RevisionUpdateThread;
        public void CheckForUpdates()
        {
            if ((RevisionUpdateThread != null && RevisionUpdateThread.IsAlive) || webclient.IsBusy) return;
            RevisionUpdateThread = new Thread(delegate()
                    {
                        try
                        {
                            webclient.DownloadStringAsync(new Uri(Program.ver_txt), Program.ver_txt);
                        }
                        catch (Exception ex) { MessageBox.Show(ex.Message); }
                    });
            RevisionUpdateThread.Start();
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Cancelled || Program.ver_txt != (string)e.UserState) return;
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                return;
            }
            try
            {
                if (new Version(Application.ProductVersion) < new Version(e.Result))
                {
                    if (MessageBox.Show("A higher version is available. Download it?", e.Result + " available", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        DownloadLatestRevision(this, EventArgs.Empty);
                    }
                }
            }
            catch
            {
                if (MessageBox.Show("Another version is available. Download it?", e.Result + " available", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DownloadLatestRevision(this, EventArgs.Empty);
                }
            }
        }

        void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Error: " + e.Error.Message);
                return;
            }
            MessageBox.Show(Path.GetFileName((string)e.UserState) + " successfully downloaded!");
            ProgressStatusLabel.Text = "Ready";
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void DownloadLatestRevision(object sender, EventArgs e)
        {
            if (webclient.IsBusy)
            {
                return;
            }
            SaveFileDialog upd_sf = new SaveFileDialog();

            upd_sf.FileName = "D-IDE.tar.gz";
            upd_sf.InitialDirectory = Application.StartupPath;
            upd_sf.Filter = "All (*.*)|*.*";
            upd_sf.DefaultExt = ".tar.gz";
            //upd_sf.AutoUpgradeEnabled = true;
            upd_sf.CheckPathExists = true;
            upd_sf.SupportMultiDottedExtensions = true;
            upd_sf.OverwritePrompt = true;

            if (upd_sf.ShowDialog() == DialogResult.OK)
            {
                ProgressStatusLabel.Text = "Downloading D-IDE.tar.gz";
                try
                {
                    webclient.DownloadFileAsync(new Uri("http://d-ide.svn.sourceforge.net/viewvc/d-ide/D-IDE/D-IDE/bin/Debug.tar.gz?view=tar"), upd_sf.FileName, upd_sf.FileName);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void visitAlexanderbothecomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.alexanderbothe.com");
        }
        #endregion

        private void projectExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prjexplorer.Show(dockPanel);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            hierarchy.Show(dockPanel);
        }

        private void toggleBuildLogToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            bpw.Show(dockPanel);
        }

        private void outputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            output.Show(dockPanel);
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (dockPanel.ActiveDocument as DockContent).Close();
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDockContent[] cts = dockPanel.DocumentsToArray();
            foreach (DockContent tp in cts)
                tp.Close();
        }

        private void closeAllOthersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDockContent[] dcs = dockPanel.DocumentsToArray();
            for (int i = 0; i < dcs.Length; i++)
            {
                if (dcs[i] == dockPanel.ActiveDocument) continue;
                (dcs[i] as DockContent).Close();
            }
        }

        private void aboutDIDEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This software is freeware\nand is written by Alexander Bothe.", title);
        }

        public void SaveAll(object sender, EventArgs e)
        {
            SaveAllTabs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void startPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startpage.Show();
        }

        private void doubleLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DocumentInstanceWindow diw = null;
            if ((diw = SelectedTabPage) == null) return;

            string sel = diw.txt.ActiveTextAreaControl.SelectionManager.SelectedText;
            if (String.IsNullOrEmpty(sel))
            {
                int line = diw.Caret.Line;
                LineSegment ls = diw.txt.Document.GetLineSegmentForOffset(diw.CaretOffset);
                diw.txt.Document.Insert(
                    diw.txt.Document.PositionToOffset(new TextLocation(0, line + 1)),
                    diw.txt.Document.TextContent.Substring(ls.Offset, ls.Length) + "\r\n"
                    );
            }
            else
            {
                ISelection isel = diw.txt.ActiveTextAreaControl.SelectionManager.SelectionCollection[0];
                diw.txt.Document.Insert(isel.EndOffset, isel.SelectedText);
            }
            diw.Refresh();
        }

        private void openProjectDirectoryInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (prj != null)
                Process.Start("explorer.exe", prj.basedir);
        }

        private void errorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            errlog.Show();
        }

        private void howToDebugDExecutablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://digitalmars.com/d/2.0/windbg.html");
        }

        private void debugOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dbgwin.Show();
        }
        #region Basics
        public void cutTBSButton_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedTabPage.EmulateCut();
            }
            catch { }
        }

        public void copyTBSButton_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedTabPage.EmulateCopy();
            }
            catch { }
        }

        public void pasteTBSButton_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedTabPage.EmulatePaste();
            }
            catch { }
        }
        #endregion

        private void visitDidesourceforgenetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://d-ide.sourceforge.net");
        }

        private void showCompletionWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedTabPage != null) SelectedTabPage.TextAreaKeyEventHandler('\0');
        }

        private void reloadProjectTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prjexplorer.UpdateFiles();
        }

        private void localsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dbgLocalswin.Show(dockPanel);
        }

        private void executeDebugCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsDebugging) return;
            InputDlg id = new InputDlg();
            if (id.ShowDialog() == DialogResult.OK)
            {
                dbg.Execute(id.InputString);
                dbg.WaitForEvent(3000);
            }
        }

        #region Commenting
        private void commentOutBlockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DocumentInstanceWindow diw = SelectedTabPage;
            if (diw != null) diw.CommentOutBlock(sender, e);
        }

        private void uncommentBlocklineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DocumentInstanceWindow diw = SelectedTabPage;
            if (diw != null) diw.UncommentBlock(sender, e);
        }
        #endregion
    }
}
