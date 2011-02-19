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

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für LogPanel.xaml
	/// </summary>
	public partial class LogPanel : DockableContent
	{
		public enum LogTab
		{
			System=1, Build=2, Output=3
		}

		public LogPanel()
		{
			InitializeComponent();
		}

		public LogTab SelectedTab{
			get { return (LogTab)(MainTabs.SelectedIndex+1);}
			set { MainTabs.SelectedIndex = (int)value - 1;	}
		}

		public void Clear()
		{
			Text_Sys.Clear();
			Text_Build.Clear();
			Text_Output.Clear();
		}

		/// <summary>
		/// Appends text and scrolls down the log
		/// </summary>
		public void AppendOutput(string s,ErrorType errorType,ErrorOrigin origin)
		{
			if(errorType!=ErrorType.Message)
			s = "> "+s;
			TextBox editor=null;
			LogTab selTab = LogTab.System;
			switch (origin)
			{
				case ErrorOrigin.System:
					editor = Text_Sys;
					selTab = LogTab.System;
					break;
				case ErrorOrigin.Build:
					editor = Text_Build;
					selTab = LogTab.Build;
					break;
				case ErrorOrigin.Debug:
				case ErrorOrigin.Program:
					selTab = LogTab.Output;
					editor = Text_Output;
					break;
			}

			//TODO: Find out why invoking the dispatcher thread blocks the entire application sometimes
			if (!Util.IsDispatcherThread)
				Dispatcher.BeginInvoke(new D_IDE.Core.Util.EmptyDelegate(() =>
				{
					SelectedTab = selTab;
					editor.AppendText(s + "\r\n");
					editor.ScrollToEnd();
				}),System.Windows.Threading.DispatcherPriority.Background);
			else
			{
				SelectedTab = selTab;
				editor.AppendText(s + "\r\n");
				editor.ScrollToEnd();
			}
		}
	}
}
