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
using D_Parser.CodeCompletion;
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

namespace D_IDE
{
	partial class Form1 : Form
	{
		public Form1(string[] args)
		{
			thisForm = this;

			Form.CheckForIllegalCrossThreadCalls = false;
			InitializeComponent();

			this.WindowState = D_IDE_Properties.Default.lastFormState;
			if (D_IDE_Properties.Default.lastFormState != FormWindowState.Maximized)
			{
				if (D_IDE_Properties.Default.lastFormSize != null)
					this.Size = D_IDE_Properties.Default.lastFormSize;
				if (D_IDE_Properties.Default.lastFormLocation != null)
					this.Location = D_IDE_Properties.Default.lastFormLocation;
			}

			dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

			Debugger.Log(0, "notice", "Load form layouts");

			#region Load Panel Layout
			try
			{
				if (File.Exists(Program.LayoutFile))
					dockPanel.LoadFromXml(Program.LayoutFile, new DeserializeDockContent(delegate(string s)
					{
						if (s == typeof(BuildProcessWin).ToString()) return bpw;
						else if (s == typeof(OutputWin).ToString()) return output;
						else if (s == typeof(BreakpointWin).ToString()) return dbgwin;
						else if (s == typeof(ClassHierarchy).ToString()) return hierarchy;
						else if (s == typeof(ProjectExplorer).ToString()) return prjexplorer;
						else if (s == typeof(CallStackWin).ToString()) return callstackwin;
						else if (s == typeof(ErrorLog).ToString()) return errlog;
						//else if(s == typeof(PropertyView).ToString())	return propView;
						return null;
					}));
			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }

			try
			{
				if (!hierarchy.Visible) hierarchy.Show(dockPanel, DockState.DockRight);
				if (!prjexplorer.Visible) prjexplorer.Show(dockPanel, DockState.DockLeft);
				if (!dbgwin.Visible) dbgwin.Show(dockPanel, DockState.DockBottomAutoHide);
				if (!bpw.Visible) bpw.Show(dockPanel, DockState.DockBottomAutoHide);
				if (!output.Visible) output.Show(dockPanel, DockState.DockBottomAutoHide);
				if (!errlog.Visible) errlog.Show(dockPanel, DockState.DockBottom);
				if (!callstackwin.Visible) callstackwin.Show(dockPanel, DockState.DockBottomAutoHide);
				//if(!propView.Visible)propView.Show(dockPanel);
			}
			catch { }
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

			Debugger.Log(0, "notice", "Register callbacks");

			HostCallbackImplementation.Register();
			HighlightingManager.Manager.AddSyntaxModeFileProvider(new SyntaxFileProvider());

			//DLexer.OnError += DLexer_OnError;

			DParser.OnError += DParser_OnError;
			DParser.OnSemanticError += DParser_OnSemanticError;

			webclient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged);
			webclient.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);

