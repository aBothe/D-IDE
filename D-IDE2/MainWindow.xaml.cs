using System.Collections.Generic;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;
using D_IDE.Core;
using D_IDE.Dialogs;
using D_IDE.Controls.Panels;
using System.Windows.Input;
using System;
using Microsoft.Win32;
using System.Linq;
using System.IO;
using System.Threading;
using AvalonDock;
using D_IDE.Core.Controls;
using System.Diagnostics;

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

		public bool RunCurrentModuleOnly
		{
			get { return Check_RunCurModule.IsEnabled?Check_RunCurModule.IsChecked.Value:true; }
			set { Check_RunCurModule.IsChecked = value; }
		}

		/// <summary>
		/// Helper property: Indicates index of tab that shall become selected after debugging/executing finished
		/// </summary>
		static int _prevSelectedMainTab = -1;
			
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
			string appendix = "D-IDE " + System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString(3);
			
			if (IDEManager.CurrentSolution != null)
				Title = IDEManager.CurrentSolution.Name + " - "+appendix;
			else
				Title = appendix;
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
			Dispatcher.Invoke(new ParameterizedThreadStart(delegate(object o)
			{
				var ed = IDEManager.Instance.CurrentEditor as EditorDocument;
				var IsEditable = ed != null;
				var HasProject = IsEditable && ed.HasProject;

				Tab_Edit.IsEnabled = IsEditable;
				Check_RunCurModule.IsEnabled = Tab_Project.IsEnabled = HasProject || IDEManager.CurrentSolution != null;
				Tab_Build.IsEnabled = ((IsEditable && ed.LanguageBinding!=null && ed.LanguageBinding.CanBuild) || IDEManager.CurrentSolution != null) && !IDEManager.IDEDebugManagement.IsExecuting;

				// Restore tab selection when executing finished
				if (!Tab_Debug.IsEnabled && IDEManager.IDEDebugManagement.IsExecuting)
				{
					_prevSelectedMainTab = Ribbon.SelectedIndex;
					Tab_Debug.IsSelected=true;
				}
				else if (Tab_Debug.IsEnabled && !IDEManager.IDEDebugManagement.IsExecuting && _prevSelectedMainTab>-1)
					Ribbon.SelectedIndex = _prevSelectedMainTab;

				Tab_Debug.IsEnabled = IDEManager.IDEDebugManagement.IsExecuting;

				Button_StopBuilding.IsEnabled = IDEManager.BuildManagement.IsBuilding;

				Button_ResumeExecution.IsEnabled =
					Button_RestartExecution.IsEnabled=
					Button_PauseExecution.IsEnabled =
					Button_StepIn.IsEnabled =
					Button_StepOut.IsEnabled =
					Button_StepOver.IsEnabled =
					IDEManager.IDEDebugManagement.IsDebugging;
				Button_StopExecution.IsEnabled = IDEManager.IDEDebugManagement.IsExecuting;
			}),this);
		}

		#endregion

		#region Initializer
		public MainWindow(string[] args)
		{
			var splashScreen = new SplashScreen("Resources/d-ide_256.png");
			splashScreen.Show(false,true);

			// Init Manager
			IDEManager.Instance = new IDEManager(this);

			InitializeComponent();

			// Init logging support
			ErrorLogger.Instance = new IDELogger(this);

			// Load global settings
			try
			{
				GlobalProperties.Init();

				// Apply window state & size
				WindowState = GlobalProperties.Instance.lastFormState;
				if (!GlobalProperties.Instance.lastFormSize.IsEmpty)
				{
					Width = GlobalProperties.Instance.lastFormSize.Width;
					Height = GlobalProperties.Instance.lastFormSize.Height;
				}

				Ribbon.SelectedIndex = GlobalProperties.Instance.LastSelectedRibbonTab;
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

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

			// Load layout
			try
			{
				var layoutFile = Path.Combine(IDEInterface.ConfigDirectory, GlobalProperties.LayoutFile);
				// Exclude this call in develop (debug) time
				if (//!System.Diagnostics.Debugger.IsAttached&&
					File.Exists(layoutFile))
					DockMgr.RestoreLayout(layoutFile);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Load language bindings
			LanguageLoader.Bindings.Add(new GenericFileBinding());
			try
			{
				LanguageLoader.LoadLanguageInterface(Util.ApplicationStartUpPath+"\\D-IDE.D.dll", "D_IDE.D.DLanguageBinding");
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Load all language-specific settings
			foreach (var lang in LanguageLoader.Bindings)
				try
				{
					if (lang.CanUseSettings)
						lang.LoadSettings(AbstractLanguageBinding.CreateSettingsFileName(lang));
				}
				catch (Exception ex) {
					ErrorLogger.Log(ex);
				}

			RefreshGUI();

			if (args.Length > 0)
				foreach (var a in args)
					IDEManager.EditingManagement.OpenFile(a);
			else

			// Load last solution
				if (GlobalProperties.Instance.OpenLastPrj && GlobalProperties.Instance.LastProjects.Count > 0)
				{
					if(File.Exists(GlobalProperties.Instance.LastProjects[0]))
						IDEManager.EditingManagement.OpenFile(GlobalProperties.Instance.LastProjects[0]);
				}

			splashScreen.Close(TimeSpan.FromSeconds(0.5));
		}
		#endregion

		public void RestoreDefaultPanelLayout()
		{
			//TODO: Make this restoring the layouts properly
			Panel_Locals.Show(DockMgr, AvalonDock.AnchorStyle.Bottom);
			StartPage.ShowAsDocument(DockManager);
			Panel_ProjectExplorer.Show(DockMgr, AvalonDock.AnchorStyle.Left);
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
						var ed= lang.OpenFile(null, sdlg.FileName);
						ed.Show(DockMgr);
						ed.Activate();
						return;
					}

				var _ed = new EditorDocument(sdlg.FileName);
				_ed.Show(DockMgr);
				_ed.Activate();
			}
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
					IDEManager.ProjectManagement.AddNewProjectToSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdir);
				else if (!pdlg.AddToCurrentSolution)
				{
					IDEManager.CurrentSolution = IDEManager.ProjectManagement.CreateNewProjectAndSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdir,
						pdlg.SolutionName);
					IDEManager.EditingManagement.AdjustLastFileList(IDEManager.CurrentSolution.FileName, true);
				}

				RefreshGUI();
			}
		}

		private void NewProject(object sender, RoutedEventArgs e)
		{
			DoNewProject();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.InitialDirectory = GlobalProperties.Instance.DefaultProjectDirectory;
			dlg.Multiselect = true;

			if (dlg.ShowDialog().Value)
				foreach (var fn in dlg.FileNames)
					IDEManager.EditingManagement.OpenFile(fn);
		}

		private void SaveAll(object sender, RoutedEventArgs e)
		{
			IDEManager.EditingManagement.SaveAllFiles();
		}

		private void SaveAs(object sender, RoutedEventArgs e)
		{
			var ce = IDEManager.Instance.CurrentEditor;
			if (ce == null)
				return;

			var dlg = new SaveFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.FileName = ce.AbsoluteFilePath;

			if (dlg.ShowDialog().Value)
				IDEManager.EditingManagement.SaveCurrentFileAs(dlg.FileName);
		}

		private void Exit(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Settings(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new GlobalSettingsDlg();
				dlg.Owner = this;
				dlg.ShowDialog();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
		}

		private void BuildSolution_Click(object sender, RoutedEventArgs e)
		{
			if (RunCurrentModuleOnly)
				IDEManager.BuildManagement.BuildSingle();
			else
				IDEManager.BuildManagement.Build();
		}

		private void LaunchDebugger_Click(object sender, RoutedEventArgs e)
		{
			if (CoreManager.DebugManagement.IsDebugging)
				Button_ResumeExecution_Click(null, null);
			else if (RunCurrentModuleOnly ? IDEManager.BuildManagement.BuildSingle() : IDEManager.BuildManagement.Build())
				IDEManager.IDEDebugManagement.LaunchWithDebugger();
		}

		bool _showextconsole = false;
		private void LaunchWithoutDebugger_Click(object sender, RoutedEventArgs e)
		{
			if (RunCurrentModuleOnly? IDEManager.BuildManagement.BuildSingle():IDEManager.BuildManagement.Build())
				IDEManager.IDEDebugManagement.LaunchWithoutDebugger(_showextconsole);
			_showextconsole = false;
		}

		private void LaunchInConsole_Click(object sender, RoutedEventArgs e)
		{
			_showextconsole = true;
		}

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
			var dbg = IDEManager.IDEDebugManagement.IsDebugging;
			Button_StopExecution_Click(sender, e);
			if (dbg)
				LaunchDebugger_Click(sender, e);
			else 
				LaunchWithoutDebugger_Click(sender, e);
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
			public LastFileItem(string file,bool IsPrj)
			{
				Header = Path.GetFileName(file);
				ToolTipTitle = file;
				Click += delegate(Object o, RoutedEventArgs _e)
				{
					IDEManager.EditingManagement.OpenFile(file);
				};

				Height = 22;
			}
		}

		public void UpdateLastFilesMenus()
		{
			Button_Open.Items.Clear();
			
			var mi = new RibbonApplicationMenuItem();
			mi.Header = "File/Project";
			mi.ToolTipTitle = "Open File (Ctrl+O)";
			mi.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/OpenPH.png", UriKind.Relative));
			mi.Click += Open;
			Button_Open.Items.Add(mi);

			// First add recent files
			if (GlobalProperties.Instance.LastFiles.Count > 0 || GlobalProperties.Instance.LastProjects.Count > 0)
				Button_Open.Items.Add(new RibbonSeparator());

			foreach (var i in GlobalProperties.Instance.LastFiles)
				Button_Open.Items.Add(new LastFileItem(i,false));

			// Then add recent projects
			if (GlobalProperties.Instance.LastFiles.Count > 0 && GlobalProperties.Instance.LastProjects.Count > 0)
				Button_Open.Items.Add(new RibbonSeparator());

			foreach (var i in GlobalProperties.Instance.LastProjects)
				Button_Open.Items.Add(new LastFileItem(i,true));

			StartPage.RefreshLastProjects();
		}

		private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Save window state & size
			GlobalProperties.Instance.lastFormSize = new Size(Width,Height);
			GlobalProperties.Instance.lastFormState = this.WindowState;
			GlobalProperties.Instance.LastSelectedRibbonTab = Ribbon.SelectedIndex;

			try
			{
				// Save docking layout
				var layoutFile = Path.Combine(IDEInterface.ConfigDirectory, GlobalProperties.LayoutFile);
				DockMgr.SaveLayout(layoutFile);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
			try{
				// Save all files
				IDEManager.EditingManagement.SaveAllFiles();

				// Save global settings
				GlobalProperties.Save();

				// Save language-specific settings
				foreach(var lang in LanguageLoader.Bindings)
					if(lang.CanUseSettings)
						try
						{
							lang.SaveSettings(AbstractLanguageBinding.CreateSettingsFileName(lang));
						}
						catch (Exception ex)
						{
							ErrorLogger.Log(ex);
						}
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
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
				var res = MessageBox.Show(this,"Save file before close?",ed.Title,MessageBoxButton.YesNoCancel,MessageBoxImage.Question,MessageBoxResult.Yes);

				if (res == MessageBoxResult.Cancel)
					e.Cancel = true;
				else if (res == MessageBoxResult.Yes)
					ed.Save();
			}
		}

		private void PrjSettings_Click(object sender, RoutedEventArgs e)
		{
			if(IDEManager.Instance.CurrentEditor!=null)
				IDEManager.ProjectManagement.ShowProjectPropertiesDialog(IDEManager.Instance.CurrentEditor.Project);
		}

		private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
		{
			IDEUtil.CheckForUpdates(false);
		}

		private void Button_Update_Click(object sender, RoutedEventArgs e)
		{
			IDEUtil.CheckForUpdates(true);
		}

		private void Button_OpenPrjDir_Click(object sender, RoutedEventArgs e)
		{
			string dir = "";

			if (IDEManager.Instance.CurrentEditor != null)
				dir = Path.GetDirectoryName(IDEManager.Instance.CurrentEditor.AbsoluteFilePath);
			else
				dir = CoreManager.CurrentSolution.BaseDirectory;

			System.Diagnostics.Process.Start("explorer.exe",dir);
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
		#endregion

		private void GotoLine(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new GotoDialog();
			dlg.Owner=this;
			if (dlg.ShowDialog().Value)
			{
				var ed = IDEManager.Instance.CurrentEditor as EditorDocument;

				if (dlg.EnteredNumber >= ed.Editor.Document.LineCount)
				{
					MessageBox.Show("Number must be a value between 0 and "+ed.Editor.Document.LineCount);
					return;
				}

				ed.Editor.TextArea.Caret.Line = dlg.EnteredNumber;
				ed.Editor.TextArea.Caret.BringCaretToView();
			}
		}


		public string LeftStatusText
		{
			get { return StatusLabel1.Text; }
			set { StatusLabel1.Text = value; }
		}

		private void CommandBinding_CanExecute_SaveAs(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (IDEManager.Instance!=null && IDEManager.Instance.CurrentEditor is EditorDocument);
		}

		private void Visitdide_sourceforge_net_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://d-ide.sourceforge.net");
		}
	}
	
}
