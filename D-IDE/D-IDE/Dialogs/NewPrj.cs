using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using D_Parser;

namespace D_IDE.Dialogs
{
	partial class NewPrj : Form
	{
		public DProject prj;
		public NewPrj()
		{
			InitializeComponent();

			prjdir.Text = D_IDE_Properties.Default.DefaultProjectDirectory;
			prjtype.SelectedIndex = 1; // Set default project type to console app......that's better, I think ;-)
			SelectedDVersion = CompilerConfiguration.DVersion.D2;
		}

		public string GetExt()
		{
			switch(prjtype.SelectedIndex)
			{
				case 0: // Windows
					return ".exe";
				case 1: // Console
					return ".exe";
				case 2: // DLL
					return ".dll";
				case 3: // LIB
					return ".lib";
				default:
					return ".exe";
			}
		}

		public CompilerConfiguration.DVersion SelectedDVersion
		{
			get
			{
				return (CompilerConfiguration.DVersion)(DVersionSelector.SelectedIndex + 1);
			}
			set
			{
				DVersionSelector.SelectedIndex = ((int)value) - 1;
			}
		}

		private void prjname_TextChanged(object sender, EventArgs e)
		{
			if(prjname.Text.StartsWith(tarfile.Text))
				tarfile.Text = prjname.Text;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if(prj == null)
				prj = new DProject();
			if(prjname.Text.Length < 1)
			{
				MessageBox.Show("Enter project name first");
				DialogResult = DialogResult.None;
				return;
			}
			prj.name = prjname.Text;
			prj.CompilerVersion = SelectedDVersion;

            switch (prjtype.SelectedIndex)
            {
                case 0: prj.type = DProject.PrjType.WindowsApp; break;
                case 1: prj.type = DProject.PrjType.ConsoleApp; break;
                case 2: prj.type = DProject.PrjType.Dll; break;
                case 3: prj.type = DProject.PrjType.StaticLib; break;
            }

			if(prjdir.Text.Length < 1)
			{
				MessageBox.Show("Enter project directory first");
				DialogResult = DialogResult.None;
				return;
			}

			if(!Directory.Exists(prjdir.Text))
			{
				if(MessageBox.Show("Project directory " + prjdir.Text + " does not exist! Shall D-IDE create it?","Selected project directory doesn't exist!",MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					Directory.CreateDirectory(prjdir.Text);
				}
				else
				{
					DialogResult = DialogResult.None;
					return;
				}
			}
			prj.basedir = prjdir.Text;
			if(createPrjDir.Checked)
			{
				prj.basedir += "\\" + prj.name;
				if(!Directory.Exists(prj.basedir))
				{
					try
					{
						Directory.CreateDirectory(prj.basedir);
					}
					catch(Exception ex)
					{
						MessageBox.Show("Error creating directory: "+ex.Message);
						DialogResult = DialogResult.None;
						return;
					}
				}
			}

			if(tarfile.Text.Length < 1)
			{
				MessageBox.Show("Enter target filename first");
				DialogResult = DialogResult.None;
				return;
			}
			prj.targetfilename = tarfile.Text;

			prj.prjfn = prj.basedir + "\\" + prj.name + DProject.prjext;
			MessageBox.Show("Project will be saved to \"" + prj.prjfn + "\"");

			prj.Save();
			DialogResult = DialogResult.OK;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void button3_Click(object sender, EventArgs e)
		{
            
			fD.SelectedPath = prjdir.Text;
			if(fD.ShowDialog() == DialogResult.OK)
			{
				prjdir.Text = fD.SelectedPath;
			}
		}

        private void prjname_KeyUp(object sender, KeyEventArgs e)
        {
            if (prjname.Text.Length>0 && prjname.Text.StartsWith(tarfile.Text.Substring(0,tarfile.Text.Length-1)) && e.KeyCode==Keys.Back)
                tarfile.Text = prjname.Text.Substring(0,prjname.Text.Length);
        }
	}
}
