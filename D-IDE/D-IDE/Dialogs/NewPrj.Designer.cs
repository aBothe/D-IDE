namespace D_IDE.Dialogs
{
    partial class NewPrj
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
	    if (disposing && (components != null))
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
        System.Windows.Forms.Label label1;
        System.Windows.Forms.Label label2;
        System.Windows.Forms.Label label3;
        System.Windows.Forms.Label label4;
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewPrj));
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.createPrjDir = new System.Windows.Forms.CheckBox();
        this.prjtype = new System.Windows.Forms.ComboBox();
        this.tarfile = new System.Windows.Forms.TextBox();
        this.button3 = new System.Windows.Forms.Button();
        this.prjdir = new System.Windows.Forms.TextBox();
        this.prjname = new System.Windows.Forms.TextBox();
        this.button1 = new System.Windows.Forms.Button();
        this.button2 = new System.Windows.Forms.Button();
        this.fD = new System.Windows.Forms.FolderBrowserDialog();
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        label4 = new System.Windows.Forms.Label();
        this.groupBox1.SuspendLayout();
        this.SuspendLayout();
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
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new System.Drawing.Point(6, 48);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(67, 13);
        label2.TabIndex = 3;
        label2.Text = "Project Type";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new System.Drawing.Point(6, 75);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(85, 13);
        label3.TabIndex = 5;
        label3.Text = "Project Directory";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new System.Drawing.Point(6, 124);
        label4.Name = "label4";
        label4.Size = new System.Drawing.Size(168, 13);
        label4.TabIndex = 9;
        label4.Text = "Target Filename without extension";
        // 
        // groupBox1
        // 
        this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.groupBox1.Controls.Add(this.createPrjDir);
        this.groupBox1.Controls.Add(this.prjtype);
        this.groupBox1.Controls.Add(label4);
        this.groupBox1.Controls.Add(this.tarfile);
        this.groupBox1.Controls.Add(this.button3);
        this.groupBox1.Controls.Add(label3);
        this.groupBox1.Controls.Add(this.prjdir);
        this.groupBox1.Controls.Add(label2);
        this.groupBox1.Controls.Add(label1);
        this.groupBox1.Controls.Add(this.prjname);
        this.groupBox1.Location = new System.Drawing.Point(12, 12);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(555, 152);
        this.groupBox1.TabIndex = 0;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "Generic";
        // 
        // createPrjDir
        // 
        this.createPrjDir.AutoSize = true;
        this.createPrjDir.Checked = true;
        this.createPrjDir.CheckState = System.Windows.Forms.CheckState.Checked;
        this.createPrjDir.Location = new System.Drawing.Point(181, 98);
        this.createPrjDir.Name = "createPrjDir";
        this.createPrjDir.Size = new System.Drawing.Size(136, 17);
        this.createPrjDir.TabIndex = 11;
        this.createPrjDir.Text = "Create Project directory";
        this.createPrjDir.UseVisualStyleBackColor = true;
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
        this.prjtype.Location = new System.Drawing.Point(181, 45);
        this.prjtype.Name = "prjtype";
        this.prjtype.Size = new System.Drawing.Size(368, 21);
        this.prjtype.TabIndex = 10;
        // 
        // tarfile
        // 
        this.tarfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.tarfile.Location = new System.Drawing.Point(181, 121);
        this.tarfile.Name = "tarfile";
        this.tarfile.Size = new System.Drawing.Size(368, 20);
        this.tarfile.TabIndex = 7;
        // 
        // button3
        // 
        this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.button3.Location = new System.Drawing.Point(523, 70);
        this.button3.Name = "button3";
        this.button3.Size = new System.Drawing.Size(26, 23);
        this.button3.TabIndex = 6;
        this.button3.Text = "...";
        this.button3.UseVisualStyleBackColor = true;
        this.button3.Click += new System.EventHandler(this.button3_Click);
        // 
        // prjdir
        // 
        this.prjdir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.prjdir.Location = new System.Drawing.Point(181, 72);
        this.prjdir.Name = "prjdir";
        this.prjdir.ReadOnly = true;
        this.prjdir.Size = new System.Drawing.Size(336, 20);
        this.prjdir.TabIndex = 4;
        // 
        // prjname
        // 
        this.prjname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.prjname.Location = new System.Drawing.Point(181, 19);
        this.prjname.Name = "prjname";
        this.prjname.Size = new System.Drawing.Size(368, 20);
        this.prjname.TabIndex = 0;
        this.prjname.TextChanged += new System.EventHandler(this.prjname_TextChanged);
        this.prjname.KeyUp += new System.Windows.Forms.KeyEventHandler(this.prjname_KeyUp);
        // 
        // button1
        // 
        this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.button1.Location = new System.Drawing.Point(411, 170);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(75, 23);
        this.button1.TabIndex = 1;
        this.button1.Text = "Create";
        this.button1.UseVisualStyleBackColor = true;
        this.button1.Click += new System.EventHandler(this.button1_Click);
        // 
        // button2
        // 
        this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.button2.Location = new System.Drawing.Point(492, 170);
        this.button2.Name = "button2";
        this.button2.Size = new System.Drawing.Size(75, 23);
        this.button2.TabIndex = 2;
        this.button2.Text = "Cancel";
        this.button2.UseVisualStyleBackColor = true;
        this.button2.Click += new System.EventHandler(this.button2_Click);
        // 
        // NewPrj
        // 
        this.AcceptButton = this.button1;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.button2;
        this.ClientSize = new System.Drawing.Size(579, 205);
        this.Controls.Add(this.button2);
        this.Controls.Add(this.button1);
        this.Controls.Add(this.groupBox1);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "NewPrj";
        this.ShowInTaskbar = false;
        this.Text = "Create new Project";
        this.TopMost = true;
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        this.ResumeLayout(false);

	}

	#endregion

	private System.Windows.Forms.GroupBox groupBox1;
	private System.Windows.Forms.Button button1;
	private System.Windows.Forms.Button button2;
	private System.Windows.Forms.TextBox prjname;
	private System.Windows.Forms.Button button3;
	private System.Windows.Forms.TextBox prjdir;
	private System.Windows.Forms.FolderBrowserDialog fD;
	private System.Windows.Forms.TextBox tarfile;
	private System.Windows.Forms.ComboBox prjtype;
	private System.Windows.Forms.CheckBox createPrjDir;
    }
}