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
using System.Threading;
using D_IDE.Core;
using System.Xml;
using System.IO;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für StartPage.xaml
	/// </summary>
	public partial class StartPage : DockableContent, System.ComponentModel.INotifyPropertyChanged
	{
		public StartPage()
		{
			DataContext = this;
			InitializeComponent();
		}

		private void Button_Open_Click(object sender, RoutedEventArgs e)
		{
			if (RecentProjectsList.SelectedItem != null)
				IDEManager.EditingManagement.OpenFile((RecentProjectsList.SelectedItem as RecentPrjItem).Path);
		}

		private void Button_CreatePrj_Click(object sender, RoutedEventArgs e)
		{
			IDEManager.Instance.MainWindow.DoNewProject();
		}

		public bool CheckForNewsFlag
		{
			get { return GlobalProperties.Instance.RetrieveNews; }
			set { GlobalProperties.Instance.RetrieveNews = value; }
		}

		readonly List<NewsItem> _LastRetrievedNews = new List<NewsItem>();
		public NewsItem[] LastRetrievedNews { get { return _LastRetrievedNews.ToArray(); } }

		public void RefreshNews()
		{
			new Thread(delegate()
			{
				Thread.CurrentThread.IsBackground = true;
				try
				{
					var data = new System.Net.WebClient().DownloadString("http://d-ide.sourceforge.net/classes/news.php?xml=1&max=20&fromIDE=1");

					var xr = new XmlTextReader(new StringReader(data));
					_LastRetrievedNews.Clear();

					while (xr.Read())
					{
						if (xr.LocalName != "n")
							continue;

						var i = new NewsItem();
						if (xr.MoveToAttribute("id"))
							i.Id = Convert.ToInt32(xr.GetAttribute("id"));
						if (xr.MoveToAttribute("timestamp"))
							i.Timestamp = Util.DateFromUnixTime(Convert.ToInt64(xr.GetAttribute("timestamp")));
						xr.MoveToElement();
						i.Content = Util.StripXmlTags(xr.ReadString());

						_LastRetrievedNews.Add(i);
					}
					xr.Close();
				}
				catch (Exception ex) { ErrorLogger.Log(ex); }

				NewsList.Dispatcher.Invoke(new Util.EmptyDelegate(delegate()
				{
					NewsList.ItemsSource = _LastRetrievedNews;
				}));
			}).Start();
		}

		public void RefreshLastProjects()
		{
			var prjs = new List<RecentPrjItem>();

			foreach (var prjfn in GlobalProperties.Instance.LastProjects)
			{
				prjs.Add(new RecentPrjItem() { Path=prjfn});
			}

			RecentProjectsList.ItemsSource = prjs;
		}

		private void DockableContent_Loaded(object sender, RoutedEventArgs e)
		{
			if (CheckForNewsFlag)
				RefreshNews();

			RefreshLastProjects();
		}

		public class NewsItem
		{
			public int Id { get; set; }
			public string Content { get; set; }
			public DateTime Timestamp { get; set; }
			public string TimeString { get { return Timestamp.ToLocalTime().ToString(); } }
		}

		public class RecentPrjItem
		{
			public string FileName { get { return System.IO.Path.GetFileName(Path); } }
			public string Path { get; set; }
		}

		private void RecentProjectsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var i = RecentProjectsList.SelectedItem as RecentPrjItem;
			if(i!=null)
				IDEManager.EditingManagement.OpenFile(i.Path);
		}
	}
}
