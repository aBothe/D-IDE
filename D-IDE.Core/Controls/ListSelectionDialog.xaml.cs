using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace D_IDE.Core.Controls
{
	/// <summary>
	/// Interaktionslogik für ListSelectionDialog.xaml
	/// </summary>
	public partial class ListSelectionDialog : Window
	{
		public ListSelectionDialog()
		{
			InitializeComponent();

			SelectionList.Focus();
		}

		public ListBox List { get { return SelectionList; } }

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void SelectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			DialogResult = true;
		}
	}
}
