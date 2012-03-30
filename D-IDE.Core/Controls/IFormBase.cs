using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;

namespace D_IDE.Core.Controls
{
	public interface IFormBase
	{
		AvalonDock.DockingManager DockManager { get; }
		System.Windows.Threading.Dispatcher Dispatcher { get; }

		ISearchResultPanel SearchResultPanel { get; }

		void RefreshMenu();
		void RefreshGUI();
		void RefreshErrorList();
		void RefreshTitle();
		void RefreshProjectExplorer();

		string LeftStatusText { get; set; }
		string SecondLeftStatusText { get; set; }
		string ThirdStatusText { get; set; }
	}

	public interface ISearchResultPanel
	{
		string SearchString
		{
			get;
			set;
		}

		IEnumerable<SearchResult> Results
		{
			get;
			set;
		}

		void Show();
	}

	public class SearchResult
	{
		public string File { get; set; }
		public string FileName
		{
			get
			{
				return System.IO.Path.GetFileName(File);
			}
		}

		public int Offset { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }

		public string CodeSnippet { get; set; }
	}
}
