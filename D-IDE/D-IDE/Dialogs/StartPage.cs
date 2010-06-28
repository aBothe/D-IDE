using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using D_Parser;
using D_IDE;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.Threading;
using System.Net;

namespace D_IDE
{
	public partial class StartPage : DockContent
	{
		public StartPage()
		{
			InitializeComponent();
			DockAreas = DockAreas.Document;

			webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webclient_DownloadStringCompleted);

			doCheckAtStartCBox.Checked = D_IDE_Properties.Default.RetrieveNews;
		}

		WebClient webclient = new WebClient();
		public void UpdateNews()
		{
			if (!webclient.IsBusy)
				webclient.DownloadStringAsync(new Uri(Program.news_php + "?xml=1&max=30&fromIDE=1"));
		}

		void webclient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			List<NewsEntry> nl = new List<NewsEntry>();
			try
			{
				nl.Clear();
				XmlTextReader xtr = new XmlTextReader(new StringReader(e.Result));

				xtr.ReadStartElement(); // Skip initial element

				while (xtr.Name == "n" && xtr.NodeType == XmlNodeType.Element)
				{
					int nid = Convert.ToInt32(xtr.GetAttribute("id"));
					long timestamp = Convert.ToInt64(xtr.GetAttribute("timestamp"));
					string content = xtr.ReadElementContentAsString();

					nl.Add(new NewsEntry(nid, D_IDE_Properties.DateFromUnixTime(timestamp).ToLocalTime(), content));
				}
				News = nl;
			}
			catch (Exception ex) { D_IDEForm.thisForm.Log(ex.Message); }
		}


		public List<NewsEntry> News
		{
			set
			{
				string c = "<html><body><style>body{ font-size:11px; font-family:Arial;} " +
					".a{font-weight:bold;}" +
					".n{display:block;padding-bottom:15px;}" +
					"p{margin:0;padding:0;}" +
					"</style>";
				foreach (NewsEntry n in value)
				{
					c += "<div class=\"n\"><span class=\"a\">" + n.time.ToString() + "</span><br />" + n.content + "</div>";
				}
				c += "</body></html>";
				web.DocumentText = c;
			}
		}

		private void lastProjects_DoubleClick(object sender, EventArgs e)
		{
			if (lastProjects.SelectedItems.Count > 0)
				D_IDEForm.thisForm.Open(D_IDE_Properties.Default.lastProjects[lastProjects.SelectedIndex]);
		}

		private void lastFiles_DoubleClick(object sender, EventArgs e)
		{
			if (lastFiles.SelectedItems.Count > 0)
				D_IDEForm.thisForm.Open(D_IDE_Properties.Default.lastFiles[lastFiles.SelectedIndex]);
		}

		private void chTimer_Tick(object sender, EventArgs e)
		{
			//if (newsTh.IsAlive) return;
			//News = nl;
			chTimer.Stop();
		}

		private void button2_Click(object sender, EventArgs e)
		{
            Hide();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			UpdateNews();
		}

		private void doCheckAtStartCBox_CheckedChanged(object sender, EventArgs e)
		{
			D_IDE_Properties.Default.RetrieveNews = doCheckAtStartCBox.Checked;
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			D_IDEForm.thisForm.NewProject(sender, e);
		}

		private void StartPage_Shown(object sender, EventArgs e)
		{
			if (doCheckAtStartCBox.Checked)
			{
				BeginInvoke(new EventHandler(delegate(object s, EventArgs ea)
					{
						UpdateNews();
					}), null, EventArgs.Empty);
			}
		}
	}

	public struct NewsEntry
	{
		public int id;
		public DateTime time;
		public string content;

		public NewsEntry(int id, DateTime time, string content)
		{
			this.id = id;
			this.time = time;
			this.content = content;
		}

		public NewsEntry(DateTime time, string content)
		{
			this.id = 1;
			this.time = time;
			this.content = content;
		}
	}
}
