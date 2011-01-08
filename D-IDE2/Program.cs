using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using D_IDE.Core;
using System.IO.Pipes;

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
	
	internal class Program :Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Program().Run(args);
		}

		public Program()
		{
			IsSingleInstance = GlobalProperties.AllowMultipleProgramInstances;
		}

		/// <summary>
		/// "Entry" point that inits the D-IDE Mainform
		/// </summary>
		protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
		{
			var CurrentApp = new Application();
			CurrentApp.MainWindow = new MainWindow(eventArgs.CommandLine.ToArray());
			CurrentApp.Run();
			// Return false to avoid base.Run() throwing an exception that no Winforms MainWindow could be found
			return false;
		}

		/// <summary>
		/// Simply activates the first instance and opens all files passed as command arguments
		/// </summary>
		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs eventArgs)
		{
			IDEManager.MainWindow.Activate();

			foreach(var s in eventArgs.CommandLine)
				IDEManager.EditingManagement.OpenFile(s);
		}
	}
}
