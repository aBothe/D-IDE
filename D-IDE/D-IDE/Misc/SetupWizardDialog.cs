using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using D_IDE;

namespace D_IDE.Misc
{
    public partial class SetupWizardDialog : Form
    {
        public CompilerConfiguration.DVersion Version = CompilerConfiguration.DVersion.D2;

        public SetupWizardDialog(CompilerConfiguration.DVersion DMDVersion)
        {
            InitializeComponent();
            Version = DMDVersion;

            Text = "First usage setup for "+Version.ToString();
        }

        private void SelectBinPath(object sender, EventArgs e)
        {
            if (Directory.Exists(BinPath.Text)) fD.SelectedPath = BinPath.Text;

            if (fD.ShowDialog() == DialogResult.OK)
            {
                BinPath.Text = fD.SelectedPath;
            }
        }

        private void GuessImpPaths(object sender, EventArgs e)
        {
            string bin = BinPath.Text;
            if (!Directory.Exists(Path.IsPathRooted(bin)?bin:(Application.StartupPath+"\\"+bin))) return;

            int i = 0;
            if (Version == CompilerConfiguration.DVersion.D2 && (i = bin.IndexOf("dmd2")) > 0)
            {
                ImportPaths.Items.Clear();
                ImportPaths.Items.Add(bin.Substring(0, i + 4) + "\\src\\druntime\\import");
                ImportPaths.Items.Add(bin.Substring(0, i + 4) + "\\src\\phobos");
            }
        }

        private void AddPath(object sender, EventArgs e)
        {
            if (fD.ShowDialog() == DialogResult.OK && !ImportPaths.Items.Contains(fD.SelectedPath))
            {
                ImportPaths.Items.Add(fD.SelectedPath);
            }
        }

        private void RemovePath(object sender, EventArgs e)
        {
            if(ImportPaths.SelectedIndex>-1)
            ImportPaths.Items.RemoveAt(ImportPaths.SelectedIndex);
        }

        private void Skip(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void SaveandClose(object sender, EventArgs e)
        {
            if (!Directory.Exists(BinPath.Text))
            {
                MessageBox.Show("Binary path has to be created!");
                return;
            }

            DialogResult = DialogResult.OK;
        }

        public void ModifyConfiguration(ref CompilerConfiguration cc)
        {
            cc.BinDirectory = BinPath.Text;
            cc.ImportDirectories.Clear();
            foreach (object o in ImportPaths.Items)
                cc.ImportDirectories.Add((string)o);
        }

        public CompilerConfiguration CompilerConfiguration
        {
            get {
                CompilerConfiguration cc = new CompilerConfiguration(Version);
                ModifyConfiguration(ref cc);
                return cc;
            }
            set
            {
                BinPath.Text = value.BinDirectory;
                ImportPaths.Items.Clear();
                foreach (string p in value.ImportDirectories)
                    ImportPaths.Items.Add(p);
            }
        }
    }
}
