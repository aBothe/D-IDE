using System.Collections.Generic;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;

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

			UpdateLastFilesMenus();

			var d = new EditorDocument();
			d.ShowAsDocument(DockMgr);
		}

		#region Ribbon buttons

		private void NewSource(object sender, RoutedEventArgs e)
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
