using System.Collections.Generic;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;
using D_IDE.Core;
using D_IDE.Dialogs;
using D_IDE.Controls.Panels;

namespace D_IDE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow
	{
		#region Properties
		AbstractEditorDocument SelectedDocument
		{
			get {
				return DockMgr.ActiveDocument as AbstractEditorDocument;
				}
		}

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
			
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			
		}

		private void SaveAll(object sender, RoutedEventArgs e)
		{

		}

		private void SaveAs(object sender, RoutedEventArgs e)
		{

		}

		private void Exit(object sender, RoutedEventArgs e)
		{

		}

		private void Settings(object sender, RoutedEventArgs e)
		{

		}

		#endregion

		#region GUI-related stuff
		public void UpdateLastFilesMenus()
		{
			Button_Open.Items.Clear();

			// First add recent files
			var l = new List<string> { "aa","bbbb","ccc" };

			foreach (var i in l)
			{
				var mi = new RibbonApplicationMenuItem();
				mi.Header = System.IO.Path.GetFileName( i);
				mi.Tag = i;
				Button_Open.Items.Add(mi);
			}

			// Then add recent projects
			
		}
		#endregion
	}
}
