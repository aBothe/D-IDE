using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Aga.Controls.Tree;
using AvalonDock;
using D_IDE.Core;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für StartPage.xaml
	/// </summary>
	public partial class StartPage : DockableContent, System.ComponentModel.INotifyPropertyChanged
	{
		public StartPage()
		{
			Name = "StartPage";
			DataContext = this;
			InitializeComponent();

			Label_Version.Content = "Version " + System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString(4);
		}

		private void Button_Open_Click(object sender, RoutedEventArgs e)
		{
			if (RecentProjectsList.SelectedNode != null && RecentProjectsList.SelectedNode.Tag is RecentPrjItem)
				IDEManager.Instance.OpenFile((RecentProjectsList.SelectedNode.Tag as RecentPrjItem).Path);
		}

		private void Button_CreatePrj_Click(object sender, RoutedEventArgs e)
		{
			WorkbenchLogic.Instance.DoNewProject();
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
				catch {  }

				NewsList.Dispatcher.Invoke(new Action(delegate()
				{
					NewsList.ItemsSource = _LastRetrievedNews;
				}));
			}).Start();
		}

		public void RefreshLastProjects()
		{
			if (!RecentProjectsList.IsInitialized)
				return;

			if (RecentProjectsList.Model == null)
				RecentProjectsList.Model = new LastProjectsModel();
			else 
				RecentProjectsList.Refresh();
		}

		class LastProjectsModel : Aga.Controls.Tree.ITreeModel
		{
			public System.Collections.IEnumerable GetChildren(object parent)
			{
				foreach (var prjfn in GlobalProperties.Instance.LastProjects)
					yield return new RecentPrjItem() { Path = prjfn };
			}

			public bool HasChildren(object parent)
			{
				return false;
			}
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
			var i = (RecentProjectsList.SelectedItem as TreeNode).Tag as RecentPrjItem;
			if(i!=null)
				IDEManager.Instance.OpenFile(i.Path);
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.ToString());
		}
	}
}
