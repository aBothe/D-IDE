using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
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

			var ed = WorkbenchLogic.Instance.OpenFile(sr.File, sr.Offset) as EditorDocument;

			// Select match
			if (ed !=null)
				ed.Editor.Select(sr.Offset, searchString.Length);
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
		
		public IEnumerable<SearchResult> Results{
			get{ return MainList.ItemsSource as IEnumerable<SearchResult>; }
			set{ MainList.ItemsSource = value; }
		}
	}
}