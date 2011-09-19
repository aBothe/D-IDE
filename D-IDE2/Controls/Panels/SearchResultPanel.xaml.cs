/*
 * Created by SharpDevelop.
 * User: Alexander
 * Date: 09/19/2011
 * Time: 13:34
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AvalonDock;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaction logic for SearchResultPanel.xaml
	/// </summary>
	public partial class SearchResultPanel : DockableContent
	{
		public SearchResultPanel()
		{
			DataContext=this;
			InitializeComponent();
		}
		
		private void MainList_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item=e.OriginalSource as FrameworkElement;
			if (item == null)
				return;
			
			var sr=item.DataContext as SearchResult;

			if (sr == null || string.IsNullOrEmpty( sr.FileName))
				return;

			IDEManager.EditingManagement.OpenFile(sr.FileName,sr.Line,sr.Column);
		}
		
		string searchString;
		public string SearchString
		{
			get{return searchString;}
			set{searchString=value;
			
				if(string.IsNullOrEmpty(searchString))
					Title="Search Results";
				else
					Title="Search Results for \""+searchString+'\"';
			}
		}
		
		public SearchResult[] Results{
			get{return MainList.ItemsSource as SearchResult[];}
			set{MainList.ItemsSource=value;}
		}
	}
	
	public class SearchResult
	{
		public string File;
		public int Line;
		public int Column;
		
		public string CodeSnippet;
	}
}