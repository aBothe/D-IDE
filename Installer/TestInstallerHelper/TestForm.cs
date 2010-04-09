using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
            string u1 = DIDE.Installer.InstallerHelper.GetLatestDMD1Url(),
                u2 = DIDE.Installer.InstallerHelper.GetLatestDMD2Url();
            int v1 = DIDE.Installer.InstallerHelper.GetLatestDMD1Version(),
                v2 = DIDE.Installer.InstallerHelper.GetLatestDMD2Version();

            System.Console.WriteLine(u1);
            System.Console.WriteLine(u2);
            System.Console.WriteLine(v1);
            System.Console.WriteLine(v2);

            System.Console.WriteLine(DIDE.Installer.LocalCompiler.InstallPathDMD1);
            System.Console.WriteLine(DIDE.Installer.LocalCompiler.InstallPathDMD1Version);
            System.Console.WriteLine(DIDE.Installer.LocalCompiler.InstallPathDMD2);
            System.Console.WriteLine(DIDE.Installer.LocalCompiler.InstallPathDMD2Version);

            DIDE.Installer.InstallerHelper.CreateConfigurationFile(@".\D-IDE.settings.xml");
        }
    }
}