			DBuilder.OnOutput += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnOutput);
			DBuilder.OnError += new System.Diagnostics.DataReceivedEventHandler(DBuilder_OnError);
			DBuilder.OnExit += new EventHandler(DBuilder_OnExit);
			DBuilder.OnMessage += new DBuilder.OutputHandler(delegate(DProject p, string file, string m) { Log(m); });

			oF.InitialDirectory = sF.InitialDirectory = D_IDE_Properties.Default.DefaultProjectDirectory;

			Debugger.Log(0, "notice", "Open last files/projects");

			if (args.Length < 1 && D_IDE_Properties.Default.OpenLastPrj && D_IDE_Properties.Default.lastProjects.Count > 0)
				Open(D_IDE_Properties.Default.lastProjects[0]);

			for (int i = 0; i < args.Length; i++)
				Open(args[i]);

			if (args.Length < 1)
			{
				UpdateLastFilesMenu();
				UpdateFiles();
			}

			//(new FXFormsDesigner()).Show(dockPanel,DockState.Document);

			//if (D_IDE_Properties.Default.WatchForUpdates) CheckForUpdates();
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
			lastOpenProjects.DropDownItems.Clear();
			lastOpenFiles.DropDownItems.Clear();
			if (D_IDE_Properties.Default.lastProjects == null) D_IDE_Properties.Default.lastProjects = new List<string>();
			if (D_IDE_Properties.Default.lastFiles == null) D_IDE_Properties.Default.lastFiles = new List<string>();
			startpage.lastProjects.Items.Clear();
			startpage.lastFiles.Items.Clear();
			foreach (string prjfile in D_IDE_Properties.Default.lastProjects)
			{
				if (File.Exists(prjfile))
				{/*
					RibbonItem ri=new RibbonOrbRecentItem();
					ri.Tag = prjfile;
					ri.Text = Path.GetFileName(prjfile);
					ri.Click += new EventHandler(LastProjectsItemClick);
					
					RibbonMenu.OrbDropDown.RecentItems.Add(ri);*/

					ToolStripMenuItem tsm = new ToolStripMenuItem();
					tsm.Tag = prjfile;
					tsm.Text = Path.GetFileName(prjfile);
					tsm.Click += new EventHandler(LastProjectsItemClick);
					lastOpenProjects.DropDownItems.Add(tsm);
					startpage.lastProjects.Items.Add(tsm.Text);
				}
			}

			foreach (string file in D_IDE_Properties.Default.lastFiles)
			{
				if (File.Exists(file))
				{
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

		void LastProjectsItemClick(object sender, EventArgs e)
		{
			string file = "";
			if (sender is ToolStripMenuItem)
				file = (string)((ToolStripMenuItem)sender).Tag;
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
		public bool UseOutput = false;

		public DProject prj;
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
			errlog.AddParserError(file, line, col, msg);
			DocumentInstanceWindow mtp;
			foreach (IDockContent dc in dockPanel.Documents)
			{
				if (dc is DocumentInstanceWindow)
				{
					mtp = (DocumentInstanceWindow)dc;
					if (mtp.fileData.mod == module || mtp.fileData.mod_file == file)
					{
						int offset = mtp.txt.Document.PositionToOffset(new TextLocation(col - 1, line - 1));
						mtp.txt.Document.MarkerStrategy.AddMarker(new TextMarker(offset, 1, TextMarkerType.WaveLine, Color.Red));
						mtp.txt.ActiveTextAreaControl.Refresh();
						break;
					}

				}
			}
		}
		void DParser_OnSemanticError(string file, string module, int line, int col, int kindOf, string msg)
		{
			errlog.AddParserError(file, line, col, msg);
			DocumentInstanceWindow mtp;
			foreach (IDockContent dc in dockPanel.Documents)
			{
				if (dc is DocumentInstanceWindow)
				{
					mtp = (DocumentInstanceWindow)dc;
					if (mtp.fileData.mod == module)
					{
						int offset = mtp.txt.Document.PositionToOffset(new TextLocation(col - 1, line - 1));
						mtp.txt.Document.MarkerStrategy.AddMarker(new TextMarker(offset, 1, TextMarkerType.WaveLine, Color.Blue));
						mtp.txt.ActiveTextAreaControl.Refresh();
						break;
					}
				}
			}
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

		private void clearParseChacheToolStripMenuItem_Click(object sender, EventArgs e)
		{
			D_IDE_Properties.GlobalModules.Clear();
		}

		private void UpdateChacheThread()
		{
			List<DModule> ret = new List<DModule>();

			stopParsingToolStripMenuItem.Enabled = true;
			bpw.Clear();
			Log("Reparse all directories");
			BuildProgressBar.Value = 0;
			BuildProgressBar.Maximum = 0;

			foreach (string dir in D_IDE_Properties.Default.parsedDirectories)
			{
				Log("Parse directory " + dir);
				if (!Directory.Exists(dir))
				{
					Log("Directory \"" + dir + "\" does not exist!");
					continue;
				}
				string[] files = Directory.GetFiles(dir, "*.d?", SearchOption.AllDirectories);
				BuildProgressBar.Maximum += files.Length;
				foreach (string tf in files)
				{
					if (tf.EndsWith("phobos.d")) continue; // Skip phobos.d

					if (D_IDE_Properties.HasModule(ret, tf)) { Log(tf + " already parsed!"); continue; }

					try
					{
						string tmodule = Path.ChangeExtension(tf, null).Remove(0, dir.Length + 1).Replace('\\', '.');
						DModule gpf = new DModule(tf, tmodule);

						D_IDE_Properties.AddFileData(ret, gpf);
						Log(tf);
						BuildProgressBar.Value++;
					}
					catch (Exception ex)
					{
						if (MessageBox.Show(ex.Message + "\n\nStop parsing process?+\n\n\n" + ex.StackTrace, "Error at " + tf, MessageBoxButtons.YesNo) == DialogResult.Yes)
						{
							stopParsingToolStripMenuItem.Enabled = false;
							return;
						}
					}
				}
			}
			Log(ProgressStatusLabel.Text = "Parsing done!");
			BuildProgressBar.Value = 0;
			stopParsingToolStripMenuItem.Enabled = false;
			lock (D_IDE_Properties.GlobalModules)
			{
				D_IDE_Properties.GlobalModules = ret;
			}
		}

		Thread updateTh;
		private void updateCacheToolStripMenuItem_Click(object sender, EventArgs e)
		{
			D_IDE_Properties.GlobalModules.Clear();
			if (updateTh != null && updateTh.ThreadState == System.Threading.ThreadState.Running) return;
			updateTh = new Thread(UpdateChacheThread);
			updateTh.Start();
		}

		private void stopParsingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (updateTh != null && updateTh.IsAlive == true)
			{
				updateTh.Abort();
				stopParsingToolStripMenuItem.Enabled = false;
			}
		}

		#endregion

		#region Building Procedures

		public void BuildSingle(object sender, EventArgs e)
		{
			UseOutput = false;
			if (SelectedTabPage != null)
			{
				bpw.Show();
				DocumentInstanceWindow tp = SelectedTabPage;
				if (!DModule.Parsable(tp.fileData.mod_file))
				{
					if (tp.fileData.mod_file.EndsWith(".rc"))
					{
						DBuilder.BuildResFile(
							tp.fileData.mod_file,
							Path.ChangeExtension(tp.fileData.mod_file, ".res"),
							Path.GetDirectoryName(tp.fileData.mod_file)
							);
						return;
					}
					MessageBox.Show("Can only build .d or .rc source files!");
					return;
				}
				Log("Build single " + tp.fileData.mod_file + " to " + Path.ChangeExtension(tp.fileData.mod_file, ".exe"));
				DBuilder.Exec(D_IDE_Properties.Default.exe_cmp, "\"" + tp.fileData.mod_file + "\" -O ", Path.GetDirectoryName(tp.fileData.mod_file), true).WaitForExit(10000);
			}
		}

		public bool Build()
		{
			bpw.Clear();
			errlog.buildErrors.Clear();
			errlog.Update();
			SaveAllTabs();
			foreach (DockContent tp in dockPanel.Documents)
			{
				if (tp is DocumentInstanceWindow)
				{
					DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
					//mtp.txt.Document.CustomLineManager.Clear();
					mtp.txt.ActiveTextAreaControl.Refresh();
				}
			}

			UseOutput = false;

			if (SelectedTabPage != null && (prj == null || prj.prjfn == ""))
			{
				BuildSingle(this, EventArgs.Empty);
				return true;
			}
			if (prj == null)
			{
				MessageBox.Show("Create project first!");
				return false;
			}
			if (D_IDE_Properties.Default.LogBuildProgress) bpw.Show();

			Log("Build entire project now");

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
			string bin = prj.basedir + "\\" + Path.ChangeExtension(prj.targetfilename, null) + ".exe";

			if (File.Exists(bin))
			{
				if (prj.type == DProject.PrjType.ConsoleApp) output.Show();

				output.Log("Executing " + prj.targetfilename);
				exeProc = DBuilder.Exec(bin, prj.execargs, prj.basedir, true); //prj.type == DProject.PrjType.ConsoleApp
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

		private void buildToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Build();
		}

		private void toggleBuildLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!bpw.Visible) bpw.Show();
			else bpw.Visible = false;
		}

		private void buildRunToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Build())
				Run();
		}

		private void runToolStripMenuItem_Click(object sender, EventArgs e)
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

				AddHighlightedBuildError(mod_file, lineNumber, errmsg, Color.LightPink);
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
		}

		/// <summary>
		/// Searches in child nodes of env
		/// </summary>
		/// <param name="env"></param>
		/// <param name="Path"></param>
		/// <returns>First match or null, if nothing were found</returns>
		public static TreeNode ContainsDir(TreeNode env, string Path)
		{
			foreach (TreeNode tn in env.Nodes)
			{
				if (tn is DirectoryTreeNode)
				{
					if (tn.Text == Path) return tn;
				}
			}
			return null;
		}

		public void UpdateFiles()
		{
			prjexplorer.prjFiles.Nodes.Clear();
			prjexplorer.prjFiles.BeginUpdate();
			foreach (string prjfn in D_IDE_Properties.Default.lastProjects)
			{
				string ext = Path.GetExtension(prjfn);
				if (!prjexplorer.fileIcons.Images.ContainsKey(ext))
				{
					Icon tico = ExtractIcon.GetIcon(prjfn, true);
					prjexplorer.fileIcons.Images.Add(ext, tico);
				}

				DProject LoadedPrj = (prj != null && prj.prjfn == prjfn) ? prj : DProject.LoadFrom(prjfn);
				if (LoadedPrj == null) continue;
				//LoadedPrj.RemoveNonExisting();

				ProjectNode CurPrjNode = new ProjectNode(LoadedPrj);
				CurPrjNode.ImageKey = CurPrjNode.SelectedImageKey = ext;
				try
				{
					if (prj != null && prjfn == prj.prjfn)
					{
						CurPrjNode.NodeFont = new Font(DefaultFont, FontStyle.Bold);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}

				foreach (string file in LoadedPrj.resourceFiles)
				{
					ext = Path.GetExtension(file);
					if (!prjexplorer.fileIcons.Images.ContainsKey(ext))
					{
						Icon tico = ExtractIcon.GetIcon(file, true);
						prjexplorer.fileIcons.Images.Add(ext, tico);
					}
					try
					{
						FileTreeNode TargetFileNode = new FileTreeNode(LoadedPrj, file);
						TargetFileNode.ImageKey = TargetFileNode.SelectedImageKey = Path.GetExtension(file);

						// if file is in project dir and isn't located in prj dir
						if (file.StartsWith(LoadedPrj.basedir) && Path.GetDirectoryName(file) != LoadedPrj.basedir)
						{
							string PathGone = "";
							string[] DirectoriesToCheck = Path.GetDirectoryName(file).Substring(LoadedPrj.basedir.Length + 1).Split('\\');

							TreeNode CurrentDirNode = new DirectoryTreeNode(LoadedPrj, LoadedPrj.basedir + "\\" + DirectoriesToCheck[0]);

							TreeNode ContainedChildNode = ContainsDir(CurPrjNode, DirectoriesToCheck[0]);
							bool isContained = false;
							if (isContained = ContainedChildNode != null)
								CurrentDirNode = ContainedChildNode;

							DirectoryTreeNode tdtn = null;

							if (DirectoriesToCheck.Length < 2)
								CurrentDirNode.Nodes.Add(TargetFileNode);

							for (int i = 1; i < DirectoriesToCheck.Length; i++)
							{
								string CurDirName = DirectoriesToCheck[i];
								if (CurDirName.Trim() == String.Empty) continue;
								PathGone += "\\" + CurDirName;

								PathGone = PathGone.TrimEnd('\\');

								ContainedChildNode = ContainsDir(CurrentDirNode, CurDirName);
								if (ContainedChildNode != null) CurrentDirNode = ContainedChildNode;

								tdtn = new DirectoryTreeNode(LoadedPrj, LoadedPrj.basedir + PathGone);
								if (i == DirectoriesToCheck.Length - 1) tdtn.Nodes.Add(TargetFileNode);

								CurrentDirNode.Nodes.Add(tdtn);

								CurrentDirNode = tdtn;
							}
							if (!isContained) CurPrjNode.Nodes.Add(CurrentDirNode);
						}
						else
						{
							CurPrjNode.Nodes.Add(TargetFileNode);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message + " (" + ex.Source + ")" + "\n\n" + ex.StackTrace);
					}
				}

				prjexplorer.prjFiles.Nodes.Add(CurPrjNode);
			}
			prjexplorer.prjFiles.ExpandAll();
			prjexplorer.prjFiles.EndUpdate();
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
			add ? prj : null);
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
				prj = np.prj;
				Text = prj.name + " - " + title;
				SaveAllTabs();

				if (D_IDE_Properties.Default.lastProjects.Contains(prj.prjfn)) { D_IDE_Properties.Default.lastProjects.Remove(prj.prjfn); }
				D_IDE_Properties.Default.lastProjects.Insert(0, prj.prjfn);
				if (D_IDE_Properties.Default.lastProjects.Count > 10) D_IDE_Properties.Default.lastProjects.RemoveAt(10);

				UpdateLastFilesMenu();
				UpdateFiles();
			}
		}

		private void SaveFile(object sender, EventArgs e)
		{
			DocumentInstanceWindow mtp = SelectedTabPage;
			if (mtp == null) return;

			mtp.Save();
			if (!mtp.fileData.IsParsable) return;
			mtp.ParseFromText(); // Reparse after save

			foreach (string dir in D_IDE_Properties.Default.parsedDirectories)
			{
				if (mtp.fileData.mod_file.StartsWith(dir))
				{
					D_IDE_Properties.AddFileData(mtp.fileData);
					break;
				}
			}

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
			TreeNode tn = new TreeNode(mtp.fileData.mod);
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
			if (Form1.thisForm.prj != null && Form1.thisForm.prj.resourceFiles.Contains(file))
				return Open(file, Form1.thisForm.prj);
			return Open(file, null);
		}
		public DocumentInstanceWindow Open(string file, DProject owner)
		{
			if (prj != null && file == prj.prjfn) return null; // Don't reopen the current project

			DocumentInstanceWindow ret = null;

			foreach (DockContent dc in dockPanel.Documents)
			{
				if (!(dc is DocumentInstanceWindow)) continue;
				DocumentInstanceWindow diw = (DocumentInstanceWindow)dc;
				if (diw.fileData.mod_file == file)
				{
					diw.Activate();
					Application.DoEvents();
					return diw;
				}
			}

			if (!File.Exists(file))
			{
				MessageBox.Show("File " + file + " doesn't exist!", "File not found!");
				return null;
			}

			if (Path.GetExtension(file) == DProject.prjext)
			{
				if (prj != null)
				{
					if (MessageBox.Show("Do you really want to open a new project?", "Open new project", MessageBoxButtons.YesNo) == DialogResult.No) return null;
				}
				prj = DProject.LoadFrom(file);
				if (prj == null) { MessageBox.Show("Failed to load project! Perhaps the projects version differs from the current version."); return null; }

				Text = prj.name + " - " + title;


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

				if (prj.lastopen.Count < 1 && prj.files.Count > 0)
				{
					string mfn = prj.files[0].mod_file;
					if (File.Exists(mfn))
					{
						Open(prj.files[0].mod_file, prj);
					}
				}
				foreach (string f in prj.lastopen)
				{
					Open(f, prj);
				}
				prj.lastopen.Clear();
				ret = SelectedTabPage;
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
			UpdateFiles();
			UpdateLastFilesMenu();
			if (this.dockPanel.ActiveDocumentPane != null) this.dockPanel.ActiveDocumentPane.ContextMenuStrip = this.contextMenuStrip1; // Set Tab selection bars context menu to ours
			return ret;
		}

		private void OpenFile(object sender, EventArgs e)
		{
			SaveAllTabs();

			if (oF.ShowDialog() == DialogResult.OK)
			{
				foreach (string fn in oF.FileNames) Open(fn);
				UpdateFiles();
			}
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

		private void TabSelectionChanged(object sender, EventArgs e)
		{
			DocumentInstanceWindow mtp = SelectedTabPage;
			if (mtp == null)
			{
				hierarchy.hierarchy.Nodes.Clear();
				return;
			}
			searchDlg.currentOffset = 0;
			mtp.ParseFolds(SelectedTabPage.fileData.dom);
			RefreshClassHierarchy();
		}

		private void CloseForm(object sender, FormClosingEventArgs e)
		{
			ForceExitDebugging();

			if (!Directory.Exists(Program.cfgDir))
				Directory.CreateDirectory(Program.cfgDir);

			if (prj != null)
			{
				foreach (DockContent tp in dockPanel.Documents)
				{
					if (tp is DocumentInstanceWindow)
					{
						DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
						if (prj.FileDataByFile(mtp.fileData.mod_file) != null || prj.resourceFiles.Contains(mtp.fileData.mod_file))
							prj.lastopen.Add(mtp.fileData.mod_file);
					}
				}
				prj.Save();
			}

			dockPanel.SaveAsXml(Program.LayoutFile);

			D_IDE_Properties.Default.lastFormState = this.WindowState;
			D_IDE_Properties.Default.lastFormLocation = this.Location;
			D_IDE_Properties.Default.lastFormSize = this.Size;

			D_IDE_Properties.Save(Program.prop_file);
			D_IDE_Properties.SaveGlobalCache(Program.ModuleCacheFile);
		}

		private void SaveAs(object sender, EventArgs e)
		{
			DocumentInstanceWindow tp = SelectedTabPage;
			if (tp == null) return;
			string bef = tp.fileData.mod_file;
			sF.FileName = bef;

			if (sF.ShowDialog() == DialogResult.OK)
			{
				if (prj != null)
				{
					if (DModule.Parsable(tp.fileData.mod_file))
					{
						if (prj.files.Contains(tp.fileData))
						{
							prj.files.Remove(tp.fileData);
						}
					}
					else
					{
						prj.resourceFiles.Remove(tp.fileData.mod_file);
					}
					prj.AddSrc(sF.FileName);
				}
				tp.fileData.mod = Path.GetFileNameWithoutExtension(sF.FileName);
				tp.fileData.mod_file = sF.FileName;
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

		private void CloseTab(object sender, EventArgs e)
		{
			DocumentInstanceWindow mtp = SelectedTabPage;
			if (mtp == null) return;

			mtp.Save();
			mtp.Close();
		}

		private void CloseAllTabs(object sender, EventArgs e)
		{
			SaveAllTabs();
			foreach (DockContent tp in dockPanel.Documents)
				tp.Close();
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
		private void searchReplaceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedTabPage != null)
			{
				if (SelectedTabPage.txt.ActiveTextAreaControl.SelectionManager.SelectedText.Length > 0)
					searchDlg.searchText = SelectedTabPage.txt.ActiveTextAreaControl.SelectionManager.SelectedText;
				searchDlg.Visible = true;
			}
		}

		private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			searchDlg.FindNextClick(sender, e);
		}

		private void searchTool_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return && SelectedTabPage != null && searchTool.TextBox.Text.Length > 0)
			{
				searchDlg.Search(searchTool.TextBox.Text);
			}
		}
		#endregion

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
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

			SelectedTabPage.ParseFolds(SelectedTabPage.fileData.dom);
		}
		#endregion

		#region Updates

		public static Version GetServerIDEVersion()
		{
			WebClient wc = new WebClient();
			string ans = "";
			try
			{
				ans = wc.DownloadString(Program.ver_txt);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error while connecting: " + ex.Message);
				return null;
			}
			try
			{
				return new Version(ans);
			}
			catch (Exception ex)
			{
				MessageBox.Show("\"" + ans + "\" is an invalid version number: " + ex.Message, "Server error");
			}
			return null;
		}

		public static WebClient webclient = new WebClient();
		public void CheckForUpdates()
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
				BuildProgressBar.Style = ProgressBarStyle.Marquee;
				webclient.DownloadFileAsync(new Uri("http://d-ide.svn.sourceforge.net/viewvc/d-ide/D-IDE/D-IDE/bin/Debug.tar.gz?view=tar"), upd_sf.FileName, upd_sf.FileName);
			}
		}

		void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			BuildProgressBar.Style = ProgressBarStyle.Continuous;
			if (e.Cancelled)
			{
				MessageBox.Show("Error: " + e.Error.Message);
				return;
			}
			MessageBox.Show(Path.GetFileName((string)e.UserState) + " successfully downloaded!");
			ProgressStatusLabel.Text = "Ready";
		}

		void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			BuildProgressBar.Maximum = 100;
			BuildProgressBar.Value = e.ProgressPercentage;
		}

		private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CheckForUpdates();
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

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (SelectedTabPage == null) e.Cancel = true;
		}

		private void testToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SelectedTabPage.Close();
		}

		private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (DockContent dc in dockPanel.Documents)
			{
				dc.Close();
			}
		}

		private void closeAllOthersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (DockContent dc in dockPanel.Documents)
			{
				if (dc == dockPanel.ActiveDocument) continue;
				dc.Close();
			}
		}

		private void aboutDIDEToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show("This software is freeware\nand is written by Alexander Bothe.", title);
		}

		private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
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

			int line = diw.Caret.Line;
			LineSegment ls = diw.txt.Document.GetLineSegmentForOffset(diw.CaretOffset);
			diw.txt.Document.Insert(
				diw.txt.Document.PositionToOffset(new TextLocation(0, line + 1)),
				diw.txt.Document.TextContent.Substring(ls.Offset, ls.Length) + "\r\n"
				);
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
		private void cutTBSButton_Click(object sender, EventArgs e)
		{
			try
			{
				SelectedTabPage.EmulateCut();
			}
			catch { }
		}

		private void copyTBSButton_Click(object sender, EventArgs e)
		{
			try
			{
				SelectedTabPage.EmulateCopy();
			}
			catch { }
		}

		private void toolStripButton5_Click(object sender, EventArgs e)
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
	}
}
