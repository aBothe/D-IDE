using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DIDE.Installer;
namespace TestInstallerHelper
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string file = @".\D-IDE.settings.xml";
            //if (File.Exists(file)) File.Delete(file);
            InstallerHelper.CreateConfigurationFile(file);

            InstallerHelper.Refresh();
            InstallerHelper.Initialize(@"C:\Users\Justin\AppData\Local\Temp\dmd.files.20100706.html");
            while (InstallerHelper.IsThreadActive)
            {
                Write(".");
                Application.DoEvents();
                Refresh();
            } WriteLine("DONE");

            WriteLine("Latest (online) DMD 1 Url       --> " + InstallerHelper.GetLatestDMD1Url());
            WriteLine("Latest (online) DMD 1 Version   --> " + InstallerHelper.GetLatestDMD1Version());
            WriteLine("Local (installed) DMD 1 Path    --> " + InstallerHelper.GetLocalDMD1Path());
            WriteLine("Local (installed) DMD 1 Version --> " + InstallerHelper.GetLocalDMD1Version());
            WriteLine("Latest (online) DMD 2 Url       --> " + InstallerHelper.GetLatestDMD2Url());
            WriteLine("Latest (online) DMD 2 Version   --> " + InstallerHelper.GetLatestDMD2Version());
            WriteLine("Local (installed) DMD 2 Path    --> " + InstallerHelper.GetLocalDMD2Path());
            WriteLine("Local (installed) DMD 2 Version --> " + InstallerHelper.GetLocalDMD2Version());
            WriteLine("Local Path Valid DMD 1          --> " + InstallerHelper.IsValidDMDInstallForVersion(1, InstallerHelper.GetLocalDMD1Path()));
            WriteLine("Local Path Valid DMD 2          --> " + InstallerHelper.IsValidDMDInstallForVersion(2, InstallerHelper.GetLocalDMD2Path()));
            WriteLine("Generated Config File           --> " + file);
            WriteLine("----------------------------------------------------------------------------------");
            WriteLine(File.ReadAllText(file));
            WriteLine("----------------------------------------------------------------------------------");
        }

        private void TestForm_Load(object sender, EventArgs e)
        {

        }

        private void Write(string s, params object[] args)
        {
            if (args.Length > 0) textBox1.AppendText(string.Format(s, args));
            else textBox1.AppendText(s);
        }

        private void WriteLine(string s, params object[] args) 
        {
            Write(s, args);
            textBox1.AppendText(Environment.NewLine);
        }
    }
}
