using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using D_IDE.CodeCompletion;
using D_Parser;
using D_Parser.CodeCompletion;
using D_IDE.Properties;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Gui.InsightWindow;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing.Drawing2D;

namespace D_IDE
{
	class ProjectPropertyPage : DockContent
	{
		private GroupBox groupBox5;
		private Button button2;
		private Button button7;
		private ListView Files;
		private GroupBox groupBox4;
		private TextBox lnkargs;
		private Label label6;
		private Label label5;
		private TextBox cpargs;
		private CheckBox checkBox1;
		private GroupBox groupBox3;
		private TextBox execargs;
		private GroupBox groupBox2;
		private Button button6;
		private Button button5;
		private Button button4;
		private TextBox tlib;
		private ListBox libs;
		private GroupBox groupBox1;
		private Button button3;
		private TextBox prjdir;
		private ComboBox prjtype;
		private TextBox tarfile;
		private TextBox prjname;
		private FolderBrowserDialog fD;
		private Button button1;
		public DProject project;

		public ProjectPropertyPage(DProject project)
		{
			if(project == null) return;
			this.project = project;
			this.DockAreas = DockAreas.Document;
			this.TabText = project.name;
			InitializeComponent();
			UpdLibs();
			execargs.Text = project.execargs;
			prjname.Text = project.name;
			prjtype.SelectedIndex = (int)project.type;
			tarfile.Text = project.targetfilename;
			prjdir.Text = project.basedir;

			checkBox1.Checked = project.isRelease;
			cpargs.Text = project.compileargs;
			lnkargs.Text = project.linkargs;

			if(libs.Items.Count > 0)
				libs.SelectedIndex = 0;
		}

		private void libs_SelectedIndexChanged(object sender, EventArgs e)
		{
			tlib.Text = (string)libs.SelectedItem;
		}

		void UpdLibs()
		{
			int ti = libs.SelectedIndex;
			libs.Items.Clear();
			foreach(string l in project.libs)
				libs.Items.Add(l);
			if(ti > 0 && libs.Items.Count > ti)
				libs.SelectedIndex = ti;

			Files.Items.Clear();
			//foreach(DModule dmod in project.files)	Files.Items.Add((dmod.mod_file.StartsWith(project.basedir)) ? dmod.mod_file.Substring(project.basedir.Length + 1) : dmod.mod_file);
			foreach(string fn in project.resourceFiles)
				Files.Items.Add((fn.StartsWith(project.basedir)) ? fn.Substring(project.basedir.Length + 1) : fn);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			string tl = tlib.Text;
			if(tl == "") return;

			if(!project.libs.Contains(tl))
			{
				project.libs.Add(tl);
			}
			UpdLibs();
		}

		private void button6_Click(object sender, EventArgs e)
		{
			string tl = (string)libs.SelectedItem;
			if(tl == "") return;

			if(project.libs.Contains(tl))
			{
				project.libs.Remove(tl);
				project.libs.Add(tlib.Text);
			}
			UpdLibs();
		}

		private void execargs_TextChanged(object sender, EventArgs e)
		{
			project.execargs = execargs.Text;
		}

		private void prjname_TextChanged(object sender, EventArgs e)
		{
			project.name = prjname.Text;
		}

		private void prjtype_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch(prjtype.SelectedIndex)
			{
				case 0: project.type = DProject.PrjType.WindowsApp; break;
				case 1: project.type = DProject.PrjType.ConsoleApp; break;
				case 2: project.type = DProject.PrjType.Dll; break;
				case 3: project.type = DProject.PrjType.StaticLib; break;
			}
		}

		private void tarfile_TextChanged(object sender, EventArgs e)
		{
			project.targetfilename = tarfile.Text;
		}

		private void button3_Click(object sender, EventArgs e)
		{
			fD.SelectedPath = project.basedir;
			if(fD.ShowDialog() == DialogResult.OK)
			{
				prjdir.Text = fD.SelectedPath;
			}
		}

