using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using D_IDE.Core;
using System.IO.Pipes;
using System.Diagnostics;
using System.IO;

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
			var dir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)+"\\D-IDE.config";
			var file = dir + "\\a.xml";

			Util.CreateDirectoryRecursively(dir);

			File.WriteAllText(file,"initial content");

			File.AppendAllText(file, "s n stuff");

			File.WriteAllText(file, "second content");

			File.Delete(file);

			File.WriteAllText(file, "third content");

			if(Debugger.IsAttached)
				new Program().Run(args);
			else try
				{
					new Program().Run(args);
				}
				catch (Exception ex) { ErrorLogger.Log(ex); }
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
			
			var mw = new MainWindow(eventArgs.CommandLine.ToArray());
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
				IDEManager.EditingManagement.OpenFile(s);
		}
	}
}
