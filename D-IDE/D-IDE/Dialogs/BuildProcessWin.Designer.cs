using WeifenLuo.WinFormsUI.Docking;
namespace D_IDE
{
    partial class BuildProcessWin:DockContent
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
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.clearListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.tbox = new System.Windows.Forms.TextBox();
		this.menuStrip1.SuspendLayout();
		this.SuspendLayout();
		// 
		// menuStrip1
		// 
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearListToolStripMenuItem});
		this.menuStrip1.Location = new System.Drawing.Point(0, 0);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Size = new System.Drawing.Size(639, 24);
		this.menuStrip1.TabIndex = 1;
		this.menuStrip1.Text = "menuStrip1";
		// 
		// clearListToolStripMenuItem
		// 
		this.clearListToolStripMenuItem.Name = "clearListToolStripMenuItem";
		this.clearListToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
		this.clearListToolStripMenuItem.Text = "Clear";
		this.clearListToolStripMenuItem.Click += new System.EventHandler(this.clearListToolStripMenuItem_Click);
		// 
		// tbox
		// 
		this.tbox.BackColor = System.Drawing.Color.White;
		this.tbox.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tbox.Location = new System.Drawing.Point(0, 24);
		this.tbox.Multiline = true;
		this.tbox.Name = "tbox";
		this.tbox.ReadOnly = true;
		this.tbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this.tbox.Size = new System.Drawing.Size(639, 297);
		this.tbox.TabIndex = 2;
		// 
		// BuildProcessWin
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.ClientSize = new System.Drawing.Size(639, 321);
		this.Controls.Add(this.tbox);
		this.Controls.Add(this.menuStrip1);
		this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)(((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
					| WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
		this.HideOnClose = true;
		this.MainMenuStrip = this.menuStrip1;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "BuildProcessWin";
		this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide;
		this.ShowIcon = false;
		this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.TabText = "Build Log";
		this.Text = "Build Log";
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		this.ResumeLayout(false);
		this.PerformLayout();

	}

	#endregion

	private System.Windows.Forms.MenuStrip menuStrip1;
	private System.Windows.Forms.ToolStripMenuItem clearListToolStripMenuItem;
	private System.Windows.Forms.TextBox tbox;

    }
}