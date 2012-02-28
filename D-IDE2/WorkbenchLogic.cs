using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using D_IDE.Dialogs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace D_IDE
{
	/// <summary>
	/// Encapsules all actions which can be started by UI Elements in the Ide Window. (Such as New File/Open File/Save/Save as etc.)
	/// </summary>
	public class WorkbenchLogic
	{
		public static WorkbenchLogic Instance;

		public readonly MainWindow RootWindow;

		public WorkbenchLogic(MainWindow IdeRootWindow)
		{
			RootWindow = IdeRootWindow;
		}

		public void InitializeEnvironment(ReadOnlyCollection<string> args)
		{
			try
			{
				// Apply window state & size
				RootWindow.WindowState = GlobalProperties.Instance.lastFormState;
				if (!GlobalProperties.Instance.lastFormSize.IsEmpty)
				{
					RootWindow.Width = GlobalProperties.Instance.lastFormSize.Width;
					RootWindow.Height = GlobalProperties.Instance.lastFormSize.Height;
				}

				RootWindow.Ribbon.SelectedIndex = GlobalProperties.Instance.LastSelectedRibbonTab;
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			var stTh = new Thread(ThreadedInit)
			{
				IsBackground = true,
				Name= "Environment intializer thread"
			};

			stTh.Start(args);
		}

		void ThreadedInit(object argsObj)
		{
			var args = argsObj as ReadOnlyCollection<string>;

			// Load language bindings
			LanguageLoader.Bindings.Add(new GenericFileBinding());
			try
			{
				LanguageLoader.LoadLanguageInterface(Util.ApplicationStartUpPath + "\\D-IDE.D.dll", "D_IDE.D.DLanguageBinding");

				LanguageLoader.LoadLanguageInterface(Util.ApplicationStartUpPath + "\\D-IDE.D.dll", "D_IDE.ResourceFiles.ResScriptFileBinding");
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Load all language-specific settings
			foreach (var lang in LanguageLoader.Bindings)
				try
				{
					if (lang.CanUseSettings)
						lang.LoadSettings(AbstractLanguageBinding.CreateSettingsFileName(lang));
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
			
			// Load last solution
			RootWindow.Dispatcher.BeginInvoke(new Action(() =>
			{

				// If given, iterate over all cmd line arguments
				if (args.Count > 0)
					foreach (var a in args)
						IDEManager.Instance.OpenFile(a);
				else
				{
					// ... or load last project otherwise
					try
					{
						if (GlobalProperties.Instance.OpenLastPrj && GlobalProperties.Instance.LastProjects.Count > 0)
							IDEManager.Instance.OpenFile(GlobalProperties.Instance.LastProjects[0]);
					}
					catch (Exception ex)
					{
						ErrorLogger.Log(ex);
					}

					try
					{
						// Finally re-open all lastly edited files
						if (GlobalProperties.Instance.OpenLastFiles)
							foreach (var kv in GlobalProperties.Instance.LastOpenFiles)
								if (File.Exists(kv.Key))
								{
									var ed = OpenFile(kv.Key, kv.Value[0]);

									if (ed is EditorDocument)
										(ed as EditorDocument).Editor.ScrollToVerticalOffset(kv.Value[1]);
								}
					}
					catch (Exception ex)
					{
						ErrorLogger.Log(ex);
					}
				}
				RootWindow.RefreshGUI();
			}), System.Windows.Threading.DispatcherPriority.Background);

			if (RootWindow.splashScreen != null)
				RootWindow.splashScreen.Close(TimeSpan.FromSeconds(0.5));
		}

		public void SaveIDEStates()
		{
			try
			{
				// Save all files
				WorkbenchLogic.Instance. SaveAllFiles();

				// Save global settings
				GlobalProperties.Save();

				// Save language-specific settings
				foreach (var lang in LanguageLoader.Bindings)
					if (lang.CanUseSettings)
						try
						{
							var fn = AbstractLanguageBinding.CreateSettingsFileName(lang);

							if (File.Exists(fn))
								File.Delete(fn);

							lang.SaveSettings(fn);
						}
						catch (Exception ex)
						{
							ErrorLogger.Log(ex);
						}
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		bool allDocsReadOnly;
		/// <summary>
		/// Make all documents read only
		/// </summary>
		public bool AllDocumentsReadOnly
		{
			get { return allDocsReadOnly; }
			set
			{
				allDocsReadOnly = value;

				// Ensure thread-safety
				if (Util.IsDispatcherThread)
					foreach (var ed in from e in IDEManager.Instance.Editors where e is EditorDocument select e as EditorDocument)
						ed.Editor.IsReadOnly = value;
				else
					RootWindow.Dispatcher.Invoke(new Action(() =>
					{
						foreach (var ed in from e in IDEManager. Instance.Editors where e is EditorDocument select e as EditorDocument)
							ed.Editor.IsReadOnly = value;
					}));
			}
		}


		/// <summary>
		/// Central method to open a file/project/solution
		/// </summary>
		/// <returns>Editor instance (if a source file was opened)</returns>
		public AbstractEditorDocument OpenFile(string FileName)
		{
			if (string.IsNullOrWhiteSpace(FileName))
				return null;
			/*
			 * 1) Solution check
			 * 2) Project file check
			 * 3) Normal file check
			 */
			var ext = Path.GetExtension(FileName);

			// 1)
			if (ext == Solution.SolutionExtension)
			{
				if (!File.Exists(FileName))
				{
					ErrorLogger.Log("Solution " + FileName + " not found!", ErrorType.Error, ErrorOrigin.System);
					return null;
				}

				// Before load a new solution, close all related edited files
				if (IDEManager.CurrentSolution != null)
					WorkbenchLogic.Instance. CloseFilesRelatedTo(IDEManager.CurrentSolution);

				/*
				 * - Load solution
				 * - Load all of its projects
				 * - Open last opened files
				 */
				var sln = IDEManager.CurrentSolution = new Solution(FileName);

				AdjustLastFileList(FileName, true);

				foreach (var f in sln.ProjectFiles)
					if (File.Exists(sln.ToAbsoluteFileName(f)))
						Project.LoadProjectFromFile(sln, f);

				foreach (var prj in sln)
					if (prj != null && prj.LastOpenedFiles.Count > 0)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(fn);

				RootWindow.RefreshGUI();
				RootWindow.Panel_ProjectExplorer.MainTree.ExpandAll();
				return null;
			}

			// 2)
			var langs = LanguageLoader.Bindings.Where(l => l.CanHandleProject(FileName)).ToArray();
			if (langs.Length > 0)
			{
				if (!File.Exists(FileName))
				{
					ErrorLogger.Log("Project " + FileName + " not found!", ErrorType.Error, ErrorOrigin.System);
					return null;
				}

				/* 
				 * - Load project
				 * - Create anonymous solution that holds the project virtually
				 * - Open last opened files
				 */
				var _oldSln = IDEManager.CurrentSolution;
				IDEManager.CurrentSolution = new Solution();
				IDEManager.CurrentSolution.FileName = Path.ChangeExtension(FileName, Solution.SolutionExtension);

				var LoadedPrj = langs[0].OpenProject(IDEManager.CurrentSolution, FileName);
				if (LoadedPrj != null)
				{
					AdjustLastFileList(FileName, true);
					IDEManager.CurrentSolution.Name = LoadedPrj.Name;
					IDEManager.CurrentSolution.AddProject(LoadedPrj);

					foreach (var prj in IDEManager.CurrentSolution)
						if (prj != null && prj.LastOpenedFiles.Count > 0)
							foreach (var fn in prj.LastOpenedFiles)
								OpenFile(prj.ToAbsoluteFileName(fn));
					IDEManager.Instance.UpdateGUI();
				}
				else IDEManager.CurrentSolution = _oldSln;
				return null;
			}

			//3)

			// Try to resolve owner project
			// - useful if relative path was given - enables
			Project _prj = null;
			if (IDEManager.CurrentSolution != null)
				foreach (var p in IDEManager.CurrentSolution.ProjectCache)
					if (p.ContainsFile(FileName))
					{
						_prj = p;
						break;
					}

			// Make file path absolute
			var absPath = _prj != null ? _prj.ToAbsoluteFileName(FileName) : FileName;

			// Add file to recently used files
			AdjustLastFileList(absPath, false);

			// Check if file already open -- Allow only one open instance of a file!
			foreach (var doc in Instance.RootWindow.DockManager.Documents)
				if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).AbsoluteFilePath == absPath)
				{
					// Activate the wanted item and return it
					doc.Activate();
					RootWindow.DockManager.ActiveDocument = doc;
					return doc as AbstractEditorDocument;
				}

			EditorDocument newEd = null;

			foreach (var lang in LanguageLoader.Bindings)
				if (lang.SupportsEditor(absPath))
				{
					newEd = lang.OpenFile(_prj, absPath);
					break;
				}

			if (newEd == null)
				newEd = new EditorDocument(absPath);

			// Set read only state if e.g. debugging currently
			newEd.Editor.IsReadOnly = AllDocumentsReadOnly;
			newEd.Show(RootWindow.DockManager);

			try
			{
				Instance.RootWindow.DockManager.ActiveDocument = newEd;
				newEd.Activate();
			}
			catch
			{

			}
			IDEManager.Instance.UpdateGUI();
			newEd.Editor.Focus();
			return newEd;
		}

		/// <summary>
		/// Opens a file and moves caret to Line,Col. Scrolls down the view if needed.
		/// </summary>
		public AbstractEditorDocument OpenFile(string FileName, int Line, int Col)
		{
			var ret = OpenFile(FileName);
			var ed = ret as EditorDocument;

			if (ed == null || ed.Editor == null || ed.Editor.Document == null)
				return ret;

			if (Line >= ed.Editor.Document.LineCount && Col >= ed.Editor.Document.Lines[ed.Editor.Document.LineCount].Length)
				ed.Editor.CaretOffset = ed.Editor.Document.TextLength;
			else
				ed.Editor.CaretOffset = ed.Editor.Document.GetOffset(Line, Col);

			ed.Editor.ScrollTo(Line, Col);
			ed.Editor.Focus();

			return ed;
		}

		public AbstractEditorDocument OpenFile(string FileName, int Offset)
		{
			var ret = OpenFile(FileName);
			var ed = ret as EditorDocument;

			if (ed == null)
				return ret;

			if (Offset > ed.Editor.Document.TextLength)
				Offset = ed.Editor.Document.TextLength;

			if (Offset >= 0)
			{
				ed.Editor.CaretOffset = Offset;
				var loc = ed.Editor.Document.GetLocation(Offset);
				ed.Editor.ScrollTo(loc.Line, loc.Column);
				/*
				var visLine=ed.Editor.TextArea.TextView.GetOrConstructVisualLine(ed.Editor.Document.GetLineByNumber(loc.Line));
				if(visLine!=null)
					ed.Editor.ScrollToVerticalOffset(visLine.VisualTop-50);*/
			}
			ed.Editor.Focus();

			return ed;
		}

		public void DoNewProject()
		{
			var pdlg = new NewProjectDlg(NewProjectDlg.DialogMode.CreateNew | (IDEManager.CurrentSolution != null ? NewProjectDlg.DialogMode.Add : 0));

			Util.CreateDirectoryRecursively(GlobalProperties.Instance.DefaultProjectDirectory);
			pdlg.ProjectDir = GlobalProperties.Instance.DefaultProjectDirectory;

			if (pdlg.ShowDialog().Value)
			{
				var pdir = pdlg.ProjectDir;

				if (pdlg.CreateSolutionDir && !pdlg.AddToCurrentSolution)
					pdir += "\\" + Util.PurifyDirName(pdlg.SolutionName);

				Util.CreateDirectoryRecursively(pdir);

				if (IDEManager.CurrentSolution != null && pdlg.AddToCurrentSolution)
				{
					IDEManager.ProjectManagement.AddNewProjectToSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdir);
				}
				else if (!pdlg.AddToCurrentSolution)
				{
					IDEManager.CurrentSolution = IDEManager.ProjectManagement.CreateNewProjectAndSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdir,
						pdlg.SolutionName);
					 AdjustLastFileList(IDEManager.CurrentSolution.FileName, true);
				}

				RootWindow.RefreshGUI();
			}
		}

		public void DoOpenFile()
		{
			var dlg = new OpenFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.InitialDirectory = GlobalProperties.Instance.DefaultProjectDirectory;
			dlg.Multiselect = true;

			if (dlg.ShowDialog().Value)
				foreach (var fn in dlg.FileNames)
					OpenFile(fn);
		}

		public void DoSaveAs()
		{
			var ce = IDEManager.Instance.CurrentEditor;
			if (ce == null)
				return;

			var dlg = new SaveFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.FileName = ce.AbsoluteFilePath;

			if (dlg.ShowDialog().Value)
				SaveCurrentFileAs(dlg.FileName);
		}

		public void DoShowGlobalSettings()
		{
			try
			{
				var dlg = new GlobalSettingsDlg();
				dlg.Owner = RootWindow;
				dlg.ShowDialog();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
		}

		public void DoShowSolutionSettings()
		{
			if (IDEManager.Instance.CurrentEditor != null)
				IDEManager.ProjectManagement.ShowProjectPropertiesDialog(IDEManager.Instance.CurrentEditor.Project);
		}

		public bool SolutionSettingsAvailable
		{
			get {
				return IDEManager.Instance.CurrentEditor != null && IDEManager.Instance.CurrentEditor.HasProject;
			}
		}

		public void DoBuild()
		{
            SaveAllFiles();

			if (RootWindow.RunCurrentModuleOnly)
				IDEManager.BuildManagement.BuildSingle();
			else
				IDEManager.BuildManagement.Build();
		}

		public void DoExplicitRebuild()
        {
            SaveAllFiles();

			if (!RootWindow.RunCurrentModuleOnly)
				IDEManager.BuildManagement.Build(IDEManager.CurrentSolution, false);
			else
			{
				IDEManager.BuildManagement.CleanUpLastSingleBuild();
				IDEManager.BuildManagement.BuildSingle();
			}
		}

		public void DoCleanSolution()
		{
			if (!RootWindow.RunCurrentModuleOnly)
				IDEManager.BuildManagement.CleanUpOutput(IDEManager.CurrentSolution);
			else
				IDEManager.BuildManagement.CleanUpLastSingleBuild();
		}

		public void DoDebugSolution()
        {
            SaveAllFiles();

			BuildResult br = null;
			if (CoreManager.DebugManagement.IsDebugging)
				IDEManager.IDEDebugManagement.ContinueDebugging();
			else if (RootWindow.RunCurrentModuleOnly ? 
				((br = IDEManager.BuildManagement.BuildSingle()) != null && br.Successful) : 
				IDEManager.BuildManagement.Build())
				IDEManager.IDEDebugManagement.LaunchWithDebugger();
		}

		public void DoLaunchWithoutDebugger(bool ExternalConsole = false)
        {
            SaveAllFiles();

			BuildResult br = null;
			if (RootWindow.RunCurrentModuleOnly ? 
				((br = IDEManager.BuildManagement.BuildSingle()) != null && br.Successful) : 
				IDEManager.BuildManagement.Build())
				IDEManager.IDEDebugManagement.LaunchWithoutDebugger(ExternalConsole);
		}

		public void DoRestartExecution()
		{
			var dbg = IDEManager.IDEDebugManagement.IsDebugging;

			IDEManager. IDEDebugManagement.StopExecution();

			if (dbg)
				IDEManager.IDEDebugManagement.ContinueDebugging();
			else
				DoLaunchWithoutDebugger();
		}

		public void DoOpenProjectDirectoryInExplorer()
		{
			string dir = "";

			if (IDEManager.Instance.CurrentEditor != null)
				dir = Path.GetDirectoryName(IDEManager.Instance.CurrentEditor.AbsoluteFilePath);
			else
				dir = CoreManager.CurrentSolution.BaseDirectory;

			Process.Start("explorer.exe", dir);
		}

		public void DoOpenOutputDirectoryInExplorer()
		{
			var ed = IDEManager.Instance.CurrentEditor;

			if (ed == null)
				return;

			string dir = "";

			if (!RootWindow.RunCurrentModuleOnly && ed.HasProject)
				dir = ed.Project.OutputDirectory;
			else
				dir = Path.GetDirectoryName(ed.AbsoluteFilePath);

			if (Directory.Exists(dir))
				Process.Start(dir);
			else
				MessageBox.Show("\"" + dir + "\" not created yet.");
		}

		public void DoOpenFeedbackDialog()
		{
			(new FeedbackDialog() { Owner = RootWindow }).Show();
		}

		public void DoCloseWorkspace()
		{
			// Close open files
			AvalonDock.DockableContent dc = null;
			while ((dc = RootWindow.DockManager.ActiveDockableContent) != null)
				dc.Close(true);

			// Save projects and solution
			if (IDEManager. CurrentSolution != null)
			{
				foreach (var prj in IDEManager. CurrentSolution.ProjectCache)
					prj.Save();

				IDEManager. CurrentSolution.Save();
			}

			// Reset solution instance
			IDEManager. CurrentSolution = null;

			// Update GUI
			RootWindow.RefreshGUI();
		}

		public void DoGotoLine()
		{
			var dlg = new GotoDialog();
			dlg.Owner = RootWindow;
			if (dlg.ShowDialog().Value)
			{
				var ed = IDEManager.Instance.CurrentEditor as EditorDocument;

				if (dlg.EnteredNumber >= ed.Editor.Document.LineCount)
				{
					MessageBox.Show("Number must be a value between 0 and " + ed.Editor.Document.LineCount);
					return;
				}

				ed.Editor.TextArea.Caret.Line = dlg.EnteredNumber;
				ed.Editor.TextArea.Caret.BringCaretToView();
			}
		}

		/// <summary>
		/// Adds a new entry to the globally shared LastFiles/LastProjects-List
		/// </summary>
		/// <param name="openedFile">The file which got opened recently</param>
		/// <param name="IsPrj">Add it to the LastProjects List?</param>
		public static void AdjustLastFileList(string openedFile, bool IsPrj)
		{
			var l = IsPrj ? GlobalProperties.Instance.LastProjects : GlobalProperties.Instance.LastFiles;
			if (l.Contains(openedFile))
				l.Remove(openedFile);
			l.Insert(0, openedFile);
			while (l.Count > 10)
				l.RemoveAt(l.Count - 1);
		}

		public void SaveCurrentFile()
		{
			if (IDEManager. Instance.CurrentEditor != null)
				IDEManager. Instance.CurrentEditor.Save();
		}

		public void SaveAllFiles()
		{
			foreach (var doc in RootWindow.DockManager.Documents)
				if (doc is AbstractEditorDocument)
					(doc as AbstractEditorDocument).Save();

			if (IDEManager. CurrentSolution != null)
			{
				IDEManager. CurrentSolution.Save();

				foreach (var p in IDEManager. CurrentSolution)
				{
					p.LastOpenedFiles.Clear();
					// Store last opened files
					foreach (var ed in IDEManager.Instance.Editors)
					{
						if (p.ContainsFile(ed.AbsoluteFilePath))
							p.LastOpenedFiles.Add(ed.FileName);
					}

					p.Save();
				}
			}
		}

		/// <summary>
		/// Saves the file under a new file name.
		/// Copies the file and does not affect the project.
		/// </summary>
		public void SaveCurrentFileAs(string NewFilePath)
		{
			if (IDEManager.Instance.CurrentEditor == null)
				return;

			IDEManager.Instance.CurrentEditor.FileName = NewFilePath;
			IDEManager.Instance.CurrentEditor.Modified = true;
			IDEManager.Instance.CurrentEditor.Save();
		}

		public void CloseFilesRelatedTo(Solution Solution)
		{
			if (Solution != null)
				foreach (var prj in Solution)
					CloseFilesRelatedTo(prj);
		}

		public void CloseFilesRelatedTo(Project Project)
		{
			if (Project == null)
				return;

			foreach (var ed in IDEManager.Instance.Editors)
				if (ed.HasProject && ed.Project == Project)
					ed.Close();
		}
	}
}
