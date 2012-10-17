using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using AvalonDock;
using D_IDE.Controls.Panels;
using D_IDE.Core;
using D_IDE.Core.Controls;
using D_IDE.Dialogs;
using Microsoft.Windows.Controls.Ribbon;

namespace D_IDE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow, IFormBase
	{
		#region Properties
		public ProjectExplorer Panel_ProjectExplorer = new ProjectExplorer();
		public ErrorListPanel Panel_ErrorList = new ErrorListPanel();
		public LogPanel Panel_Log = new LogPanel();
		public StartPage StartPage;
		public DebugLocalsPanel Panel_Locals = new DebugLocalsPanel();
		public SearchAndReplaceDlg SearchAndReplaceDlg;
		public SearchResultPanel Panel_SearchResult=new SearchResultPanel();

		public bool RunCurrentModuleOnly
		{
			get { return Check_RunCurModule.IsEnabled ? Check_RunCurModule.IsChecked.Value : true; }
			set { Check_RunCurModule.IsChecked = value; }
		}

		/// <summary>
		/// Helper property: Indicates index of tab that shall become selected after debugging/executing finished
		/// </summary>
		static int _prevSelectedMainTab = -1;

		public string LeftStatusText
		{
			get { return StatusLabel1.Text; }
			set { StatusLabel1.Text = value; }
		}

		public string SecondLeftStatusText
		{
			get
			{
				return StatusLabel2.Text;
			}
			set
			{
				StatusLabel2.Text = value;
			}
		}

		public string ThirdStatusText
		{
			get
			{
				return StatusLabel3.Text;
			}
			set
			{
				StatusLabel3.Text = value;
			}
		}

		public ISearchResultPanel SearchResultPanel
		{
			get { return Panel_SearchResult; }
		}
		#endregion

		#region GUI Interactions
		public void RefreshGUI()
		{
			RefreshProjectExplorer();
			RefreshTitle();
			UpdateLastFilesMenus();
			RefreshMenu();
		}
		public void RefreshErrorList()
		{
			Panel_ErrorList.RefreshErrorList();
		}
		public void RefreshTitle()
		{
			Dispatcher.Invoke(new Action(() =>
			{
				string appendix = "D-IDE"; //+ System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3);

				if (IDEManager.CurrentSolution != null)
					Title = IDEManager.CurrentSolution.Name + " - " + appendix;
				else
					Title = appendix;
			}));
		}

		public void ClearLog()
		{
			Panel_Log.Clear();
		}

		public void RefreshProjectExplorer()
		{
			Panel_ProjectExplorer.Update();
		}

		public void RefreshMenu()
		{
			//Dispatcher.Invoke(new ParameterizedThreadStart(delegate(object o){

				var ed = IDEManager.Instance.CurrentEditor as EditorDocument;
				var IsEditable = ed != null;
				var HasProject = IsEditable && ed.HasProject;

				Tab_Edit.IsEnabled = IsEditable;
				Check_RunCurModule.IsEnabled = Tab_Project.IsEnabled = HasProject || IDEManager.CurrentSolution != null;
				Tab_Build.IsEnabled = ((IsEditable && ed.LanguageBinding != null && ed.LanguageBinding.CanBuild) || IDEManager.CurrentSolution != null) && !IDEManager.IDEDebugManagement.IsExecuting;

				// Restore tab selection when executing finished
				if (!Tab_Debug.IsEnabled && IDEManager.IDEDebugManagement.IsExecuting)
				{
					_prevSelectedMainTab = Ribbon.SelectedIndex;
					Tab_Debug.IsSelected = true;
				}
				else if (Tab_Debug.IsEnabled && !IDEManager.IDEDebugManagement.IsExecuting && _prevSelectedMainTab > -1)
					Ribbon.SelectedIndex = _prevSelectedMainTab;

				Tab_Debug.IsEnabled = IDEManager.IDEDebugManagement.IsExecuting;

				Button_StopBuilding.IsEnabled = IDEManager.BuildManagement.IsBuilding;

				Button_ResumeExecution.IsEnabled =
					Button_RestartExecution.IsEnabled =
					Button_PauseExecution.IsEnabled =
					Button_StepIn.IsEnabled =
					Button_StepOut.IsEnabled =
					Button_StepOver.IsEnabled =
					IDEManager.IDEDebugManagement.IsDebugging;
				Button_StopExecution.IsEnabled = IDEManager.IDEDebugManagement.IsExecuting;

				if (encoding_DropDown.IsEnabled = IsEditable)
					encoding_DropDown.Label = ed.Editor.Encoding.EncodingName;
				else
					encoding_DropDown.Label = "(No Encoding)";
			//}), this);
		}

		#endregion

		#region Initializer
		internal SplashScreen splashScreen;
		public MainWindow(ReadOnlyCollection<string> args)
		{
			if (!Debugger.IsAttached)
			{
				splashScreen = new SplashScreen("Resources/d-ide_256.png");
				splashScreen.Show(false, true);
			}

			// Init Manager
			WorkbenchLogic.Instance = new WorkbenchLogic(this);
			IDEManager.Instance = new IDEManager(this);

			// Load global settings
			try
			{
				GlobalProperties.Init();
			}
			catch { }

			InitializeComponent();

			encoding_DropDown.ItemsSource = new[] { 
				Encoding.ASCII, 
				Encoding.UTF8, 
				Encoding.Unicode, 
				Encoding.UTF32 
			};

			// Init logging support
			ErrorLogger.Instance = new IDELogger(this);

			// Showing the window is required because the DockMgr has to init all panels first before being able to restore last layouts
			Show();

			#region Init panels and their layouts
			Panel_Locals.Name = "LocalsPanel";
			Panel_Locals.HideOnClose = true;

			StartPage = new Controls.Panels.StartPage();
			StartPage.Name = "IDEStartPage";
			StartPage.HideOnClose = true;

			// Note: To enable the docking manager saving&restoring procedures it's needed to name all the panels
			Panel_ProjectExplorer.Name = "ProjectExplorer";
			Panel_ProjectExplorer.HideOnClose = true;

			Panel_Log.Name = "Output";
			Panel_Log.DockableStyle |= DockableStyle.AutoHide;

			Panel_ErrorList.Name = "ErrorList";
			Panel_ErrorList.HideOnClose = true;
			Panel_ErrorList.DockableStyle |= DockableStyle.AutoHide;

			RestoreDefaultPanelLayout();
			#endregion

			try
			{
				var layoutFile = Path.Combine(IDEInterface.ConfigDirectory, GlobalProperties.LayoutFile);

				// Exclude this call in develop (debug) time
				if (//!System.Diagnostics.Debugger.IsAttached&&
					File.Exists(layoutFile))
				{
					var fcontent = File.ReadAllText(layoutFile);
					if (!string.IsNullOrWhiteSpace(fcontent))
					{
						var s = new StringReader(fcontent);
						DockMgr.RestoreLayout(s);
						s.Close();
					}
				}
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			WorkbenchLogic.Instance.InitializeEnvironment(args);

			/*
			if (GlobalProperties.Instance.IsFirstTimeStart)
			{
				if(splashScreen!=null)
					splashScreen.Close(TimeSpan.FromSeconds( 0));
				if (MessageBox.Show("D-IDE seems to be launched for the first time. Start configuration now?", "First time startup", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
					new GlobalSettingsDlg() { Owner = this }.ShowDialog();
			}*/
		}

		private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
		{
			IDEUtil.CheckForUpdates(false);
		}
		#endregion

		public void RestoreDefaultPanelLayout()
		{
			// Note: The panels' order depends on the order of the initialization calls
			StartPage.ShowAsDocument(DockManager);
			
			Panel_ProjectExplorer.Show(DockMgr, AvalonDock.AnchorStyle.Left);

			Panel_Locals.Show(DockMgr, AvalonDock.AnchorStyle.Bottom);
			Panel_SearchResult.Show(DockMgr,AnchorStyle.Bottom);
			Panel_Log.Show(DockMgr, AvalonDock.AnchorStyle.Bottom);
			Panel_ErrorList.Show(DockMgr, AvalonDock.AnchorStyle.Bottom);
		}

		#region Ribbon buttons

		/// <summary>
		/// Create a source file that is unrelated to any open project or solution
		/// </summary>
		private void NewSource(object sender, RoutedEventArgs e)
		{
			var sdlg = new NewSrcDlg();
			if (sdlg.ShowDialog().Value)
			{
				foreach (var lang in LanguageLoader.Bindings)
					if (lang.CanHandleFile(sdlg.FileName))
					{
						var ed = lang.OpenFile(null, sdlg.FileName);

						// If language won't handle file anyway, just put it inside a neutral document
						if (ed == null)
							ed = new EditorDocument(sdlg.FileName);

						ed.Show(DockMgr);
						ed.Activate();
						return;
					}

				var _ed = new EditorDocument(sdlg.FileName);
				_ed.Show(DockMgr);
				_ed.Activate();
			}
		}

		private void NewProject(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoNewProject();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoOpenFile();
		}

		private void SaveAll(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.SaveAllFiles();
		}

		private void SaveAs(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoSaveAs();
		}

		private void Exit(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Settings(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoShowGlobalSettings();
		}

		private void BuildSolution_Click(object sender, ExecutedRoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoBuild();
		}

		private void CleanupProject_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoCleanSolution();				
		}

		private void Rebuild_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoExplicitRebuild();
		}

		private void LaunchDebugger_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoDebugSolution();
		}

		private void LaunchWithoutDebugger_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoLaunchWithoutDebugger(false);
		}

		private void LaunchInConsole_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoLaunchWithoutDebugger(true);
		}

		/*
		private void SendInput_Click(object sender, RoutedEventArgs e)
		{
			var prc=IDEManager.IDEDebugManagement.CurrentProcess;
			if (prc != null && prc.StartInfo.RedirectStandardInput)
			{
				prc.StandardInput.WriteLine("hello");
				prc.StandardInput.Flush();
			}
		}
		*/
		private void RefreshBreakpoints_Click(object sender, RoutedEventArgs e)
		{
			foreach (var ed in IDEManager.Instance.Editors)
				if (ed is EditorDocument)
					(ed as EditorDocument).RefreshBreakpointHighlightings();
		}

		private void Button_ResumeExecution_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.ContinueDebugging();
		}

		private void Button_PauseExecution_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.PauseExecution();
		}

		private void Button_StopExecution_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.StopExecution();
		}

		private void Button_RestartExecution_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoRestartExecution();
		}

		private void Button_StepIn_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.StepIn();
		}

		private void Button_StepOver_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.StepOver();
		}

		private void Button_StepOut_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.StepOut();
		}

		private void Button_StopBuilding_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.BuildManagement.StopBuilding();
		}

		#endregion

		class LastFileItem : RibbonApplicationMenuItem
		{
			public LastFileItem(string file, bool IsPrj)
			{
				Header = Path.GetFileName(file);
				ToolTipTitle = file;
				Click += delegate(Object o, RoutedEventArgs _e)
				{
					IDEManager.Instance.OpenFile(file);
				};

				Height = 22;
			}
		}

		public void UpdateLastFilesMenus()
		{
			Dispatcher.Invoke(new Action(() =>
			{
				Button_Open.Items.Clear();

				// Add default file open button
				var mi = new RibbonApplicationMenuItem();
				mi.Header = "File/Project";
				mi.ToolTipTitle = "Open File (Ctrl+O)";
				mi.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/OpenPH.png", UriKind.Relative));
				mi.Click += Open;
				Button_Open.Items.Add(mi);

				// Add recent files
				if (GlobalProperties.Instance.LastFiles.Count > 0 || GlobalProperties.Instance.LastProjects.Count > 0)
					Button_Open.Items.Add(new RibbonSeparator());

				foreach (var i in GlobalProperties.Instance.LastFiles)
					Button_Open.Items.Add(new LastFileItem(i, false));

				// Then add recent projects
				if (GlobalProperties.Instance.LastFiles.Count > 0 && GlobalProperties.Instance.LastProjects.Count > 0)
					Button_Open.Items.Add(new RibbonSeparator());

				foreach (var i in GlobalProperties.Instance.LastProjects)
					Button_Open.Items.Add(new LastFileItem(i, true));

				StartPage.RefreshLastProjects();
			}));
		}

		private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				// Save docking layout
				var layoutFile = Path.Combine(IDEInterface.ConfigDirectory, GlobalProperties.LayoutFile);
				if (File.Exists(layoutFile))
					File.Delete(layoutFile);
				DockMgr.SaveLayout(layoutFile);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Important: To free the handle of the search&replace-dlg it's needed to destroy the dlg
			if (SearchAndReplaceDlg != null && SearchAndReplaceDlg.IsLoaded)
				SearchAndReplaceDlg.Close();

			// Save window state & size
			GlobalProperties.Instance.lastFormSize = new Size(Width, Height);
			GlobalProperties.Instance.lastFormState = this.WindowState;
			GlobalProperties.Instance.LastSelectedRibbonTab = Ribbon.SelectedIndex;

			// Save currently opened documents
			GlobalProperties.Instance.LastOpenFiles.Clear();
			foreach (var doc in DockManager.Documents)
			{
				var aed = doc as AbstractEditorDocument;
				var ed = doc as EditorDocument;
				if (aed!=null)
					GlobalProperties.Instance.LastOpenFiles.Add(aed.AbsoluteFilePath,ed==null? new[]{0,0}: 
						new[]{ed.Editor.CaretOffset,(int)ed.Editor.TextArea.TextView.ScrollOffset.Y});
			}

			WorkbenchLogic.Instance.SaveIDEStates();
		}

		public DockingManager DockManager
		{
			get { return DockMgr; }
		}

		private void DockMgr_ActiveDocumentChanged(object sender, EventArgs e)
		{
			RefreshMenu();
			IDEManager.ErrorManagement.RefreshErrorList();
		}

		private void DockMgr_DocumentClosed(object sender, EventArgs e)
		{
			RefreshGUI();
		}

		private void DockMgr_DocumentClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var ed = IDEManager.Instance.CurrentEditor;

			if (ed != null && ed.Modified)
			{
				var res = MessageBox.Show(this, "Save file before close?", ed.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

				if (res == MessageBoxResult.Cancel)
					e.Cancel = true;
				else if (res == MessageBoxResult.Yes)
					ed.Save();
			}
		}

		private void PrjSettings_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoShowSolutionSettings();
		}

		private void CanOpenProjectSettings(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = WorkbenchLogic.Instance.SolutionSettingsAvailable;
		}

		private void Button_Update_Click(object sender, RoutedEventArgs e)
		{
			IDEUtil.CheckForUpdates(true);
		}

		private void Button_OpenPrjDir_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoOpenProjectDirectoryInExplorer();
		}

		private void Button_SetStackFrame_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.IDEDebugManagement.SetStackFrameToCurrentLine();
		}

		#region View buttons
		private void ShowLocals_Click(object sender, RoutedEventArgs e)
		{
			Panel_Locals.Show();
		}

		private void ShowPrjExplorerPanel_Click(object sender, RoutedEventArgs e)
		{
			Panel_ProjectExplorer.Show();
		}

		private void ShowStartpage_Click(object sender, RoutedEventArgs e)
		{
			StartPage.Show();
		}

		private void ShowLogPanel_Click(object sender, RoutedEventArgs e)
		{
			Panel_Log.Show();
		}

		private void ShowErrors_Click(object sender, RoutedEventArgs e)
		{
			Panel_ErrorList.Show();
		}

		private void RestoreDefaultLayout_Click(object sender, RoutedEventArgs e)
		{
			RestoreDefaultPanelLayout();
		}

		private void ShowSearchResults_Click(object sender, RoutedEventArgs e)
		{
			Panel_SearchResult.Show();
		}
		#endregion

		private void GotoLine(object sender, ExecutedRoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoGotoLine();
		}

		private void CommandBinding_CanExecute_SaveAs(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (IDEManager.Instance != null && IDEManager.Instance.CurrentEditor is EditorDocument);
		}

		private void Visitdide_sourceforge_net_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://d-ide.sourceforge.net");
		}

		private void Visitdigitalmars_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://digitalmars.com/d/2.0/");
		}

		private void SearchAndReplace_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (SearchAndReplaceDlg != null && SearchAndReplaceDlg.IsLoaded)
			{
				SearchAndReplaceDlg.Focus();
				return;
			}

			SearchAndReplaceDlg = new Dialogs.SearchAndReplaceDlg();
			SearchAndReplaceDlg.Owner = this;
			SearchAndReplaceDlg.Show();
		}

		private void FindNext_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			SearchAndReplaceDlg.DoFindNext();
		}

		private void encodingDropDown_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var enc = (sender as FrameworkElement).DataContext as Encoding;

			var ed = DockManager.ActiveDocument as EditorDocument;

			if (ed == null)
				return;

			ed.Editor.Encoding = enc;
			ed.Modified = true;
			encoding_DropDown.Label = enc.EncodingName;
		}

		private void RibbonWindow_Activated(object sender, EventArgs e)
		{
			var ed = IDEManager.Instance.CurrentEditor as EditorDocument;
			if (ed!=null)
				ed.DoOutsideModificationCheck();
		}

		/// <summary>
		/// Opens either the project's output directory OR the current module's output directory.
		/// Depends on if a project's been opened and/or the current module shall be built only.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OpenBinDirectory_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoOpenOutputDirectoryInExplorer();
		}

		private void Image_Feedback_MouseDown(object sender, MouseButtonEventArgs e)
		{
			WorkbenchLogic.Instance.DoOpenFeedbackDialog();
		}

		private void Button_GiveFeedback_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoOpenFeedbackDialog();
		}

		private void CloseWorkspace_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoCloseWorkspace();
		}
	}
}
