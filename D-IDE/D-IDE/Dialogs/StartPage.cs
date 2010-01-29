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

namespace D_IDE
{
	public partial class StartPage : DockContent
	{
		public StartPage()
		{
			InitializeComponent();
			DockAreas = DockAreas.Document;

			doCheckAtStartCBox.Checked = D_IDE_Properties.Default.RetrieveNews;

			if (doCheckAtStartCBox.Checked)
				UpdateNews();
		}

		Thread newsTh;
		List<NewsEntry> nl = new List<NewsEntry>();
		public void UpdateNews()
		{
			if (newsTh == null || newsTh.ThreadState == ThreadState.Stopped)
				newsTh = new Thread(delegate(object o)
				{
					try
					{
						nl.Clear();
						XmlTextReader xtr = new XmlTextReader(Program.news_php + "?xml=1&max=30&fromIDE=1");
						try
						{
							xtr.ReadStartElement(); // Skip initial element

							while (xtr.Name == "n" && xtr.NodeType == XmlNodeType.Element)
							{
								int nid = Convert.ToInt32(xtr.GetAttribute("id"));
								long timestamp = Convert.ToInt64(xtr.GetAttribute("timestamp"));
								string content = xtr.ReadElementContentAsString();

								nl.Add(new NewsEntry(nid, D_IDE_Properties.DateFromUnixTime(timestamp).ToLocalTime(), content));
							}
						}
						catch (Exception ex) { Form1.thisForm.Log(ex.Message); }
					}
					catch (Exception ex) { Form1.thisForm.Log(ex.Message); }

				});
			//if(!newsTh.IsAlive)
			newsTh.Start();
			chTimer.Start();
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
				Form1.thisForm.Open(D_IDE_Properties.Default.lastProjects[lastProjects.SelectedIndex]);
		}

		private void lastFiles_DoubleClick(object sender, EventArgs e)
		{
			if (lastFiles.SelectedItems.Count > 0)
				Form1.thisForm.Open(D_IDE_Properties.Default.lastFiles[lastFiles.SelectedIndex]);
		}

		private void chTimer_Tick(object sender, EventArgs e)
		{
			if (newsTh.IsAlive) return;
			News = nl;
			chTimer.Stop();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			UpdateNews();
		}

		private void doCheckAtStartCBox_CheckedChanged(object sender, EventArgs e)
		{
			D_IDE_Properties.Default.RetrieveNews = doCheckAtStartCBox.Checked;
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			Form1.thisForm.NewProject(sender, e);
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
