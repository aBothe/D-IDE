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
		private Button button3;
		private TextBox prjdir;
		private ComboBox prjtype;
		private TextBox tarfile;
		private TextBox prjname;
		private FolderBrowserDialog fD;
		private Button button1;
		private TabControl tabControl1;
		private TabPage tabPage1;
		private TabPage tabPage2;
		private GroupBox groupBox4;
		private TextBox lnkargs;
		private Label label6;
		private Label label5;
		private TextBox cpargs;
		private CheckBox IsReleaseBuild;
		private GroupBox groupBox3;
		private TextBox execargs;
		private TabPage tabPage3;
		private GroupBox groupBox2;
		private Button button6;
		private Button button5;
		private Button button4;
		private TextBox tlib;
		private ListBox libs;
		private TabPage tabPage4;
		private Button button2;
		private Button button7;
		private ListView Files;
		private GroupBox groupBox1;
		private Button button9;
		private Button button8;
		private Button button10;
		private Button button11;
		private Button button12;
		public DProject lastProject;
		private Button button13;
		private TextBox OutputDir_dbg;
		private ListBox FileDeps;
		private CheckBox SubversioningEnabled;
		private CheckBox StoreLastSources;
		private Label label8;
		private TextBox LastVersionCount;
		private GroupBox groupBox5;
		private ListBox ProjectDeps;
		private Button button14;
		private Button button15;
		private ToolTip toolTip1;
		private System.ComponentModel.IContainer components;
		private ComboBox ManifestCreation;
		private GroupBox Subversioning;
		private Label label9;
		private Button button16;
		private TextBox OutputDir;
        private ComboBox DVersionSelector;
		public DProject project;

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

		public new void Load(DProject prj)
		{
			if (prj == null) return;
			project = prj;

			execargs.Text = prj.execargs;
			prjname.Text = prj.name;
			prjtype.SelectedIndex = (int)prj.type;
			tarfile.Text = prj.targetfilename;
			prjdir.Text = prj.basedir;
			OutputDir_dbg.Text = prj.OutputDirectory_dbg;
			OutputDir.Text = prj.OutputDirectory;

			SelectedDVersion = prj.CompilerVersion;
			IsReleaseBuild.Checked = prj.isRelease;
			cpargs.Text = prj.compileargs;
			lnkargs.Text = prj.linkargs;

			SubversioningEnabled.Checked = prj.EnableSubversioning;
			StoreLastSources.Checked = prj.AlsoStoreSources;
			if (prj.LastVersionCount > 0)
			{
				LastVersionCount.Text = prj.LastVersionCount.ToString();
			}

			ManifestCreation.SelectedIndex = (int)prj.ManifestCreation;

			if (libs.Items.Count > 0)
				libs.SelectedIndex = 0;

			Files.Items.Clear();
			foreach (string fn in prj.resourceFiles)
			{
				ListViewItem lvi = Files.Items.Add((fn.StartsWith(prj.basedir)) ? fn.Substring(prj.basedir.Length + 1) : fn);
				lvi.Tag = prj.GetPhysFilePath(fn);
			}

			FileDeps.Items.Clear();
			foreach (string fn in prj.FileDependencies)
			{
				if (!String.IsNullOrEmpty(fn))
				{
					FileDeps.Items.Add(fn);
				}
			}

			ProjectDeps.Items.Clear();
			foreach (string fn in prj.ProjectDependencies)
			{
				if (!String.IsNullOrEmpty(fn))
				{
					ProjectDeps.Items.Add(fn);
				}
			}

			libs.Items.Clear();
			foreach (string fn in prj.libs)
			{
				if (!String.IsNullOrEmpty(fn))
					libs.Items.Add(fn);
			}
		}

		public DProject Save()
		{
			lastProject = project;

			DProject prj = new DProject();
			prj.prjfn = project.prjfn;
			prj.execargs = execargs.Text;
			prj.name = prjname.Text;
			prj.type = (DProject.PrjType)prjtype.SelectedIndex;
			prj.targetfilename = tarfile.Text;
			prj.basedir = prjdir.Text;
			prj.OutputDirectory = OutputDir.Text;
			prj.OutputDirectory_dbg = OutputDir_dbg.Text;
			prj.CompilerVersion = SelectedDVersion;
			prj.isRelease = IsReleaseBuild.Checked;
			prj.compileargs = cpargs.Text;
			prj.linkargs = lnkargs.Text;

			prj.EnableSubversioning = SubversioningEnabled.Checked;
			prj.AlsoStoreSources = StoreLastSources.Checked;
			try
			{
				if (LastVersionCount.Text.Length < 1) prj.LastVersionCount = -1;
				{
					int c = Convert.ToInt32(LastVersionCount.Text);
					if (c > 0)
						prj.LastVersionCount = c;
					else
						prj.LastVersionCount = -1;
				}
			}
			catch { }

			prj.ManifestCreation = (DProject.ManifestCreationType)ManifestCreation.SelectedIndex;

			foreach (string lvi in FileDeps.Items)
			{
				if (!prj.FileDependencies.Contains(lvi))
					prj.FileDependencies.Add(lvi);
			}

			foreach (string lvi in ProjectDeps.Items)
			{
				if (!prj.ProjectDependencies.Contains(lvi))
					prj.ProjectDependencies.Add(lvi);
			}

			foreach (string lib in libs.Items)
				if (!String.IsNullOrEmpty(lib)) prj.libs.Add(lib);

			foreach (ListViewItem lvi in Files.Items)
			{
				string fn = lvi.Text;
				prj.resourceFiles.Add(prj.GetRelFilePath(fn));
				if (DModule.Parsable(fn) && File.Exists(prj.GetPhysFilePath(fn)))
					prj.files.Add(new DModule(prj,prj.GetPhysFilePath(fn)));
			}

			project = prj;
			prj.Save();

			D_IDE_Properties.Projects[prj.prjfn] = project;
			D_IDEForm.thisForm.UpdateFiles();

			return prj;
		}

		public ProjectPropertyPage(DProject project)
		{
			if (project == null) return;
			this.project = project;
			this.DockAreas = DockAreas.Document;
			this.TabText = project.name;

			InitializeComponent();

			Load(project);
		}

		private void libs_SelectedIndexChanged(object sender, EventArgs e)
		{
			tlib.Text = (string)libs.SelectedItem;
		}

		private void button4_Click(object sender, EventArgs e)
		{
			string tl = tlib.Text.ToLower();
			if (tl == "") return;

			foreach (string t in libs.Items)
			{
				if (t.ToLower() == tl) return;
			}

			libs.Items.Add(tl);
		}

		private void button5_Click(object sender, EventArgs e)
		{
			libs.Items.RemoveAt(libs.SelectedIndex);
		}

		private void button6_Click(object sender, EventArgs e)
		{
			button5_Click(sender, e);
			button4_Click(sender, e);
		}

		private void button3_Click(object sender, EventArgs e)
		{
			fD.SelectedPath = prjdir.Text;
			if (fD.ShowDialog() == DialogResult.OK)
			{
				prjdir.Text = fD.SelectedPath;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			button10_Click(sender, e);
			button12_Click(sender, e);
		}

		private void button2_Click_1(object sender, EventArgs e)
		{
			foreach (ListViewItem lvi in Files.SelectedItems)
			{
				Files.Items.Remove(lvi);
			}
		}

		private void button7_Click(object sender, EventArgs e)
		{
			D_IDEForm.thisForm.oF.InitialDirectory = prjdir.Text;
			if (D_IDEForm.thisForm.oF.ShowDialog() == DialogResult.OK)
			{
				foreach (string file in D_IDEForm.thisForm.oF.FileNames)
				{
					if (Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

					Files.Items.Add(file);
				}
			}
		}

		private void button10_Click(object sender, EventArgs e)
		{
			if (!Directory.Exists(prjdir.Text))
			{
				MessageBox.Show(prjdir.Text + " does not exist");
				DialogResult = DialogResult.None;
				return;
			}

			if (prjname.Text == "")
			{
				MessageBox.Show("Project name cannot be empty");
				DialogResult = DialogResult.None;
				return;
			}

			if (tarfile.Text == "")
			{
				MessageBox.Show("Target name cannot be empty");
				DialogResult = DialogResult.None;
				return;
			}

			Save();
			if (project.prjfn == D_IDEForm.thisForm.prj.prjfn)
			{
				D_IDEForm.thisForm.prj = project;
				//Form1.thisForm.UpdateFiles();
			}
		}

		private void button11_Click(object sender, EventArgs e)
		{
			Load(lastProject == null ? project : lastProject);
		}
		#region Form code
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.Label label14;
            this.button3 = new System.Windows.Forms.Button();
            this.prjdir = new System.Windows.Forms.TextBox();
            this.prjtype = new System.Windows.Forms.ComboBox();
            this.tarfile = new System.Windows.Forms.TextBox();
            this.prjname = new System.Windows.Forms.TextBox();
            this.fD = new System.Windows.Forms.FolderBrowserDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label9 = new System.Windows.Forms.Label();
            this.button16 = new System.Windows.Forms.Button();
            this.OutputDir = new System.Windows.Forms.TextBox();
            this.button13 = new System.Windows.Forms.Button();
            this.OutputDir_dbg = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.Subversioning = new System.Windows.Forms.GroupBox();
            this.SubversioningEnabled = new System.Windows.Forms.CheckBox();
            this.StoreLastSources = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.LastVersionCount = new System.Windows.Forms.TextBox();
            this.ManifestCreation = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.DVersionSelector = new System.Windows.Forms.ComboBox();
            this.lnkargs = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cpargs = new System.Windows.Forms.TextBox();
            this.IsReleaseBuild = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.execargs = new System.Windows.Forms.TextBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.ProjectDeps = new System.Windows.Forms.ListBox();
            this.button14 = new System.Windows.Forms.Button();
            this.button15 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.FileDeps = new System.Windows.Forms.ListBox();
            this.button9 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button6 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.tlib = new System.Windows.Forms.TextBox();
            this.libs = new System.Windows.Forms.ListBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.button2 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.Files = new System.Windows.Forms.ListView();
            this.button10 = new System.Windows.Forms.Button();
            this.button11 = new System.Windows.Forms.Button();
            this.button12 = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.Subversioning.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(6, 94);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(85, 13);
            label3.TabIndex = 12;
            label3.Text = "Project Directory";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(6, 68);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(83, 13);
            label4.TabIndex = 9;
            label4.Text = "Target Filename";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 41);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(67, 13);
            label2.TabIndex = 3;
            label2.Text = "Project Type";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 15);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(35, 13);
            label1.TabIndex = 1;
            label1.Text = "Name";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(6, 120);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(93, 13);
            label7.TabIndex = 15;
            label7.Text = "Debug Output Dir.";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(6, 22);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(53, 13);
            label14.TabIndex = 41;
            label14.Text = "D Version";
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button3.Location = new System.Drawing.Point(635, 89);
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
            this.prjdir.Location = new System.Drawing.Point(132, 91);
            this.prjdir.Name = "prjdir";
            this.prjdir.ReadOnly = true;
            this.prjdir.Size = new System.Drawing.Size(497, 20);
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
            this.prjtype.Location = new System.Drawing.Point(132, 38);
            this.prjtype.Name = "prjtype";
            this.prjtype.Size = new System.Drawing.Size(529, 21);
            this.prjtype.TabIndex = 10;
            // 
            // tarfile
            // 
            this.tarfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tarfile.Location = new System.Drawing.Point(132, 65);
            this.tarfile.Name = "tarfile";
            this.tarfile.Size = new System.Drawing.Size(529, 20);
            this.tarfile.TabIndex = 7;
            // 
            // prjname
            // 
            this.prjname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.prjname.Location = new System.Drawing.Point(132, 12);
            this.prjname.Name = "prjname";
            this.prjname.Size = new System.Drawing.Size(529, 20);
            this.prjname.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(577, 495);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(95, 23);
            this.button1.TabIndex = 13;
            this.button1.Text = "Apply && Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(677, 489);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.button16);
            this.tabPage1.Controls.Add(this.OutputDir);
            this.tabPage1.Controls.Add(this.button13);
            this.tabPage1.Controls.Add(label7);
            this.tabPage1.Controls.Add(this.OutputDir_dbg);
            this.tabPage1.Controls.Add(this.button3);
            this.tabPage1.Controls.Add(label1);
            this.tabPage1.Controls.Add(label3);
            this.tabPage1.Controls.Add(this.prjname);
            this.tabPage1.Controls.Add(this.prjdir);
            this.tabPage1.Controls.Add(label2);
            this.tabPage1.Controls.Add(this.prjtype);
            this.tabPage1.Controls.Add(this.tarfile);
            this.tabPage1.Controls.Add(label4);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(669, 463);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 146);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(100, 13);
            this.label9.TabIndex = 19;
            this.label9.Text = "Release Output Dir.";
            // 
            // button16
            // 
            this.button16.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button16.Location = new System.Drawing.Point(635, 141);
            this.button16.Name = "button16";
            this.button16.Size = new System.Drawing.Size(26, 23);
            this.button16.TabIndex = 18;
            this.button16.Text = "...";
            this.button16.UseVisualStyleBackColor = true;
            this.button16.Click += new System.EventHandler(this.button16_Click);
            // 
            // OutputDir
            // 
            this.OutputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.OutputDir.Location = new System.Drawing.Point(132, 143);
            this.OutputDir.Name = "OutputDir";
            this.OutputDir.Size = new System.Drawing.Size(497, 20);
            this.OutputDir.TabIndex = 17;
            // 
            // button13
            // 
            this.button13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button13.Location = new System.Drawing.Point(635, 115);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(26, 23);
            this.button13.TabIndex = 16;
            this.button13.Text = "...";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.button13_Click);
            // 
            // OutputDir_dbg
            // 
            this.OutputDir_dbg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.OutputDir_dbg.Location = new System.Drawing.Point(132, 117);
            this.OutputDir_dbg.Name = "OutputDir_dbg";
            this.OutputDir_dbg.Size = new System.Drawing.Size(497, 20);
            this.OutputDir_dbg.TabIndex = 14;
            // 
            // tabPage2
            // 
            this.tabPage2.AutoScroll = true;
            this.tabPage2.Controls.Add(this.Subversioning);
            this.tabPage2.Controls.Add(this.ManifestCreation);
            this.tabPage2.Controls.Add(this.groupBox4);
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(669, 463);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Build options";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // Subversioning
            // 
            this.Subversioning.Controls.Add(this.SubversioningEnabled);
            this.Subversioning.Controls.Add(this.StoreLastSources);
            this.Subversioning.Controls.Add(this.label8);
            this.Subversioning.Controls.Add(this.LastVersionCount);
            this.Subversioning.Location = new System.Drawing.Point(6, 216);
            this.Subversioning.Name = "Subversioning";
            this.Subversioning.Size = new System.Drawing.Size(425, 100);
            this.Subversioning.TabIndex = 17;
            this.Subversioning.TabStop = false;
            this.Subversioning.Text = "Last version storage";
            // 
            // SubversioningEnabled
            // 
            this.SubversioningEnabled.AutoSize = true;
            this.SubversioningEnabled.Location = new System.Drawing.Point(9, 19);
            this.SubversioningEnabled.Name = "SubversioningEnabled";
            this.SubversioningEnabled.Size = new System.Drawing.Size(229, 17);
            this.SubversioningEnabled.TabIndex = 10;
            this.SubversioningEnabled.Text = "Store each new build in a new subdirectory";
            this.SubversioningEnabled.UseVisualStyleBackColor = true;
            // 
            // StoreLastSources
            // 
            this.StoreLastSources.AutoSize = true;
            this.StoreLastSources.Location = new System.Drawing.Point(9, 42);
            this.StoreLastSources.Name = "StoreLastSources";
            this.StoreLastSources.Size = new System.Drawing.Size(250, 17);
            this.StoreLastSources.TabIndex = 13;
            this.StoreLastSources.Text = "Also store the changed sources in that directory";
            this.StoreLastSources.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 68);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(200, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Limit last versions to: (Leave empty if not)";
            // 
            // LastVersionCount
            // 
            this.LastVersionCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LastVersionCount.Location = new System.Drawing.Point(262, 65);
            this.LastVersionCount.MaxLength = 10;
            this.LastVersionCount.Name = "LastVersionCount";
            this.LastVersionCount.Size = new System.Drawing.Size(157, 20);
            this.LastVersionCount.TabIndex = 14;
            this.LastVersionCount.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
            // 
            // ManifestCreation
            // 
            this.ManifestCreation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ManifestCreation.FormattingEnabled = true;
            this.ManifestCreation.Items.AddRange(new object[] {
            "Don\'t create manifest file",
            "Create a manifest with a resource file",
            "Create and copy the manifest into the output dir."});
            this.ManifestCreation.Location = new System.Drawing.Point(6, 322);
            this.ManifestCreation.Name = "ManifestCreation";
            this.ManifestCreation.Size = new System.Drawing.Size(425, 21);
            this.ManifestCreation.TabIndex = 16;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(label14);
            this.groupBox4.Controls.Add(this.DVersionSelector);
            this.groupBox4.Controls.Add(this.lnkargs);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.cpargs);
            this.groupBox4.Controls.Add(this.IsReleaseBuild);
            this.groupBox4.Location = new System.Drawing.Point(6, 60);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(425, 150);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Build arguments";
            // 
            // DVersionSelector
            // 
            this.DVersionSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DVersionSelector.FormattingEnabled = true;
            this.DVersionSelector.Items.AddRange(new object[] {
            "Version 1",
            "Version 2"});
            this.DVersionSelector.Location = new System.Drawing.Point(262, 19);
            this.DVersionSelector.Name = "DVersionSelector";
            this.DVersionSelector.Size = new System.Drawing.Size(157, 21);
            this.DVersionSelector.TabIndex = 40;
            // 
            // lnkargs
            // 
            this.lnkargs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkargs.Location = new System.Drawing.Point(6, 121);
            this.lnkargs.Name = "lnkargs";
            this.lnkargs.Size = new System.Drawing.Size(413, 20);
            this.lnkargs.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 105);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(116, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Extra linking arguments";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 66);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(122, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Extra compile arguments";
            // 
            // cpargs
            // 
            this.cpargs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cpargs.Location = new System.Drawing.Point(6, 82);
            this.cpargs.Name = "cpargs";
            this.cpargs.Size = new System.Drawing.Size(413, 20);
            this.cpargs.TabIndex = 6;
            // 
            // IsReleaseBuild
            // 
            this.IsReleaseBuild.AutoSize = true;
            this.IsReleaseBuild.Location = new System.Drawing.Point(6, 46);
            this.IsReleaseBuild.Name = "IsReleaseBuild";
            this.IsReleaseBuild.Size = new System.Drawing.Size(102, 17);
            this.IsReleaseBuild.TabIndex = 5;
            this.IsReleaseBuild.Text = "Is Release Build";
            this.IsReleaseBuild.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.execargs);
            this.groupBox3.Location = new System.Drawing.Point(6, 6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(425, 48);
            this.groupBox3.TabIndex = 11;
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
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox5);
            this.tabPage3.Controls.Add(this.groupBox1);
            this.tabPage3.Controls.Add(this.groupBox2);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(669, 463);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Dependencies";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.ProjectDeps);
            this.groupBox5.Controls.Add(this.button14);
            this.groupBox5.Controls.Add(this.button15);
            this.groupBox5.Location = new System.Drawing.Point(432, 6);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(231, 451);
            this.groupBox5.TabIndex = 12;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Projects to build before building";
            // 
            // ProjectDeps
            // 
            this.ProjectDeps.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ProjectDeps.FormattingEnabled = true;
            this.ProjectDeps.Location = new System.Drawing.Point(6, 48);
            this.ProjectDeps.Name = "ProjectDeps";
            this.ProjectDeps.Size = new System.Drawing.Size(219, 394);
            this.ProjectDeps.TabIndex = 3;
            this.toolTip1.SetToolTip(this.ProjectDeps, "These projects get builded and their targets get copied into the output directory" +
                    "");
            // 
            // button14
            // 
            this.button14.Location = new System.Drawing.Point(92, 19);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(80, 23);
            this.button14.TabIndex = 2;
            this.button14.Text = "Exclude";
            this.button14.UseVisualStyleBackColor = true;
            this.button14.Click += new System.EventHandler(this.button14_Click);
            // 
            // button15
            // 
            this.button15.Location = new System.Drawing.Point(6, 19);
            this.button15.Name = "button15";
            this.button15.Size = new System.Drawing.Size(80, 23);
            this.button15.TabIndex = 1;
            this.button15.Text = "Add";
            this.button15.UseVisualStyleBackColor = true;
            this.button15.Click += new System.EventHandler(this.button15_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox1.Controls.Add(this.FileDeps);
            this.groupBox1.Controls.Add(this.button9);
            this.groupBox1.Controls.Add(this.button8);
            this.groupBox1.Location = new System.Drawing.Point(173, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(253, 451);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Files to copy into the output directory";
            // 
            // FileDeps
            // 
            this.FileDeps.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FileDeps.FormattingEnabled = true;
            this.FileDeps.Location = new System.Drawing.Point(6, 48);
            this.FileDeps.Name = "FileDeps";
            this.FileDeps.Size = new System.Drawing.Size(241, 394);
            this.FileDeps.TabIndex = 3;
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(92, 19);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(80, 23);
            this.button9.TabIndex = 2;
            this.button9.Text = "Exclude";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(6, 19);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(80, 23);
            this.button8.TabIndex = 1;
            this.button8.Text = "Add";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
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
            this.groupBox2.Location = new System.Drawing.Point(6, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(161, 451);
            this.groupBox2.TabIndex = 10;
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
            this.libs.IntegralHeight = false;
            this.libs.Location = new System.Drawing.Point(9, 73);
            this.libs.Name = "libs";
            this.libs.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.libs.Size = new System.Drawing.Size(146, 368);
            this.libs.TabIndex = 0;
            this.libs.SelectedIndexChanged += new System.EventHandler(this.libs_SelectedIndexChanged);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.button2);
            this.tabPage4.Controls.Add(this.button7);
            this.tabPage4.Controls.Add(this.Files);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(669, 463);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Files";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(94, 6);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(80, 23);
            this.button2.TabIndex = 16;
            this.button2.Text = "Exclude";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(8, 6);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(80, 23);
            this.button7.TabIndex = 15;
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
            this.Files.Location = new System.Drawing.Point(8, 35);
            this.Files.Name = "Files";
            this.Files.Size = new System.Drawing.Size(653, 422);
            this.Files.TabIndex = 14;
            this.Files.UseCompatibleStateImageBehavior = false;
            this.Files.View = System.Windows.Forms.View.List;
            // 
            // button10
            // 
            this.button10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button10.Location = new System.Drawing.Point(415, 495);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(75, 23);
            this.button10.TabIndex = 15;
            this.button10.Text = "Apply";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // button11
            // 
            this.button11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button11.Location = new System.Drawing.Point(308, 495);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(101, 23);
            this.button11.TabIndex = 16;
            this.button11.Text = "Restore previous";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // button12
            // 
            this.button12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button12.Location = new System.Drawing.Point(496, 495);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(75, 23);
            this.button12.TabIndex = 17;
            this.button12.Text = "Close";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // ProjectPropertyPage
            // 
            this.AcceptButton = this.button1;
            this.ClientSize = new System.Drawing.Size(676, 530);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button1);
            this.Name = "ProjectPropertyPage";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.Subversioning.ResumeLayout(false);
            this.Subversioning.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion
		private void button12_Click(object sender, EventArgs e)
		{
			Close();
		}

		#region Filedependencies
		private void button8_Click(object sender, EventArgs e)
		{
			D_IDEForm.thisForm.oF.InitialDirectory = prjdir.Text;
			if (D_IDEForm.thisForm.oF.ShowDialog() == DialogResult.OK)
			{
				foreach (string file in D_IDEForm.thisForm.oF.FileNames)
				{
					if (Path.GetExtension(file) == DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

					if (FileDeps.Items.Contains(file)) continue;

					FileDeps.Items.Add(GetRelativePath(file));
				}
				FileDeps.Refresh();
			}
		}

		private void button9_Click(object sender, EventArgs e)
		{
			List<string> si=new List<string>();
			foreach (string lvi in FileDeps.SelectedItems)
			{
				si.Add(lvi);
			}

			foreach (string lvi in si)
			{
				FileDeps.Items.Remove(lvi);
			}
		}
		#endregion
		#region Project deps
		private void button15_Click(object sender, EventArgs e)
		{
			D_IDEForm.thisForm.oF.InitialDirectory = prjdir.Text;
			string filterBefore = D_IDEForm.thisForm.oF.Filter;
			D_IDEForm.thisForm.oF.Filter = "D Projects (*"+DProject.prjext+")|*"+DProject.prjext;
			if (D_IDEForm.thisForm.oF.ShowDialog() == DialogResult.OK)
			{
				foreach (string file in D_IDEForm.thisForm.oF.FileNames)
				{
					if (Path.GetExtension(file) != DProject.prjext) { MessageBox.Show("Cannot add " + file + " !"); continue; }

					if (ProjectDeps.Items.Contains(file)) continue;

					ProjectDeps.Items.Add(file);
				}
				ProjectDeps.Refresh();
			}
			D_IDEForm.thisForm.oF.Filter = filterBefore;
		}

		private void button14_Click(object sender, EventArgs e)
		{
			List<string> si = new List<string>();
			foreach (string lvi in ProjectDeps.SelectedItems)
			{
				si.Add(lvi);
			}
			foreach (string lvi in si)
			{
				ProjectDeps.Items.Remove(lvi);
			}
		}
		#endregion

		public string GetRelativePath(string f)
		{
			if (!Path.IsPathRooted(f)) return f;
			string ret = f;

			if (ret.StartsWith(prjdir.Text))
				ret = ret.Substring(prjdir.Text.Length);

			ret = ret.Trim(new char[] { '\\' });
			return ret;
		}

		private void button13_Click(object sender, EventArgs e)
		{
			fD.SelectedPath = Path.IsPathRooted( OutputDir_dbg.Text)?OutputDir_dbg.Text:(prjdir.Text+"\\"+OutputDir_dbg.Text);
			if (fD.ShowDialog() == DialogResult.OK)
			{
				OutputDir_dbg.Text = GetRelativePath(fD.SelectedPath);
			}
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (!Char.IsDigit((char)e.KeyValue)) e.SuppressKeyPress = true;
		}

		private void button16_Click(object sender, EventArgs e)
		{
			fD.SelectedPath = Path.IsPathRooted(OutputDir.Text) ? OutputDir.Text : (prjdir.Text + "\\" + OutputDir.Text);
			if (fD.ShowDialog() == DialogResult.OK)
			{
				OutputDir.Text = GetRelativePath(fD.SelectedPath);
			}
		}

		

		
	}
}
