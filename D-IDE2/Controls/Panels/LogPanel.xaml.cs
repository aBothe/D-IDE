﻿using System;
using System.Text;
using System.Windows.Media;
using AvalonDock;
using D_IDE.Core;
using D_IDE.Core.Controls.Editor;
using ICSharpCode.AvalonEdit;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für LogPanel.xaml
	/// </summary>
	public partial class LogPanel : DockableContent
	{
		TextMarkerService tms1, tms2, tms3;

		public enum LogTab
		{
			System=1, Build=2, Output=3
		}

		public LogPanel()
		{
			Name = "LogPanel";
			InitializeComponent();

			tms1 = new TextMarkerService(Text_Sys);
			tms2 = new TextMarkerService(Text_Build);
			tms3 = new TextMarkerService(Text_Output);

			Text_Output.Encoding = Encoding.Unicode;
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

			tms1.RemoveAll((o) => { return true; });
			tms2.RemoveAll((o) => { return true; });
			tms3.RemoveAll((o) => { return true; });
		}

		void AddMarkerForOffsetUntilEnd(TextEditor editor, TextMarkerService tms, int beginOffset,ErrorType type)
		{
			if (type == ErrorType.Message || type==ErrorType.Information)
				return;

			var tm = new TextMarker(tms,beginOffset,true);
			
			tm.MarkerType = TextMarkerType.None;
			tm.ForegroundColor = type==ErrorType.Error? Colors.Red:Colors.OrangeRed;

			tms.Add(tm);
			tm.Redraw();
		}

		/// <summary>
		/// Appends text and scrolls down the log
		/// </summary>
		public void AppendOutput(string s,ErrorType errorType,ErrorOrigin origin)
		{
			if(errorType!=ErrorType.Message)
				s = "> ("+DateTime.Now.ToLongTimeString()+") "+s;
			TextEditor editor=null;
			var selTab = LogTab.System;
			TextMarkerService tms = null ;
			switch (origin)
			{
				default:
					editor = Text_Sys;
					selTab = LogTab.System;
					tms = tms1;
					break;
				case ErrorOrigin.Build:
					editor = Text_Build;
					selTab = LogTab.Build;
					tms = tms2;
					break;
				case ErrorOrigin.Debug:
				case ErrorOrigin.Program:
					selTab = LogTab.Output;
					editor = Text_Output;
					tms = tms3;
					break;
			}

			if (editor == null)
				return;			

			//TODO?: Find out why invoking the dispatcher thread blocks the entire application sometimes
			if (!Util.IsDispatcherThread)
				Dispatcher.BeginInvoke(new Action(() =>
				{
					SelectedTab = selTab;
					int off=editor.Document.TextLength;
					editor.AppendText(s + "\r\n");
					editor.ScrollToEnd();

					AddMarkerForOffsetUntilEnd(editor, tms, off,errorType);
				}),System.Windows.Threading.DispatcherPriority.Background);
			else
			{
				int off = editor.Document.TextLength;
				SelectedTab = selTab;
				editor.AppendText(s + "\r\n");
				editor.ScrollToEnd();

				AddMarkerForOffsetUntilEnd(editor, tms, off,errorType);
			}
		}
	}
}