		private void prjdir_TextChanged(object sender, EventArgs e)
		{
			project.basedir = prjdir.Text;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if(!Directory.Exists(project.basedir))
			{
				MessageBox.Show(project.basedir + " does not exist");
				DialogResult = DialogResult.None;
				return;
			}

			if(project.name == "")
			{
				MessageBox.Show("Project name cannot be empty");
				DialogResult = DialogResult.None;
				return;
			}

			if(project.targetfilename == "")
			{
				MessageBox.Show("Target name cannot be empty");
				DialogResult = DialogResult.None;
				return;
			}

			project.Save();
			Close();
		}

		private void button5_Click(object sender, EventArgs e)
		{
			string tl = (string)libs.SelectedItem;
			if(tl == "") return;

			project.libs.Remove(tl);
			UpdLibs();
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			project.isRelease = checkBox1.Checked;
		}

		private void cpargs_TextChanged(object sender, EventArgs e)
		{
			project.compileargs = cpargs.Text;
		}

		private void lnkargs_TextChanged(object sender, EventArgs e)
		{
			project.linkargs = lnkargs.Text;
		}

		private void PrjProp_FormClosing(object sender, FormClosingEventArgs e)
		{
			Form1.thisForm.UpdateFiles();
		}

		private void button2_Click_1(object sender, EventArgs e)
		{
			foreach(ListViewItem lvi in Files.SelectedItems)
			{
				string fn = lvi.Text;
				if(!Path.IsPathRooted(fn))
				{
					fn = project.basedir + "\\" + fn;
				}

				project.files.Remove(project.FileDataByFile(fn));
				project.resourceFiles.Remove(fn);
				UpdLibs();
				Form1.thisForm.UpdateFiles();
			}
		}

