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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;

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
		}

		public ListView List { get { return SelectionList; } }

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
