using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace D_IDE
{
	static class Program
	{
		public static string cfgDir = Application.StartupPath+"\\config";
		public static string prop_file = "D-IDE.properties.dat";
		public static string ModuleCacheFile = "D-IDE.cache.dat";
		public static string LayoutFile = "D-IDE.layout.xml";
		public const string news_php = "http://d-ide.sourceforge.net/classes/news.php";
		public const string ver_txt = "http://d-ide.svn.sourceforge.net/viewvc/d-ide/ver.txt";
		public static App app;
		public static bool Parsing = false;
		/// <summary>
		/// Der Haupteinstiegspunkt für die Anwendung.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//try			{
				if(!Directory.Exists(Program.cfgDir))
					Directory.CreateDirectory(Program.cfgDir);

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				app = new App();

				Program.prop_file = cfgDir + "\\" + Program.prop_file;
				Program.ModuleCacheFile = cfgDir + "\\" + ModuleCacheFile;
				Program.LayoutFile = cfgDir + "\\" + LayoutFile;
				
				try
				{
					D_IDE_Properties.Load(prop_file);
					D_IDE_Properties.LoadGlobalCache(ModuleCacheFile);
				}
				catch(Exception ex)
				{
					MessageBox.Show(ex.Message + " (" + ex.Source + ")" + "\n\n" + ex.StackTrace, "Error while loading global settings");
				}

				app.Run(args);
			/*}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message + " (" + ex.Source + ")" + "\n\n" + ex.StackTrace, "Error while initalizing application");
			}*/
		}
	}

	/// <summary>
	///  We inherit from WindowsFormApplicationBase which contains the logic for the application model, including
	///  the single-instance functionality.
	/// </summary>
	class App : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		public App()
		{
			this.IsSingleInstance = D_IDE_Properties.Default.SingleInstance; // makes this a single-instance app
			this.EnableVisualStyles = true; // C# windowsForms apps typically turn this on.  We'll do the same thing here.
			this.ShutdownStyle = Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterMainFormCloses; // the vb app model supports two different shutdown styles.  We'll use this one for the sample.
		}

		public bool singleInstance
		{
			set { IsSingleInstance = value; }
		}

		/// <summary>
		/// This is how the application model learns what the main form is
		/// </summary>
		protected override void OnCreateMainForm()
		{
			List<string> args = new List<string>();
			args.AddRange(CommandLineArgs);
			this.MainForm = new Form1(args.ToArray());
		}

		protected override void OnShutdown()
		{
			D_IDE_Properties.Save(Program.prop_file);
			base.OnShutdown();
		}

		/// <summary>
		/// Gets called when subsequent application launches occur.  The subsequent app launch will result in this function getting called
		/// and then the subsequent instances will just exit.  You might use this method to open the requested doc, or whatever 
		/// </summary>
		/// <param name="eventArgs"></param>
		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
		{
			base.OnStartupNextInstance(e);

			e.BringToForeground = true;
			foreach(string file in e.CommandLine)
				Form1.thisForm.Open(file);
		}
	}
}
