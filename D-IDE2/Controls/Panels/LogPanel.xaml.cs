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

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für LogPanel.xaml
	/// </summary>
	public partial class LogPanel : DockableContent
	{
		public LogPanel()
		{
			InitializeComponent();
		}

		public void Clear()
		{
			MainText.Clear();
		}

		public string LogText
		{
			get { return MainText.Text; }
			set { MainText.Text = value; }
		}

		/// <summary>
		/// Appends text and scrolls down the log
		/// </summary>
		public void AppendOutput(string s)
		{
			Dispatcher.Invoke(new EventHandler(delegate(object o,EventArgs e) {
				MainText.AppendText(o as string + "\r\n");
				MainText.ScrollToEnd();
			}), s,EventArgs.Empty);
		}
	}
}
