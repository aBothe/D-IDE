namespace D_IDE
{
	partial class SearchReplaceDlg
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
			this.search = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.replace = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.caseSensitivity = new System.Windows.Forms.CheckBox();
			this.button3 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// search
			// 
			this.search.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.search.Location = new System.Drawing.Point(106, 12);
			this.search.Name = "search";
			this.search.Size = new System.Drawing.Size(392, 20);
			this.search.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(69, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Search string";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 41);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(69, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Replace with";
			// 
			// replace
			// 
			this.replace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.replace.Location = new System.Drawing.Point(106, 38);
			this.replace.Name = "replace";
			this.replace.Size = new System.Drawing.Size(392, 20);
			this.replace.TabIndex = 2;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(93, 64);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 5;
			this.button2.Text = "Replace";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.ReplaceClick);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(174, 64);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 6;
			this.button1.Text = "Replace All";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.ReplaceAllClick);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(12, 64);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(75, 23);
			this.button4.TabIndex = 8;
			this.button4.Text = "Find next";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.FindNextClick);
			// 
			// caseSensitivity
			// 
			this.caseSensitivity.AutoSize = true;
			this.caseSensitivity.Location = new System.Drawing.Point(12, 93);
			this.caseSensitivity.Name = "caseSensitivity";
			this.caseSensitivity.Size = new System.Drawing.Size(94, 17);
			this.caseSensitivity.TabIndex = 9;
			this.caseSensitivity.Text = "Case sensitive";
			this.caseSensitivity.UseVisualStyleBackColor = true;
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(423, 64);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 10;
			this.button3.Text = "Close";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.CloseClick);
			// 
			// SearchReplaceDlg
			// 
			this.AcceptButton = this.button4;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(510, 117);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.caseSensitivity);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.replace);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.search);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "SearchReplaceDlg";
			this.Opacity = 0.95;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Search and Replace";
			this.TopMost = true;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SearchReplaceDlg_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox search;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox replace;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.CheckBox caseSensitivity;
		private System.Windows.Forms.Button button3;
	}
}