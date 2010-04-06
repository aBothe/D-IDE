namespace D_IDE.Misc
{
    partial class SetupWizardDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.GroupBox groupBox1;
            System.Windows.Forms.GroupBox groupBox2;
            System.Windows.Forms.Button button4;
            System.Windows.Forms.Button button3;
            System.Windows.Forms.Button button2;
            System.Windows.Forms.Button button5;
            System.Windows.Forms.Button button6;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupWizardDialog));
            this.button1 = new System.Windows.Forms.Button();
            this.BinPath = new System.Windows.Forms.TextBox();
            this.ImportPaths = new System.Windows.Forms.ListBox();
            this.fD = new System.Windows.Forms.FolderBrowserDialog();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            button4 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            button5 = new System.Windows.Forms.Button();
            button6 = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(this.button1);
            groupBox1.Controls.Add(this.BinPath);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(424, 51);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Path where dmd.exe is located";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(390, 17);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(28, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.SelectBinPath);
            // 
            // BinPath
            // 
            this.BinPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.BinPath.Location = new System.Drawing.Point(6, 19);
            this.BinPath.Name = "BinPath";
            this.BinPath.Size = new System.Drawing.Size(378, 20);
            this.BinPath.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            groupBox2.Controls.Add(this.ImportPaths);
            groupBox2.Controls.Add(button4);
            groupBox2.Controls.Add(button3);
            groupBox2.Controls.Add(button2);
            groupBox2.Location = new System.Drawing.Point(12, 69);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(424, 180);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Import paths";
            // 
            // ImportPaths
            // 
            this.ImportPaths.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.ImportPaths.FormattingEnabled = true;
            this.ImportPaths.Location = new System.Drawing.Point(6, 48);
            this.ImportPaths.Name = "ImportPaths";
            this.ImportPaths.Size = new System.Drawing.Size(411, 121);
            this.ImportPaths.TabIndex = 3;
            // 
            // button4
            // 
            button4.Location = new System.Drawing.Point(317, 19);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(100, 23);
            button4.TabIndex = 2;
            button4.Text = "Delete selected";
            button4.UseVisualStyleBackColor = true;
            button4.Click += new System.EventHandler(this.RemovePath);
            // 
            // button3
            // 
            button3.Location = new System.Drawing.Point(191, 19);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(120, 23);
            button3.TabIndex = 1;
            button3.Text = "Add path manually";
            button3.UseVisualStyleBackColor = true;
            button3.Click += new System.EventHandler(this.AddPath);
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(6, 19);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(179, 23);
            button2.TabIndex = 0;
            button2.Text = "Guess import paths from bin path";
            button2.UseVisualStyleBackColor = true;
            button2.Click += new System.EventHandler(this.GuessImpPaths);
            // 
            // button5
            // 
            button5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            button5.Location = new System.Drawing.Point(336, 255);
            button5.Name = "button5";
            button5.Size = new System.Drawing.Size(100, 23);
            button5.TabIndex = 2;
            button5.Text = "Save settings";
            button5.UseVisualStyleBackColor = true;
            button5.Click += new System.EventHandler(this.SaveandClose);
            // 
            // button6
            // 
            button6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            button6.Location = new System.Drawing.Point(230, 255);
            button6.Name = "button6";
            button6.Size = new System.Drawing.Size(100, 23);
            button6.TabIndex = 3;
            button6.Text = "Skip";
            button6.UseVisualStyleBackColor = true;
            button6.Click += new System.EventHandler(this.Skip);
            // 
            // fD
            // 
            this.fD.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.fD.ShowNewFolderButton = false;
            // 
            // SetupWizardDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 290);
            this.Controls.Add(button6);
            this.Controls.Add(button5);
            this.Controls.Add(groupBox2);
            this.Controls.Add(groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SetupWizardDialog";
            this.Text = "First time setup";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox BinPath;
        private System.Windows.Forms.ListBox ImportPaths;
        private System.Windows.Forms.FolderBrowserDialog fD;
    }
}