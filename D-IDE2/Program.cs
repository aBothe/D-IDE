using System;
using System.Diagnostics;
using System.Windows;
using D_IDE.Dialogs;

namespace D_IDE
{
	/*internal class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var CurrentApp = new Application();
			CurrentApp.Run(new MainWindow(args));
		}
	}*/
	
	internal class Program : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		[STAThread]
		public static void Main(string[] args)
		{
			if(Debugger.IsAttached)
				new Program().Run(args);
			else try
				{
					new Program().Run(args);
				}
				catch (Exception ex) { new CrashDialog(ex).ShowDialog(); }
		}

		public Program()
		{
			IsSingleInstance = !GlobalProperties.AllowMultipleProgramInstances;
		}

		/// <summary>
		/// "Entry" point that inits the D-IDE Mainform
		/// </summary>
		protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
		{
			var CurrentApp = new Application();
			
			var mw = new MainWindow(eventArgs.CommandLine);
			CurrentApp.MainWindow = mw;
			CurrentApp.Run();
			// Return false to avoid base.Run() throwing an exception that no Winforms MainWindow could be found
			return false;
		}

		/// <summary>
		/// Simply activates the first instance and opens all files passed as command arguments
		/// </summary>
		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs eventArgs)
		{
			IDEManager.Instance.MainWindow.Activate();

			foreach(var s in eventArgs.CommandLine)
				IDEManager.Instance.OpenFile(s);
		}
	}
}
