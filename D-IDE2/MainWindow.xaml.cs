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

namespace D_IDE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow
	{
		#region Properties
		ProjectExplorer Panel_ProjectExplorer = new ProjectExplorer();
		#endregion

		#region GUI Interactions
		public void UpdateProjectExplorer()
		{
			Panel_ProjectExplorer.Update();
		}
		#endregion

		public MainWindow()
		{
			InitializeComponent();

			IDEManager.MainWindow = this;

            // Load global settings
            GlobalProperties.Init();

			// Load language bindings
			LanguageLoader.Bindings.Add(new GenericFileBinding());
			LanguageLoader.LoadLanguageInterface("D-IDE.D.dll", "D_IDE.D.DLanguageBinding");

			UpdateLastFilesMenus();

			// Init panels and their layouts
			Panel_ProjectExplorer.Show(DockMgr,AvalonDock.AnchorStyle.Left);
		}

		#region Ribbon buttons

		private void NewSource(object sender, RoutedEventArgs e)
		{
			var sdlg = new NewSrcDlg();
			if (sdlg.ShowDialog().Value)
			{

			}
		}

		private void NewProject(object sender, RoutedEventArgs e)
		{
			var pdlg = new NewProjectDlg(NewProjectDlg.DialogMode.CreateNew | (IDEManager.CurrentSolution!=null?NewProjectDlg.DialogMode.Add:0));

			pdlg.ProjectDir = GlobalProperties.Current.DefaultProjectDirectory;
			
			if (pdlg.ShowDialog().Value)
			{
				if (IDEManager.CurrentSolution != null && pdlg.AddToCurrentSolution)
					IDEManager.ProjectManagement.AddNewProjectToCurrentSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdlg.ProjectDir);
				else if (!pdlg.AddToCurrentSolution)
					IDEManager.CurrentSolution = IDEManager.ProjectManagement.CreateNewProjectAndSolution(
						pdlg.SelectedLanguageBinding,
						pdlg.SelectedProjectType,
						pdlg.ProjectName,
						pdlg.ProjectDir,
						pdlg.SolutionName);

				UpdateProjectExplorer();
			}
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.InitialDirectory = GlobalProperties.Current.DefaultProjectDirectory;
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
			var ce=IDEManager.CurrentEditor;
			if (ce == null)
				return;

			var dlg = new SaveFileDialog();

			// Add filter
			dlg.Filter = "All files (*.*)|*.*";
			dlg.FileName=ce.AbsoluteFilePath;

			if (dlg.ShowDialog().Value)
				IDEManager.EditingManagement.SaveCurrentFileAs(dlg.FileName);
		}

		private void Exit(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Settings(object sender, RoutedEventArgs e)
		{

		}

		#endregion

		public void UpdateLastFilesMenus()
		{
			Button_Open.Items.Clear();

			var mi = new RibbonApplicationMenuItem();
			mi.Header = "File";
			mi.ToolTipTitle = "Open File (Ctrl+O)";
			mi.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/file.png",UriKind.Relative));
			mi.Click += Open;
			Button_Open.Items.Add(mi);

			// First add recent files
			if (GlobalProperties.Current.LastFiles.Count > 0 || GlobalProperties.Current.LastProjects.Count > 0)
				Button_Open.Items.Add(new RibbonSeparator());

			foreach (var i in GlobalProperties.Current.LastFiles)
			{
				mi = new RibbonApplicationMenuItem();
				mi.Header = i;
				mi.Click+=delegate(Object o,RoutedEventArgs _e)
				{
					IDEManager.EditingManagement.OpenFile(i);
				};
				Button_Open.Items.Add(mi);
			}

			// Then add recent projects
			if(GlobalProperties.Current.LastFiles.Count>0 && GlobalProperties.Current.LastProjects.Count>0)
				Button_Open.Items.Add(new RibbonSeparator());

			foreach (var i in GlobalProperties.Current.LastProjects)
			{
				mi = new RibbonApplicationMenuItem();
				mi.Header = i;
				mi.Click += delegate(Object o, RoutedEventArgs _e)
				{
					IDEManager.EditingManagement.OpenFile(i);
				};
				Button_Open.Items.Add(mi);
			}
		}

		/// <summary>
		/// Catches all menu shortcuts
		/// </summary>
		private void RibbonWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
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
		}

		private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}
	}
}
