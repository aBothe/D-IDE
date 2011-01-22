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

namespace D_IDE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow
	{
		#region Properties
		public ProjectExplorer Panel_ProjectExplorer = new ProjectExplorer();
		public ErrorListPanel Panel_ErrorList = new ErrorListPanel();
		#endregion

		#region GUI Interactions
		public void UpdateGUIElements()
		{
			UpdateProjectExplorer();
			UpdateTitle();
			UpdateLastFilesMenus();
		}
		public void UpdateProjectExplorer()
		{
			Panel_ProjectExplorer.Update();
		}
		public void UpdateTitle()
		{
			if (IDEManager.CurrentSolution != null)
				Title = IDEManager.CurrentSolution.Name + " - D-IDE";
			else
				Title = "D-IDE";
		}
		#endregion

		#region Initializer
		public MainWindow(string[] args)
		{
			InitializeComponent();

			// Init global variables
			IDEManager.MainWindow = this;

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
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Showing the window is required because the DockMgr has to init all panels first before being able to restore last layouts
			Show();

			#region Init panels and their layouts
			// Note: To enable the docking manager saving&restoring procedures it's needed to name all the panels
			Panel_ProjectExplorer.Name = "ProjectExplorer";
			Panel_ProjectExplorer.HideOnClose = true;
			Panel_ProjectExplorer.Show(DockMgr, AvalonDock.AnchorStyle.Left);

			Panel_ErrorList.Name = "ErrorList";
			Panel_ErrorList.HideOnClose = true;
			Panel_ErrorList.Show(DockMgr,AvalonDock.AnchorStyle.Bottom);
			#endregion

			// Load layout
			try
			{
				var layoutFile = Path.Combine(IDEInterface.ConfigDirectory, GlobalProperties.LayoutFile);
				// Exclude this call in develop time
				//if (File.Exists(layoutFile))	DockMgr.RestoreLayout(layoutFile);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			// Load language bindings
			LanguageLoader.Bindings.Add(new GenericFileBinding());
			try
			{
				LanguageLoader.LoadLanguageInterface("D-IDE.D.dll", "D_IDE.D.DLanguageBinding");
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			UpdateGUIElements();
		}
		#endregion

		#region Ribbon buttons

		/// <summary>
		/// Create a source file that is unrelated to any open project or solution
		/// </summary>
		private void NewSource(object sender, RoutedEventArgs e)
		{
			var sdlg = new NewSrcDlg();
			if (sdlg.ShowDialog().Value)
			{
				var ed = new EditorDocument(sdlg.FileName);
				ed.Show(DockMgr);
			}
		}

		private void NewProject(object sender, RoutedEventArgs e)
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
					IDEManager.CurrentSolution = IDEManager.ProjectManagement.CreateNewProjectAndSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdir,
						pdlg.SolutionName);

				UpdateGUIElements();
			}
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

		private void Save(object sender, RoutedEventArgs e)
		{
			IDEManager.EditingManagement.SaveCurrentFile();
		}

		private void SaveAll(object sender, RoutedEventArgs e)
		{
			IDEManager.EditingManagement.SaveAllFiles();
		}

		private void SaveAs(object sender, RoutedEventArgs e)
		{
			var ce = IDEManager.CurrentEditor;
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
			var dlg = new GlobalSettingsDlg();
			dlg.ShowDialog();
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
		}

		/// <summary>
		/// Catches all menu shortcuts
		/// </summary>
		private void RibbonWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			e.Handled = true;
			bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			if (ctrl)
				switch (e.Key)
				{
					case Key.N:
						NewSource(sender, null);
						return;
					case Key.O:
						Open(sender, null);
						return;
					case Key.S:
						Save(sender, null);
						return;
				}

			e.Handled = false;
		}

		private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Save window state & size
			GlobalProperties.Instance.lastFormSize = new Size(Width,Height);
			GlobalProperties.Instance.lastFormState = this.WindowState;

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
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		private void BuildSolution_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.BuildManagement.Build();
		}

		private void BuildToStandAlone_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.BuildManagement.BuildSingle();
		}
	}
}
