using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Net;
using System.IO;
using System.Windows;
using System.Threading;
using D_IDE.Dialogs;
using System.Windows.Threading;
using System.Collections.Specialized;

namespace D_IDE
{
    public class IDEUtil:Util
    {
		#region Auto updater
		static string UpdaterExe = ApplicationStartUpPath + "\\D-IDE.Updater.exe";
		static string FileVersionFile = ApplicationStartUpPath + "\\LastModificationTime";
		const string TimeStampUrl = "http://d-ide.sourceforge.net/d-ide.php?action=fileversion";

		public static void CheckForUpdates(bool ForceWatch)
		{
			if (GlobalProperties.Instance.WatchForUpdates || ForceWatch)
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
					else if (ForceWatch)
						MessageBox.Show("No update available.","Obtaining online status.");
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
					var wc = new WebClient();
					var LastOnlineModTime = wc.DownloadString(TimeStampUrl);

					if (!File.Exists(FileVersionFile))
						return false;

					// Check if offline version is already the latest
					if (File.ReadAllText(FileVersionFile) == LastOnlineModTime)
						return false;
				}
				catch
				{
					//ErrorLogger.Log(ex,ErrorType.Information,ErrorOrigin.System);
					return false;
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
			IDEManager.Instance.MainWindow.Dispatcher.Invoke(new Action(() =>
			IDEManager.Instance.MainWindow.Close()));
		}
		#endregion
	}

	public class IDELogger : ErrorLogger
	{
		const string ReportContributionUrl = "http://d-ide.sf.net/contrib_error.php";

		readonly MainWindow Owner;
		public IDELogger(MainWindow Owner)
		{
			this.Owner = Owner;
		}

		public static void SendErrorReport(string message,string userComment)
		{
			try
			{
				var versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3);

				var wc = new WebClient();

				var nvc = new NameValueCollection();
				nvc.Add("report_data", message);
				nvc.Add("comment", userComment);
				nvc.Add("ide_version", versionString);

				wc.UploadValues(new Uri(ReportContributionUrl), "POST", nvc);
				MessageBox.Show("Report uploaded!");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Error occurred while trying to send error report - And yes, I know that it sounds stupid:\r\n\r\n"+ex);
			}
		}

		protected override void OnLog(Exception ex, ErrorType ErrorType, ErrorOrigin Origin)
		{
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
				{
					new CrashDialog(ex).ShowDialog();
				}));
			
			//base.OnLog(ex, ErrorType, Origin);
		}

		protected override void OnLog(string Message, ErrorType etype, ErrorOrigin Origin)
		{
			if(Origin==ErrorOrigin.System)
				base.OnLog(Message, etype,Origin);
			try
			{
				if (Owner != null && Owner.Panel_Log != null)
					Owner.Panel_Log.AppendOutput(Message, etype, Origin);
			}
			catch { }
		}
	}
}
