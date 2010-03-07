namespace D_IDE
{
	partial class IDESettings
	{
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IDESettings));
			this.button1 = new System.Windows.Forms.Button();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.DoAutoSave = new System.Windows.Forms.CheckBox();
			this.UseIntegDbg = new System.Windows.Forms.CheckBox();
			this.showCompleteLog = new System.Windows.Forms.CheckBox();
			this.label9 = new System.Windows.Forms.Label();
			this.logbuildprogress_chk = new System.Windows.Forms.CheckBox();
			this.dbg_exe = new System.Windows.Forms.TextBox();
			this.dbg_args = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.exe_win = new System.Windows.Forms.TextBox();
			this.exe_dll = new System.Windows.Forms.TextBox();
			this.exe_cmp = new System.Windows.Forms.TextBox();
			this.exe_console = new System.Windows.Forms.TextBox();
			this.exe_rc = new System.Windows.Forms.TextBox();
			this.exe_lib = new System.Windows.Forms.TextBox();
			this.link_win_exe = new System.Windows.Forms.TextBox();
			this.link_to_dll = new System.Windows.Forms.TextBox();
			this.cmp_to_obj = new System.Windows.Forms.TextBox();
			this.link_to_exe = new System.Windows.Forms.TextBox();
			this.rc = new System.Windows.Forms.TextBox();
			this.link_to_lib = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.parsedFileList = new System.Windows.Forms.ListView();
			this.button3 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.parsedFiles = new System.Windows.Forms.ListBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.restoreLastSession = new System.Windows.Forms.CheckBox();
			this.button6 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.reopenLastPrj = new System.Windows.Forms.CheckBox();
			this.updates = new System.Windows.Forms.CheckBox();
			this.label7 = new System.Windows.Forms.Label();
			this.button4 = new System.Windows.Forms.Button();
			this.defPrjDir = new System.Windows.Forms.TextBox();
			this.singleInst = new System.Windows.Forms.CheckBox();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.AutoSkipUnknownCode = new System.Windows.Forms.CheckBox();
			this.verbosedbgoutput = new System.Windows.Forms.CheckBox();
			this.tabPage5 = new System.Windows.Forms.TabPage();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label10 = new System.Windows.Forms.Label();
			this.HighLightingSearchXSHD = new System.Windows.Forms.Button();
			this.HighLightingAssocXSHDFile = new System.Windows.Forms.TextBox();
			this.HighLightingDelExt = new System.Windows.Forms.Button();
			this.HighlightingAddExt = new System.Windows.Forms.Button();
			this.HighLightingExt = new System.Windows.Forms.TextBox();
			this.HighLightingExts = new System.Windows.Forms.ListBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.tabPage1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage5.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(535, 621);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.DoAutoSave);
			this.tabPage1.Controls.Add(this.UseIntegDbg);
			this.tabPage1.Controls.Add(this.showCompleteLog);
			this.tabPage1.Controls.Add(this.label9);
			this.tabPage1.Controls.Add(this.logbuildprogress_chk);
			this.tabPage1.Controls.Add(this.dbg_exe);
			this.tabPage1.Controls.Add(this.dbg_args);
			this.tabPage1.Controls.Add(this.label8);
			this.tabPage1.Controls.Add(this.exe_win);
			this.tabPage1.Controls.Add(this.exe_dll);
			this.tabPage1.Controls.Add(this.exe_cmp);
			this.tabPage1.Controls.Add(this.exe_console);
			this.tabPage1.Controls.Add(this.exe_rc);
			this.tabPage1.Controls.Add(this.exe_lib);
			this.tabPage1.Controls.Add(this.link_win_exe);
			this.tabPage1.Controls.Add(this.link_to_dll);
			this.tabPage1.Controls.Add(this.cmp_to_obj);
			this.tabPage1.Controls.Add(this.link_to_exe);
			this.tabPage1.Controls.Add(this.rc);
			this.tabPage1.Controls.Add(this.link_to_lib);
			this.tabPage1.Controls.Add(this.label6);
			this.tabPage1.Controls.Add(this.label5);
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.label4);
			this.tabPage1.Controls.Add(this.label2);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(618, 589);
			this.tabPage1.TabIndex = 1;
			this.tabPage1.Text = "Build commands";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// DoAutoSave
			// 
			this.DoAutoSave.AutoSize = true;
			this.DoAutoSave.Location = new System.Drawing.Point(104, 189);
			this.DoAutoSave.Name = "DoAutoSave";
			this.DoAutoSave.Size = new System.Drawing.Size(182, 17);
			this.DoAutoSave.TabIndex = 24;
			this.DoAutoSave.Text = "Auto-Save all files before building";
			this.DoAutoSave.UseVisualStyleBackColor = true;
			// 
			// UseIntegDbg
			// 
			this.UseIntegDbg.AutoSize = true;
			this.UseIntegDbg.Location = new System.Drawing.Point(104, 212);
			this.UseIntegDbg.Name = "UseIntegDbg";
			this.UseIntegDbg.Size = new System.Drawing.Size(133, 17);
			this.UseIntegDbg.TabIndex = 23;
			this.UseIntegDbg.Text = "Use external debugger";
			this.UseIntegDbg.UseVisualStyleBackColor = true;
			// 
			// showCompleteLog
			// 
			this.showCompleteLog.AutoSize = true;
			this.showCompleteLog.Location = new System.Drawing.Point(104, 258);
			this.showCompleteLog.Name = "showCompleteLog";
			this.showCompleteLog.Size = new System.Drawing.Size(132, 17);
			this.showCompleteLog.TabIndex = 22;
			this.showCompleteLog.Text = "Show build commands";
			this.showCompleteLog.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(295, 190);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(175, 117);
			this.label9.TabIndex = 21;
			this.label9.Text = resources.GetString("label9.Text");
			// 
			// logbuildprogress_chk
			// 
			this.logbuildprogress_chk.AutoSize = true;
			this.logbuildprogress_chk.Location = new System.Drawing.Point(104, 235);
			this.logbuildprogress_chk.Name = "logbuildprogress_chk";
			this.logbuildprogress_chk.Size = new System.Drawing.Size(149, 17);
			this.logbuildprogress_chk.TabIndex = 20;
			this.logbuildprogress_chk.Text = "Show build log on building";
			this.logbuildprogress_chk.UseVisualStyleBackColor = true;
			// 
			// dbg_exe
			// 
			this.dbg_exe.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.dbg_exe.Location = new System.Drawing.Point(104, 163);
			this.dbg_exe.Name = "dbg_exe";
			this.dbg_exe.Size = new System.Drawing.Size(188, 20);
			this.dbg_exe.TabIndex = 19;
			// 
			// dbg_args
			// 
			this.dbg_args.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.dbg_args.Location = new System.Drawing.Point(298, 163);
			this.dbg_args.Name = "dbg_args";
			this.dbg_args.Size = new System.Drawing.Size(318, 20);
			this.dbg_args.TabIndex = 17;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(3, 166);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(77, 13);
			this.label8.TabIndex = 18;
			this.label8.Text = "Start debugger";
			// 
			// exe_win
			// 
			this.exe_win.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_win.Location = new System.Drawing.Point(104, 33);
			this.exe_win.Name = "exe_win";
			this.exe_win.Size = new System.Drawing.Size(188, 20);
			this.exe_win.TabIndex = 16;
			// 
			// exe_dll
			// 
			this.exe_dll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_dll.Location = new System.Drawing.Point(104, 85);
			this.exe_dll.Name = "exe_dll";
			this.exe_dll.Size = new System.Drawing.Size(188, 20);
			this.exe_dll.TabIndex = 13;
			// 
			// exe_cmp
			// 
			this.exe_cmp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_cmp.Location = new System.Drawing.Point(104, 7);
			this.exe_cmp.Name = "exe_cmp";
			this.exe_cmp.Size = new System.Drawing.Size(188, 20);
			this.exe_cmp.TabIndex = 11;
			// 
			// exe_console
			// 
			this.exe_console.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_console.Location = new System.Drawing.Point(104, 59);
			this.exe_console.Name = "exe_console";
			this.exe_console.Size = new System.Drawing.Size(188, 20);
			this.exe_console.TabIndex = 12;
			// 
			// exe_rc
			// 
			this.exe_rc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_rc.Location = new System.Drawing.Point(104, 137);
			this.exe_rc.Name = "exe_rc";
			this.exe_rc.Size = new System.Drawing.Size(188, 20);
			this.exe_rc.TabIndex = 15;
			// 
			// exe_lib
			// 
			this.exe_lib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.exe_lib.Location = new System.Drawing.Point(104, 111);
			this.exe_lib.Name = "exe_lib";
			this.exe_lib.Size = new System.Drawing.Size(372, 20);
			this.exe_lib.TabIndex = 14;
			// 
			// link_win_exe
			// 
			this.link_win_exe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.link_win_exe.Location = new System.Drawing.Point(298, 33);
			this.link_win_exe.Name = "link_win_exe";
			this.link_win_exe.Size = new System.Drawing.Size(318, 20);
			this.link_win_exe.TabIndex = 10;
			// 
			// link_to_dll
			// 
			this.link_to_dll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.link_to_dll.Location = new System.Drawing.Point(298, 85);
			this.link_to_dll.Name = "link_to_dll";
			this.link_to_dll.Size = new System.Drawing.Size(318, 20);
			this.link_to_dll.TabIndex = 3;
			// 
			// cmp_to_obj
			// 
			this.cmp_to_obj.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cmp_to_obj.Location = new System.Drawing.Point(298, 7);
			this.cmp_to_obj.Name = "cmp_to_obj";
			this.cmp_to_obj.Size = new System.Drawing.Size(318, 20);
			this.cmp_to_obj.TabIndex = 1;
			// 
			// link_to_exe
			// 
			this.link_to_exe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.link_to_exe.Location = new System.Drawing.Point(298, 59);
			this.link_to_exe.Name = "link_to_exe";
			this.link_to_exe.Size = new System.Drawing.Size(318, 20);
			this.link_to_exe.TabIndex = 2;
			// 
			// rc
			// 
			this.rc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.rc.Location = new System.Drawing.Point(298, 137);
			this.rc.Name = "rc";
			this.rc.Size = new System.Drawing.Size(318, 20);
			this.rc.TabIndex = 6;
			// 
			// link_to_lib
			// 
			this.link_to_lib.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.link_to_lib.Location = new System.Drawing.Point(298, 111);
			this.link_to_lib.Name = "link_to_lib";
			this.link_to_lib.Size = new System.Drawing.Size(318, 20);
			this.link_to_lib.TabIndex = 4;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(3, 36);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(98, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "Link windows *.exe";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 88);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(59, 13);
			this.label5.TabIndex = 8;
			this.label5.Text = "Link to *.dll";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(66, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Build to *.obj";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 140);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(63, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "Compile *.rc";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 62);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Link to *.exe";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 114);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(59, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Link to *.lib";
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.parsedFileList);
			this.tabPage3.Controls.Add(this.button3);
			this.tabPage3.Controls.Add(this.button2);
			this.tabPage3.Controls.Add(this.parsedFiles);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(618, 589);
			this.tabPage3.TabIndex = 0;
			this.tabPage3.Text = "Global file parsing";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// parsedFileList
			// 
			this.parsedFileList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.parsedFileList.BackColor = System.Drawing.Color.White;
			this.parsedFileList.FullRowSelect = true;
			this.parsedFileList.Location = new System.Drawing.Point(424, 3);
			this.parsedFileList.MultiSelect = false;
			this.parsedFileList.Name = "parsedFileList";
			this.parsedFileList.ShowGroups = false;
			this.parsedFileList.Size = new System.Drawing.Size(191, 583);
			this.parsedFileList.TabIndex = 3;
			this.parsedFileList.UseCompatibleStateImageBehavior = false;
			this.parsedFileList.View = System.Windows.Forms.View.List;
			this.parsedFileList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.parsedFileList_MouseDoubleClick);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(3, 3);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(160, 23);
			this.button3.TabIndex = 2;
			this.button3.Text = "Add Directory";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(169, 3);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(249, 23);
			this.button2.TabIndex = 1;
			this.button2.Text = "Remove selected directory from list";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// parsedFiles
			// 
			this.parsedFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.parsedFiles.FormattingEnabled = true;
			this.parsedFiles.Location = new System.Drawing.Point(3, 39);
			this.parsedFiles.Name = "parsedFiles";
			this.parsedFiles.Size = new System.Drawing.Size(415, 537);
			this.parsedFiles.TabIndex = 0;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Controls.Add(this.tabPage5);
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(626, 615);
			this.tabControl1.TabIndex = 3;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.restoreLastSession);
			this.tabPage2.Controls.Add(this.button6);
			this.tabPage2.Controls.Add(this.button5);
			this.tabPage2.Controls.Add(this.reopenLastPrj);
			this.tabPage2.Controls.Add(this.updates);
			this.tabPage2.Controls.Add(this.label7);
			this.tabPage2.Controls.Add(this.button4);
			this.tabPage2.Controls.Add(this.defPrjDir);
			this.tabPage2.Controls.Add(this.singleInst);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(618, 589);
			this.tabPage2.TabIndex = 2;
			this.tabPage2.Text = "IDE";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// restoreLastSession
			// 
			this.restoreLastSession.AutoSize = true;
			this.restoreLastSession.Location = new System.Drawing.Point(8, 159);
			this.restoreLastSession.Name = "restoreLastSession";
			this.restoreLastSession.Size = new System.Drawing.Size(170, 17);
			this.restoreLastSession.TabIndex = 8;
			this.restoreLastSession.Text = "Restore last session on startup";
			this.restoreLastSession.UseVisualStyleBackColor = true;
			// 
			// button6
			// 
			this.button6.Location = new System.Drawing.Point(8, 130);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(124, 23);
			this.button6.TabIndex = 7;
			this.button6.Text = "Restore Defaults";
			this.button6.UseVisualStyleBackColor = true;
			this.button6.Click += new System.EventHandler(this.button6_Click_1);
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(8, 101);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(250, 23);
			this.button5.TabIndex = 6;
			this.button5.Text = "Associate *.d; *.rc; *.dproj Files with D-IDE";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.Assoc_DProj_CheckedChanged);
			// 
			// reopenLastPrj
			// 
			this.reopenLastPrj.AutoSize = true;
			this.reopenLastPrj.Location = new System.Drawing.Point(8, 78);
			this.reopenLastPrj.Name = "reopenLastPrj";
			this.reopenLastPrj.Size = new System.Drawing.Size(168, 17);
			this.reopenLastPrj.TabIndex = 5;
			this.reopenLastPrj.Text = "Reopen last project on startup";
			this.reopenLastPrj.UseVisualStyleBackColor = true;
			// 
			// updates
			// 
			this.updates.AutoSize = true;
			this.updates.Enabled = false;
			this.updates.Location = new System.Drawing.Point(8, 29);
			this.updates.Name = "updates";
			this.updates.Size = new System.Drawing.Size(160, 17);
			this.updates.TabIndex = 4;
			this.updates.Text = "Check for updates at startup";
			this.updates.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(8, 55);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(122, 13);
			this.label7.TabIndex = 3;
			this.label7.Text = "Default Project Directory";
			// 
			// button4
			// 
			this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button4.Location = new System.Drawing.Point(586, 50);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(26, 22);
			this.button4.TabIndex = 2;
			this.button4.Text = "...";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// defPrjDir
			// 
			this.defPrjDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.defPrjDir.Location = new System.Drawing.Point(163, 52);
			this.defPrjDir.Name = "defPrjDir";
			this.defPrjDir.ReadOnly = true;
			this.defPrjDir.Size = new System.Drawing.Size(417, 20);
			this.defPrjDir.TabIndex = 1;
			// 
			// singleInst
			// 
			this.singleInst.AutoSize = true;
			this.singleInst.Location = new System.Drawing.Point(8, 6);
			this.singleInst.Name = "singleInst";
			this.singleInst.Size = new System.Drawing.Size(250, 17);
			this.singleInst.TabIndex = 0;
			this.singleInst.Text = "Allow just one instance of D-IDE (Needs restart)";
			this.singleInst.UseVisualStyleBackColor = true;
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add(this.AutoSkipUnknownCode);
			this.tabPage4.Controls.Add(this.verbosedbgoutput);
			this.tabPage4.Location = new System.Drawing.Point(4, 22);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage4.Size = new System.Drawing.Size(618, 589);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "Debugging";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// AutoSkipUnknownCode
			// 
			this.AutoSkipUnknownCode.AutoSize = true;
			this.AutoSkipUnknownCode.Location = new System.Drawing.Point(8, 29);
			this.AutoSkipUnknownCode.Name = "AutoSkipUnknownCode";
			this.AutoSkipUnknownCode.Size = new System.Drawing.Size(262, 17);
			this.AutoSkipUnknownCode.TabIndex = 26;
			this.AutoSkipUnknownCode.Text = "Skip non-resolvable code operations automatically";
			this.toolTip1.SetToolTip(this.AutoSkipUnknownCode, "Check this to skip code with an unknown source location automatically");
			this.AutoSkipUnknownCode.UseVisualStyleBackColor = true;
			// 
			// verbosedbgoutput
			// 
			this.verbosedbgoutput.AutoSize = true;
			this.verbosedbgoutput.Location = new System.Drawing.Point(8, 6);
			this.verbosedbgoutput.Name = "verbosedbgoutput";
			this.verbosedbgoutput.Size = new System.Drawing.Size(131, 17);
			this.verbosedbgoutput.TabIndex = 25;
			this.verbosedbgoutput.Text = "Verbose debug output";
			this.verbosedbgoutput.UseVisualStyleBackColor = true;
			// 
			// tabPage5
			// 
			this.tabPage5.Controls.Add(this.groupBox1);
			this.tabPage5.Location = new System.Drawing.Point(4, 22);
			this.tabPage5.Name = "tabPage5";
			this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage5.Size = new System.Drawing.Size(618, 589);
			this.tabPage5.TabIndex = 4;
			this.tabPage5.Text = "Editor";
			this.tabPage5.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.HighLightingSearchXSHD);
			this.groupBox1.Controls.Add(this.HighLightingAssocXSHDFile);
			this.groupBox1.Controls.Add(this.HighLightingDelExt);
			this.groupBox1.Controls.Add(this.HighlightingAddExt);
			this.groupBox1.Controls.Add(this.HighLightingExt);
			this.groupBox1.Controls.Add(this.HighLightingExts);
			this.groupBox1.Location = new System.Drawing.Point(8, 6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(607, 190);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Code highlighting (Changes require restart)";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(156, 74);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(215, 52);
			this.label10.TabIndex = 6;
			this.label10.Text = "-Enter extension like .d in the upper left field \r\n-Select .xshd file via \"...\"-B" +
				"utton\r\n-Click \"Add Extension\"\r\n-Restart D-IDE after editing your .xshd files";
			// 
			// HighLightingSearchXSHD
			// 
			this.HighLightingSearchXSHD.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HighLightingSearchXSHD.Location = new System.Drawing.Point(577, 46);
			this.HighLightingSearchXSHD.Name = "HighLightingSearchXSHD";
			this.HighLightingSearchXSHD.Size = new System.Drawing.Size(27, 23);
			this.HighLightingSearchXSHD.TabIndex = 5;
			this.HighLightingSearchXSHD.Text = "...";
			this.HighLightingSearchXSHD.UseVisualStyleBackColor = true;
			this.HighLightingSearchXSHD.Click += new System.EventHandler(this.HighLightingSearchXSHD_Click);
			// 
			// HighLightingAssocXSHDFile
			// 
			this.HighLightingAssocXSHDFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.HighLightingAssocXSHDFile.BackColor = System.Drawing.Color.White;
			this.HighLightingAssocXSHDFile.Location = new System.Drawing.Point(156, 48);
			this.HighLightingAssocXSHDFile.Name = "HighLightingAssocXSHDFile";
			this.HighLightingAssocXSHDFile.ReadOnly = true;
			this.HighLightingAssocXSHDFile.Size = new System.Drawing.Size(415, 20);
			this.HighLightingAssocXSHDFile.TabIndex = 4;
			// 
			// HighLightingDelExt
			// 
			this.HighLightingDelExt.Location = new System.Drawing.Point(100, 19);
			this.HighLightingDelExt.Name = "HighLightingDelExt";
			this.HighLightingDelExt.Size = new System.Drawing.Size(50, 23);
			this.HighLightingDelExt.TabIndex = 3;
			this.HighLightingDelExt.Text = "Delete";
			this.HighLightingDelExt.UseVisualStyleBackColor = true;
			this.HighLightingDelExt.Click += new System.EventHandler(this.HighLightingDelExt_Click);
			// 
			// HighlightingAddExt
			// 
			this.HighlightingAddExt.Location = new System.Drawing.Point(6, 19);
			this.HighlightingAddExt.Name = "HighlightingAddExt";
			this.HighlightingAddExt.Size = new System.Drawing.Size(88, 23);
			this.HighlightingAddExt.TabIndex = 2;
			this.HighlightingAddExt.Text = "Add Extension";
			this.HighlightingAddExt.UseVisualStyleBackColor = true;
			this.HighlightingAddExt.Click += new System.EventHandler(this.HighlightingAddExt_Click);
			// 
			// HighLightingExt
			// 
			this.HighLightingExt.Location = new System.Drawing.Point(6, 48);
			this.HighLightingExt.Name = "HighLightingExt";
			this.HighLightingExt.Size = new System.Drawing.Size(144, 20);
			this.HighLightingExt.TabIndex = 1;
			this.toolTip1.SetToolTip(this.HighLightingExt, "for example \".d\" or \".rc\" (no quotas!)");
			// 
			// HighLightingExts
			// 
			this.HighLightingExts.FormattingEnabled = true;
			this.HighLightingExts.Location = new System.Drawing.Point(6, 74);
			this.HighLightingExts.Name = "HighLightingExts";
			this.HighLightingExts.Size = new System.Drawing.Size(144, 108);
			this.HighLightingExts.TabIndex = 0;
			this.HighLightingExts.SelectedIndexChanged += new System.EventHandler(this.HighLightingExts_SelectedIndexChanged);
			// 
			// toolTip1
			// 
			this.toolTip1.AutomaticDelay = 250;
			// 
			// IDESettings
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(626, 656);
			this.ControlBox = false;
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDESettings";
			this.TabText = "Global Settings";
			this.Text = "Global Settings";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IDESettings_FormClosing);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage3.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			this.tabPage5.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TextBox exe_win;
		private System.Windows.Forms.TextBox exe_dll;
		private System.Windows.Forms.TextBox exe_cmp;
		private System.Windows.Forms.TextBox exe_console;
		private System.Windows.Forms.TextBox exe_rc;
		private System.Windows.Forms.TextBox exe_lib;
		private System.Windows.Forms.TextBox link_win_exe;
		private System.Windows.Forms.TextBox link_to_dll;
		private System.Windows.Forms.TextBox cmp_to_obj;
		private System.Windows.Forms.TextBox link_to_exe;
		private System.Windows.Forms.TextBox rc;
		private System.Windows.Forms.TextBox link_to_lib;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.ListView parsedFileList;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.ListBox parsedFiles;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.CheckBox singleInst;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.TextBox defPrjDir;
		private System.Windows.Forms.CheckBox updates;
		private System.Windows.Forms.CheckBox reopenLastPrj;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.TextBox dbg_exe;
        private System.Windows.Forms.TextBox dbg_args;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox logbuildprogress_chk;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.CheckBox showCompleteLog;
		private System.Windows.Forms.CheckBox UseIntegDbg;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.CheckBox verbosedbgoutput;
		private System.Windows.Forms.CheckBox AutoSkipUnknownCode;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox DoAutoSave;
		private System.Windows.Forms.CheckBox restoreLastSession;
		private System.Windows.Forms.TabPage tabPage5;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button HighLightingSearchXSHD;
		private System.Windows.Forms.TextBox HighLightingAssocXSHDFile;
		private System.Windows.Forms.Button HighLightingDelExt;
		private System.Windows.Forms.Button HighlightingAddExt;
		private System.Windows.Forms.TextBox HighLightingExt;
		private System.Windows.Forms.ListBox HighLightingExts;
		private System.Windows.Forms.Label label10;
	}
}