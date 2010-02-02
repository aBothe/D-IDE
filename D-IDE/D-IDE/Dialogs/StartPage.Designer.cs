namespace D_IDE
{
	partial class StartPage
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lastProjects = new System.Windows.Forms.ListBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lastFiles = new System.Windows.Forms.ListBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.web = new System.Windows.Forms.WebBrowser();
			this.chTimer = new System.Windows.Forms.Timer(this.components);
			this.doCheckAtStartCBox = new System.Windows.Forms.CheckBox();
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lastProjects);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(284, 268);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Last opened projects";
			// 
			// lastProjects
			// 
			this.lastProjects.BackColor = System.Drawing.Color.White;
			this.lastProjects.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.lastProjects.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lastProjects.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lastProjects.IntegralHeight = false;
			this.lastProjects.ItemHeight = 18;
			this.lastProjects.Items.AddRange(new object[] {
            "Test",
            "A",
            "B"});
			this.lastProjects.Location = new System.Drawing.Point(3, 16);
			this.lastProjects.Name = "lastProjects";
			this.lastProjects.Size = new System.Drawing.Size(278, 249);
			this.lastProjects.TabIndex = 0;
			this.lastProjects.DoubleClick += new System.EventHandler(this.lastProjects_DoubleClick);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox2.Controls.Add(this.lastFiles);
			this.groupBox2.Location = new System.Drawing.Point(12, 317);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(284, 280);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Last opened files";
			// 
			// lastFiles
			// 
			this.lastFiles.BackColor = System.Drawing.Color.White;
			this.lastFiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.lastFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lastFiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lastFiles.IntegralHeight = false;
			this.lastFiles.ItemHeight = 18;
			this.lastFiles.Items.AddRange(new object[] {
            "Test",
            "A",
            "B"});
			this.lastFiles.Location = new System.Drawing.Point(3, 16);
			this.lastFiles.Name = "lastFiles";
			this.lastFiles.Size = new System.Drawing.Size(278, 261);
			this.lastFiles.TabIndex = 0;
			this.lastFiles.DoubleClick += new System.EventHandler(this.lastFiles_DoubleClick);
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.web);
			this.groupBox3.Location = new System.Drawing.Point(302, 12);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(529, 559);
			this.groupBox3.TabIndex = 2;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Latest news from d-ide.sourceforge.net";
			// 
			// web
			// 
			this.web.AllowNavigation = false;
			this.web.AllowWebBrowserDrop = false;
			this.web.Dock = System.Windows.Forms.DockStyle.Fill;
			this.web.IsWebBrowserContextMenuEnabled = false;
			this.web.Location = new System.Drawing.Point(3, 16);
			this.web.MinimumSize = new System.Drawing.Size(20, 20);
			this.web.Name = "web";
			this.web.Size = new System.Drawing.Size(523, 540);
			this.web.TabIndex = 1;
			this.web.WebBrowserShortcutsEnabled = false;
			// 
			// chTimer
			// 
			this.chTimer.Interval = 1000;
			this.chTimer.Tick += new System.EventHandler(this.chTimer_Tick);
			// 
			// doCheckAtStartCBox
			// 
			this.doCheckAtStartCBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.doCheckAtStartCBox.AutoSize = true;
			this.doCheckAtStartCBox.Location = new System.Drawing.Point(305, 577);
			this.doCheckAtStartCBox.Name = "doCheckAtStartCBox";
			this.doCheckAtStartCBox.Size = new System.Drawing.Size(141, 17);
			this.doCheckAtStartCBox.TabIndex = 3;
			this.doCheckAtStartCBox.Text = "Retrieve news at startup";
			this.doCheckAtStartCBox.UseVisualStyleBackColor = true;
			this.doCheckAtStartCBox.CheckedChanged += new System.EventHandler(this.doCheckAtStartCBox_CheckedChanged);
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.Location = new System.Drawing.Point(753, 574);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 5;
			this.button2.Text = "Close";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 286);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(140, 23);
			this.button1.TabIndex = 6;
			this.button1.Text = "Create Project";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click_1);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(156, 286);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(140, 23);
			this.button3.TabIndex = 7;
			this.button3.Text = "Open Selected";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.lastProjects_DoubleClick);
			// 
			// StartPage
			// 
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(843, 611);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.doCheckAtStartCBox);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.DoubleBuffered = true;
			this.HideOnClose = true;
			this.Name = "StartPage";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.Document;
			this.TabText = "StartPage";
			this.Text = "StartPage";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		public System.Windows.Forms.ListBox lastProjects;
		private System.Windows.Forms.GroupBox groupBox2;
		public System.Windows.Forms.ListBox lastFiles;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Timer chTimer;
		private System.Windows.Forms.CheckBox doCheckAtStartCBox;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button3;
		public System.Windows.Forms.WebBrowser web;
	}
}