
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
using D_IDE.Core.Controls;
using D_IDE.Core;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaction logic for SearchResultPanel.xaml
	/// </summary>
	public partial class SearchResultPanel : DockableContent, ISearchResultPanel
	{
		public SearchResultPanel()
		{
			Name = "SearchResults";
			DataContext=this;
			InitializeComponent();
		}

		public override bool Hide()
		{
			return base.Hide();
		}
		
		private void MainList_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item=e.OriginalSource as FrameworkElement;
			if (item == null)
				return;
			
			var sr=item.DataContext as SearchResult;

			if (sr == null || string.IsNullOrEmpty( sr.File))
				return;

			var ed = WorkbenchLogic.Instance.OpenFile(sr.File, sr.Offset);

			// Select match
			if (ed is EditorDocument)
				(ed as EditorDocument).Editor.SelectionLength = searchString.Length;
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
}