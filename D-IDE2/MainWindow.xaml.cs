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
using Microsoft.Windows.Controls.Ribbon;

namespace D_IDE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			var src = new DSourceDocument();
			src.ShowAsDocument(DockMgr);
		}

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

		private void SaveAs(object sender, RoutedEventArgs e)
		{

		}

		private void Exit(object sender, RoutedEventArgs e)
		{

		}

		private void Settings(object sender, RoutedEventArgs e)
		{

		}
	}
}
