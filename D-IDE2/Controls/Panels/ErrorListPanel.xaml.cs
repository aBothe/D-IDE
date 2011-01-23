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
using System.ComponentModel;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für ListPanel.xaml
	/// </summary>
	public partial class ErrorListPanel : DockableContent
	{
		public ErrorListPanel()
		{
			DataContext = this;
			InitializeComponent();
		}

		public readonly List<GenericError> Errors = new List<GenericError>();

		public void RefreshErrorList()
		{
			var selIndex = MainList.SelectedIndex;

			MainList.ItemsSource = Errors;

			if (MainList.Items.Count > selIndex)
				MainList.SelectedIndex = selIndex;
		}

		private void MainList_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item=e.OriginalSource as FrameworkElement;
			var err=item.DataContext as GenericError;

			if (err == null)
				return;

			var editor = IDEManager.EditingManagement.OpenFile(err.FileName) as EditorDocument;

			if(editor!=null)
				editor.Editor.Select(editor.Editor.Document.GetOffset(err.Location.Line,err.Location.Column),0);
		}
	}
}
