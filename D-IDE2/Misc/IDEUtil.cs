using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using Parser.Core;
using System.Net;
using System.IO;
using System.Windows;
using System.Threading;

namespace D_IDE
{
    public class IDEUtil:Util
    {
        public static CodeLocation ToCodeLocation(ICSharpCode.AvalonEdit.Document.TextLocation Caret)
        {
            return new CodeLocation(Caret.Column + 1, Caret.Line + 1);
        }
        public static CodeLocation ToCodeLocation(CodeLocation loc)
        {
            return loc;
        }

		#region Auto updater
		static string UpdaterExe = ApplicationStartUpPath + "\\D-IDE.Updater.exe";
		static string FileVersionFile = ApplicationStartUpPath + "\\LastModificationTime";
		const string TimeStampUrl = "http://d-ide.sourceforge.net/d-ide.php?action=fileversion";

		public static void CheckForUpdates()
		{
			if (GlobalProperties.Instance.WatchForUpdates)
				new Thread(() =>
				{
					Thread.CurrentThread.IsBackground = true;
					if (IDEUtil.IsUpdateAvailable)
					{
						if (MessageBox.Show("A program update is available. Install it now?\r\nWarning: The program will be closed then!", 
							"Update available", 
							MessageBoxButton.YesNo, 
							MessageBoxImage.Question, 
							MessageBoxResult.Yes) == MessageBoxResult.Yes)
							IDEUtil.DoUpdate();
					}
				}).Start();
		}

		/// <summary>
		/// Checks if a new program version has been uploaded
		/// </summary>
		public static bool IsUpdateAvailable
		{
			get{
				// Get latest online file timestamp
				try
				{
					if (!File.Exists(FileVersionFile))
						return true;

					var LastOnlineModTime = new WebClient().DownloadString(TimeStampUrl);

					// Check if offline version is already the latest
					if (File.ReadAllText(FileVersionFile) == LastOnlineModTime)
						return false;
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
				return true;
			}
		}

		public static void DoUpdate()
		{
			if (!File.Exists(UpdaterExe))
			{
				ErrorLogger.Log(UpdaterExe+" not found! Cannot proceed with update!",ErrorType.Error,ErrorOrigin.System);
				return;
			}

			// Start the updater
			var upt=FileExecution.ExecuteAsync(UpdaterExe, "-a -o \""+ApplicationStartUpPath+"\"", ApplicationStartUpPath, null);

			// Close main window - the D-IDE.exe will be overwritten!
			IDEManager.Instance.MainWindow.Dispatcher.Invoke(new EmptyDelegate(() =>
			IDEManager.Instance.MainWindow.Close()));
		}
		#endregion
	}

	public class IDELogger : ErrorLogger
	{
		readonly MainWindow Owner;
		public IDELogger(MainWindow Owner)
		{
			this.Owner = Owner;
		}

		protected override void OnLog(string Message, ErrorType etype, ErrorOrigin Origin)
		{
			if(Origin==ErrorOrigin.System)
				base.OnLog(Message, etype,Origin);

			Owner.Panel_Log.AppendOutput(Message,etype,Origin);
		}
	}
}
