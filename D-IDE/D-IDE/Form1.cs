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
    partial class D_IDEForm : Form
    {
        public D_IDEForm(string[] args)
        {
            thisForm = this;

            Breakpoints = new BreakpointHelper(this);

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
                if (File.Exists(D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.LayoutFile))
                    dockPanel.LoadFromXml(D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.LayoutFile, new DeserializeDockContent(delegate(string s)
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
            startpage.TabPageContextMenuStrip = DocumentWindowContextMenu;
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
            /*
            webclient.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);
            webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
            */
            DBuilder.OnOutput += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnOutput);
            DBuilder.OnError += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnError);
            DBuilder.OnExit += new EventHandler(DBuilder_OnExit);
            //CodeViewToPDB.CodeViewToPDBConverter.Message += new CodeViewToPDB.CodeViewToPDBConverter.MsgHandler(CodeViewToPDBConverter_Message);
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
                //CheckForUpdates();
            }
        }

        #region Building Procedures
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
                args = args.Replace("$objs", "\"" + tp.fileData.mod_file + "\"");
                args = args.Replace("$libs", "");
                args = args.Replace("\"$exe\"","$exe").Replace("$exe", "\"" + exe + "\"");

                try
                {
                    Process p = DBuilder.Exec(Path.IsPathRooted(cc.ExeLinker) ? cc.ExeLinker : (cc.BinDirectory + "\\" + cc.ExeLinker), args, Path.GetDirectoryName(tp.fileData.mod_file), false);
                    if (p != null && !p.WaitForExit(10000))
                    {
                        Log("Execeeded 10 seconds execution time!");
                        p.Kill();
                        return null;
                    }
                }
                catch (Exception ex) { Log(ex.Message); }

                //DBuilder.CreatePDBFromExe(null, exe);
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
                        exeProc = DBuilder.Exec(single_bin, "", Path.GetDirectoryName(single_bin), D_IDE_Properties.Default.ShowExternalConsoleWhenExecuting);
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
                exeProc = DBuilder.Exec(bin, prj.execargs, prj.basedir, D_IDE_Properties.Default.ShowExternalConsoleWhenExecuting); //prj.type == DProject.PrjType.ConsoleApp
                if (exeProc == null) return false;

                exeProc.Exited += delegate(object se, EventArgs ev)
                {
                    dbgStopButtonTS.Enabled = false;
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
        #endregion

        #region File menu

        public void NewSourceFile(object sender, EventArgs e)
        {
            CreateNewSourceFile(true);
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

                string main = prj.basedir + "\\main.d";

                if (!File.Exists(main))
                {
                    File.WriteAllText(main, "import std.stdio, std.cstream;\r\n\r\nvoid main(string[] args)\r\n{\r\n\twriteln(\"Hello World\");\r\n\tdin.getc();\r\n}");

                    prj.AddSrc(main);
                    prj.Save();
                    Open(main);
                }
            }
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

            #region Update the global cache if a file which is located in a global import path gets saved
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
            #endregion

            RefreshClassHierarchy();
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

        public void SaveAll(object sender, EventArgs e)
        {
            SaveAllTabs();
        }

        private void ExitProgramClick(object sender, EventArgs e)
        {
            Close();
        }

        private void AddExistingFile(object sender, EventArgs e)
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

        private void LastProjectsItemClick(object sender, EventArgs e)
        {
            string file = "";
            if (sender is ToolStripMenuItem)
                file = (string)((ToolStripMenuItem)sender).Tag;
            /*else if (sender is RibbonButton)
                file = (string)((RibbonButton)sender).Tag;*/
            Open(file);
        }

        #endregion

        #region Edit menu
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

        public void GotoLine(object sender, EventArgs e)
        {
            if (SelectedTabPage != null) gotoDlg.Visible = true;
        }

        private void formatFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedTabPage == null) return;
            for (int i = 1; i < SelectedTabPage.txt.Document.TotalNumberOfLines; i++)
                SelectedTabPage.txt.Document.FormattingStrategy.IndentLine(SelectedTabPage.txt.ActiveTextAreaControl.TextArea, i);

            SelectedTabPage.ParseFolds();
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

        private void showCompletionWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedTabPage != null) SelectedTabPage.TextAreaKeyEventHandler('\0');
        }
        #endregion

        #region View menu

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

        private void startPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startpage.Show();
        }

        private void errorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            errlog.Show();
        }

        private void debugOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dbgwin.Show();
        }

        private void localsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dbgLocalswin.Show(dockPanel);
        }

        private void reloadProjectTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prjexplorer.UpdateFiles();
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
        #endregion

        #region Project menu
        public void BuildProjectClick(object sender, EventArgs e)
        {
            if (prj != null)
            {
                prj.LastModifyingDates.Clear();
            }
            Build();
        }

        public void BuildSingleClick(object sender, EventArgs e)
        {
            BuildSingle();
        }

        public void RunClick(object sender, EventArgs e)
        {
            Run();
        }

        private void OpenProjectDirectoryInExplorerClick(object sender, EventArgs e)
        {
            if (prj != null)
                Process.Start("explorer.exe", prj.basedir);
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
        #endregion

        // Debug menu is implemented in Form1Debugger.cs

        #region Global menu
        private void GlobalSettingsClick(object sender, EventArgs e)
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
        /// <summary>
        /// Shows code templates UI
        /// </summary>
        private void codeTemplatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dockPanel.DocumentsCount > 0)
                foreach (DockContent dc in dockPanel.Documents)
                    if (dc is CodeTemplates)
                    {
                        dc.Activate();
                        return;
                    }
            (new CodeTemplates()).Show(dockPanel);
        }

        private void ReparseCacheClick(object sender, EventArgs e)
        {
            D_IDE_Properties.Default.dmd1.GlobalModules.Clear();
            D_IDE_Properties.Default.dmd2.GlobalModules.Clear();
            if (updateTh != null && updateTh.ThreadState == System.Threading.ThreadState.Running) return;
            //updateTh = new Thread(delegate()      {
                UpdateChacheThread(D_IDE_Properties.Default.dmd1);
                UpdateChacheThread(D_IDE_Properties.Default.dmd2);
            //});            updateTh.Start();
        }

        private void StopReparsingCacheClick(object sender, EventArgs e)
        {
            if (updateTh != null && updateTh.IsAlive == true)
            {
                updateTh.Abort();
                stopParsingToolStripMenuItem.Enabled = false;
                DModule.ClearErrorLogBeforeParsing = true;
            }
        }
        #endregion

        #region About menu
        private void visitDidesourceforgenetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://d-ide.sourceforge.net");
        }

        private void visitAlexanderbothecomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.alexanderbothe.com");
        }

        private void aboutDIDEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This software is freeware\nand is written by Alexander Bothe.", title);
        }

        private void howToDebugDExecutablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://digitalmars.com/d/2.0/windbg.html");
        }
        #endregion

        #region GUI events and other procs
        public delegate void BuildErrorDelegate(string file, int lineNumber, string errmsg, Color color);

        void DBuilder_OnExit(object sender, EventArgs e)
        {
            Process proc = (Process)sender;
            Log(ProgressStatusLabel.Text = "Process exited with code " + proc.ExitCode.ToString());
        }
        void DLexer_OnError(int line, int col, string message)
        {
            //Log("Text Error in Line "+line.ToString()+", Col "+col.ToString()+": "+message);
        }
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
            mtp.DrawBreakPoints();
            RefreshClassHierarchy();
        }

        private void CloseForm(object sender, FormClosingEventArgs e)
        {
            if (updateTh != null && updateTh.IsAlive)
                updateTh.Abort();

            ForceExitDebugging();

            if (!Directory.Exists(D_IDE_Properties.cfgDir))
                Directory.CreateDirectory(D_IDE_Properties.cfgDir);

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

            if (!File.Exists(D_IDE_Properties.UserDocStorageFile))
            {
                D_IDE_Properties.cfgDir = Application.StartupPath + "\\" + D_IDE_Properties.cfgDirName;
            }
            else
            {
                D_IDE_Properties.cfgDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + D_IDE_Properties.cfgDirName;
            }

            if (!Directory.Exists(D_IDE_Properties.cfgDir))
                DBuilder.CreateDirectoryRecursively(D_IDE_Properties.cfgDir);

            dockPanel.SaveAsXml(D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.LayoutFile);

            D_IDE_Properties.Default.lastFormState = this.WindowState;
            D_IDE_Properties.Default.lastFormLocation = this.Location;
            D_IDE_Properties.Default.lastFormSize = this.Size;

            D_IDE_Properties.Save(D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.prop_file);
            D_IDE_Properties.SaveGlobalCache(D_IDE_Properties.Default.dmd1, D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.D1ModuleCacheFile);
            D_IDE_Properties.SaveGlobalCache(D_IDE_Properties.Default.dmd2, D_IDE_Properties.cfgDir + "\\" + D_IDE_Properties.D2ModuleCacheFile);
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
        #endregion

        #region Updates
        /*
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
        }*/
        #endregion

        #region Document Window Context menu
        private void CloseDocWinClick(object sender, EventArgs e)
        {
            IDockContent[] dcs = dockPanel.DocumentsToArray();

            for (int i = 0; i < dcs.Length; i++)
            {
                if (dcs[i] == dockPanel.ActiveDocument)
                {
                    if (i > 0) (dcs[i - 1] as DockContent).Activate();

                    if (dcs[i] is DocumentInstanceWindow)
                        (dcs[i] as DockContent).Close();
                    else (dcs[i] as DockContent).Hide();
                    break;
                }
            }
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDockContent[] cts = dockPanel.DocumentsToArray();
            foreach (DockContent tp in cts)
            {
                if (tp is StartPage) tp.Hide();
                else
                    tp.Close();
            }
        }

        private void closeAllOthersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDockContent[] dcs = dockPanel.DocumentsToArray();
            for (int i = 0; i < dcs.Length; i++)
            {
                if (dcs[i] == dockPanel.ActiveDocument) continue;
                if (dcs[i] is StartPage) (dcs[i] as DockContent).Hide();
                else
                    (dcs[i] as DockContent).Close();
            }
        }
        #endregion

    }
}
