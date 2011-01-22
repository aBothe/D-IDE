using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using D_IDE.Core;
using System.Collections.ObjectModel;
using AvalonDock;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für ListPanel.xaml
	/// </summary>
	public partial class ErrorListPanel : DockableContent
	{
		public ErrorListPanel()
		{
			InitializeComponent();

			MainList.ItemsSource = Errors;
		}

		public readonly ObservableCollection<BuildError> Errors = new ObservableCollection<BuildError>();

		private void MainList_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item=e.OriginalSource as FrameworkElement;
			var err=item.DataContext as BuildError;

			if (err == null)
				return;

			var editor = IDEManager.EditingManagement.OpenFile(err.FileName) as EditorDocument;

			if(editor!=null)
				editor.Editor.Select(editor.Editor.Document.GetOffset(err.Location.Line,err.Location.Column),0);
		}
	}
}
