using System.Collections.Generic;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;
using System.Windows.Media;
using System;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using D_IDE.Core;

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
		#endregion

		public MainWindow()
		{
			InitializeComponent();

			// Init the IDE interface
			IDEInterface.Current = new IDEInterface();

			LanguageLoader.LoadLanguageInterface("D-IDE.D.dll","D_IDE.D.DLanguageBinding");

			UpdateNewMenuButton();

			UpdateLastFilesMenus();

			var d = new EditorDocument();
			d.ShowAsDocument(DockMgr);
		}

		/// <summary>
		/// Puts all language specific file types or project types into the new button of the main menu
		/// </summary>
		void UpdateNewMenuButton()
		{
			Button_New.Items.Clear();

			// Loop through all languages
			foreach (var l in LanguageLoader.Bindings)
			{
				var lb = new RibbonApplicationMenuItem()
				{
					Header = l.LanguageName,
					ImageSource = l.LanguageIcon as ImageSource,
					Tag=l
				};

				// Add module types
				foreach (var ft in l.ModuleTypes)
				{
					var i=new RibbonApplicationMenuItem()
					{
						Header = ft.Name,
						ToolTipDescription=ft.Description,
						ImageSource = ft.SmallImage as ImageSource,
						Tag=ft
					};
					i.Click+=NewLanguageSource;
					lb.Items.Add(i);
				}

				// If projects supported...
				if (l.ProjectsSupported)
				{
					if (lb.Items.Count > 0)
						lb.Items.Add(new RibbonSeparator());

					// Add project types
					foreach (var ft in l.ProjectTypes)
					{
						var i = new RibbonApplicationMenuItem()
						{
							Header = ft.Name,
							ToolTipDescription = ft.Description,
							ImageSource = ft.SmallImage as ImageSource,
							Tag = ft
						};
						i.Click += NewLanguageSource;
						lb.Items.Add(i);
					}
				}
				Button_New.Items.Add(lb);
			}

			if (Button_New.Items.Count > 0)
				Button_New.Items.Add(new RibbonSeparator());

			var gi=new RibbonApplicationMenuItem() {
				Header="Text file"
			};
			gi.Click += NewGenericSource;
			Button_New.Items.Add(gi);
		}

		#region Ribbon buttons

		private void NewLanguageSource(object sender, RoutedEventArgs e)
		{
			
		}

		void NewGenericSource(object sender, RoutedEventArgs e)
		{

		}

		private void NewProject(object sender, RoutedEventArgs e)
		{

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