		private void button7_Click(object sender, EventArgs e)
		{
			if(Form1.thisForm.oF.ShowDialog() == DialogResult.OK)
			{
				foreach(string file in Form1.thisForm.oF.FileNames)
				{
					if(Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

					project.AddSrc(file);
				}
				UpdLibs();
				Form1.thisForm.UpdateFiles();
			}
		}

		private void InitializeComponent()
		{
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label1;
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.button2 = new System.Windows.Forms.Button();
			this.button7 = new System.Windows.Forms.Button();
			this.Files = new System.Windows.Forms.ListView();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.lnkargs = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.cpargs = new System.Windows.Forms.TextBox();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.execargs = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.button6 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.tlib = new System.Windows.Forms.TextBox();
			this.libs = new System.Windows.Forms.ListBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.button3 = new System.Windows.Forms.Button();
			this.prjdir = new System.Windows.Forms.TextBox();
			this.prjtype = new System.Windows.Forms.ComboBox();
			this.tarfile = new System.Windows.Forms.TextBox();
			this.prjname = new System.Windows.Forms.TextBox();
			this.fD = new System.Windows.Forms.FolderBrowserDialog();
			this.button1 = new System.Windows.Forms.Button();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			this.groupBox5.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(6, 101);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(85, 13);
			label3.TabIndex = 12;
			label3.Text = "Project Directory";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(6, 75);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(83, 13);
			label4.TabIndex = 9;
			label4.Text = "Target Filename";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point(6, 48);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(67, 13);
			label2.TabIndex = 3;
			label2.Text = "Project Type";
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(6, 22);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(35, 13);
			label1.TabIndex = 1;
			label1.Text = "Name";
			// 
			// groupBox5
			// 
			this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox5.Controls.Add(this.button2);
			this.groupBox5.Controls.Add(this.button7);
			this.groupBox5.Controls.Add(this.Files);
			this.groupBox5.Location = new System.Drawing.Point(610, 12);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(310, 345);
			this.groupBox5.TabIndex = 12;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Files";
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(157, 19);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(147, 23);
			this.button2.TabIndex = 5;
			this.button2.Text = "Rem from Prj";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click_1);
			// 
			// button7
			// 
			this.button7.Location = new System.Drawing.Point(6, 19);
			this.button7.Name = "button7";
			this.button7.Size = new System.Drawing.Size(145, 23);
			this.button7.TabIndex = 4;
			this.button7.Text = "Add";
			this.button7.UseVisualStyleBackColor = true;
			this.button7.Click += new System.EventHandler(this.button7_Click);
			// 
			// Files
			// 
			this.Files.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.Files.FullRowSelect = true;
			this.Files.HideSelection = false;
			this.Files.Location = new System.Drawing.Point(6, 48);
			this.Files.Name = "Files";
			this.Files.Size = new System.Drawing.Size(298, 291);
			this.Files.TabIndex = 0;
			this.Files.UseCompatibleStateImageBehavior = false;
			this.Files.View = System.Windows.Forms.View.List;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.lnkargs);
			this.groupBox4.Controls.Add(this.label6);
			this.groupBox4.Controls.Add(this.label5);
			this.groupBox4.Controls.Add(this.cpargs);
			this.groupBox4.Controls.Add(this.checkBox1);
			this.groupBox4.Location = new System.Drawing.Point(12, 148);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(425, 126);
			this.groupBox4.TabIndex = 11;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Build arguments";
			// 
			// lnkargs
			// 
			this.lnkargs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lnkargs.Location = new System.Drawing.Point(6, 94);
			this.lnkargs.Name = "lnkargs";
			this.lnkargs.Size = new System.Drawing.Size(413, 20);
			this.lnkargs.TabIndex = 9;
			this.lnkargs.TextChanged += new System.EventHandler(this.lnkargs_TextChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(6, 78);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(116, 13);
			this.label6.TabIndex = 8;
			this.label6.Text = "Extra linking arguments";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 39);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(122, 13);
			this.label5.TabIndex = 7;
			this.label5.Text = "Extra compile arguments";
			// 
			// cpargs
			// 
			this.cpargs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cpargs.Location = new System.Drawing.Point(6, 55);
			this.cpargs.Name = "cpargs";
			this.cpargs.Size = new System.Drawing.Size(413, 20);
			this.cpargs.TabIndex = 6;
			this.cpargs.TextChanged += new System.EventHandler(this.cpargs_TextChanged);
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(6, 19);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(102, 17);
			this.checkBox1.TabIndex = 5;
			this.checkBox1.Text = "Is Release Build";
			this.checkBox1.UseVisualStyleBackColor = true;
			this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.execargs);
			this.groupBox3.Location = new System.Drawing.Point(12, 280);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(425, 48);
			this.groupBox3.TabIndex = 10;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Execution arguments";
			// 
			// execargs
			// 
			this.execargs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.execargs.Location = new System.Drawing.Point(6, 22);
			this.execargs.Name = "execargs";
			this.execargs.Size = new System.Drawing.Size(413, 20);
			this.execargs.TabIndex = 0;
			this.execargs.TextChanged += new System.EventHandler(this.execargs_TextChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox2.Controls.Add(this.button6);
			this.groupBox2.Controls.Add(this.button5);
			this.groupBox2.Controls.Add(this.button4);
			this.groupBox2.Controls.Add(this.tlib);
			this.groupBox2.Controls.Add(this.libs);
			this.groupBox2.Location = new System.Drawing.Point(443, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(161, 345);
			this.groupBox2.TabIndex = 9;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Link Libraries";
			// 
			// button6
			// 
			this.button6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button6.Location = new System.Drawing.Point(59, 45);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(56, 23);
			this.button6.TabIndex = 4;
			this.button6.Text = "Apply";
			this.button6.UseVisualStyleBackColor = true;
			this.button6.Click += new System.EventHandler(this.button6_Click);
			// 
			// button5
			// 
			this.button5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button5.Location = new System.Drawing.Point(121, 45);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(34, 23);
			this.button5.TabIndex = 3;
			this.button5.Text = "Del";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.button5_Click);
			// 
			// button4
			// 
			this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.button4.Location = new System.Drawing.Point(9, 45);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(44, 23);
			this.button4.TabIndex = 2;
			this.button4.Text = "Add";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// tlib
			// 
			this.tlib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tlib.Location = new System.Drawing.Point(9, 19);
			this.tlib.Name = "tlib";
			this.tlib.Size = new System.Drawing.Size(146, 20);
			this.tlib.TabIndex = 1;
			// 
			// libs
			// 
			this.libs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.libs.FormattingEnabled = true;
			this.libs.Location = new System.Drawing.Point(9, 73);
			this.libs.Name = "libs";
			this.libs.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.libs.Size = new System.Drawing.Size(146, 264);
			this.libs.TabIndex = 0;
			this.libs.SelectedIndexChanged += new System.EventHandler(this.libs_SelectedIndexChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.button3);
			this.groupBox1.Controls.Add(label3);
			this.groupBox1.Controls.Add(this.prjdir);
			this.groupBox1.Controls.Add(this.prjtype);
			this.groupBox1.Controls.Add(label4);
			this.groupBox1.Controls.Add(this.tarfile);
			this.groupBox1.Controls.Add(label2);
			this.groupBox1.Controls.Add(label1);
			this.groupBox1.Controls.Add(this.prjname);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(425, 130);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Generic";
			// 
			// button3
			// 
			this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button3.Location = new System.Drawing.Point(393, 96);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(26, 23);
			this.button3.TabIndex = 13;
			this.button3.Text = "...";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// prjdir
			// 
			this.prjdir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.prjdir.Location = new System.Drawing.Point(132, 98);
			this.prjdir.Name = "prjdir";
			this.prjdir.ReadOnly = true;
			this.prjdir.Size = new System.Drawing.Size(255, 20);
			this.prjdir.TabIndex = 11;
			// 
			// prjtype
			// 
			this.prjtype.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.prjtype.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.prjtype.FormattingEnabled = true;
			this.prjtype.Items.AddRange(new object[] {
            "Windows App",
            "Console App",
            "Dynamic Link Library",
            "Static Library"});
			this.prjtype.Location = new System.Drawing.Point(132, 45);
			this.prjtype.Name = "prjtype";
			this.prjtype.Size = new System.Drawing.Size(287, 21);
			this.prjtype.TabIndex = 10;
			this.prjtype.SelectedIndexChanged += new System.EventHandler(this.prjtype_SelectedIndexChanged);
			// 
			// tarfile
			// 
			this.tarfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tarfile.Location = new System.Drawing.Point(132, 72);
			this.tarfile.Name = "tarfile";
			this.tarfile.Size = new System.Drawing.Size(287, 20);
			this.tarfile.TabIndex = 7;
			this.tarfile.TextChanged += new System.EventHandler(this.tarfile_TextChanged);
			// 
			// prjname
			// 
			this.prjname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.prjname.Location = new System.Drawing.Point(132, 19);
			this.prjname.Name = "prjname";
			this.prjname.Size = new System.Drawing.Size(287, 20);
			this.prjname.TabIndex = 0;
			this.prjname.TextChanged += new System.EventHandler(this.prjname_TextChanged);
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(845, 363);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 13;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// ProjectPropertyPage
			// 
			this.AcceptButton = this.button1;
			this.ClientSize = new System.Drawing.Size(932, 398);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "ProjectPropertyPage";
			this.Resize += new System.EventHandler(this.ProjectPropertyPage_Resize);
			this.groupBox5.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		private void ProjectPropertyPage_Resize(object sender, EventArgs e)
		{
			int tx=Files.Width;
			button7.Width = tx / 2;
			button2.Left = button7.Left+ tx/2;
			button2.Width = tx / 2;
		}
	}
}
