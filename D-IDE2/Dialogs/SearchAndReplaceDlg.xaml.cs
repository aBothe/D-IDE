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
using AvalonDock;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für SearchAndReplaceDlg.xaml
	/// </summary>
	public partial class SearchAndReplaceDlg : DockableContent
	{
		public SearchAndReplaceDlg()
		{
			InitializeComponent();
			LoadSearchOptions();
		}

		public void LoadSearchOptions()
		{
			var fsm = IDEManager.FileSearchManagement.Instance;

			comboBox_InputString.Text = fsm.CurrentSearchString;
			comboBox_ReplaceString.Text = fsm.CurrentReplaceString;

			comboBox_InputString.ItemsSource = fsm.LastSearchStrings;
			comboBox_ReplaceString.ItemsSource = fsm.LastReplaceStrings;

			comboBox_SearchLocation.SelectedIndex = (int)fsm.CurrentSearchLocation;

			checkBox_CaseSensitive.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.CaseSensitive);
			checkBox_SearchUpward.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.Upward);
			checkBox_WordOnly.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.FullWord);
		}

		public void ApplySearchOptions()
		{
			var fsm = IDEManager.FileSearchManagement.Instance;
			
			fsm.CurrentSearchString = comboBox_InputString.Text;
			fsm.CurrentReplaceString = comboBox_ReplaceString.Text;

			comboBox_InputString.ItemsSource = fsm.LastSearchStrings;
			comboBox_ReplaceString.ItemsSource = fsm.LastReplaceStrings;

			fsm.CurrentSearchLocation = (IDEManager.FileSearchManagement.SearchLocations)comboBox_SearchLocation.SelectedIndex;

			fsm.SearchOptions = 0;

			if (checkBox_CaseSensitive.IsChecked.Value)
				fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.CaseSensitive;

			if (checkBox_SearchUpward.IsChecked.Value)
				fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.Upward;

			if (checkBox_WordOnly.IsChecked.Value)
				fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.FullWord;
		}

		public void DoFindNext()
		{
			if (string.IsNullOrEmpty(comboBox_InputString.Text))
			{
				MessageBox.Show("Search string must not be empty!");
				return;
			}

			ApplySearchOptions();

			IDEManager.FileSearchManagement.Instance.FindNext();
		}

		private void FindNext_Click(object sender, RoutedEventArgs e)
		{
			DoFindNext();
			comboBox_InputString.Focus();
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			comboBox_InputString.Focus();
		}
	}
}
