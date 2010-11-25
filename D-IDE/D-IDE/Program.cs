using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace D_IDE
{
    static class Program
    {
        public const string news_php = "http://d-ide.sourceforge.net/classes/news.php";
        public const string ver_txt = "http://d-ide.svn.sourceforge.net/viewvc/d-ide/ver.txt";
        public static App app;
        public static bool Parsing = false;

        public static CachingScreen StartScreen;
        public static DateTime tdate;

        [STAThread]
        static void Main(string[] args)
        {
            tdate = DateTime.Now;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            app = new App();

            // Show startup popup
            StartScreen = new CachingScreen();
            if(!Debugger.IsAttached)StartScreen.Show();

            app.Run(args);
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
            this.IsSingleInstance = true; // makes this a single-instance app
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

            // Init global properties
            D_IDE_Properties.Init();

            this.MainForm = new D_IDEForm(args.ToArray());
        }

        protected override void OnShutdown()
        {
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
            foreach (string file in e.CommandLine)
                D_IDEForm.thisForm.Open(file);
        }
    }
}
