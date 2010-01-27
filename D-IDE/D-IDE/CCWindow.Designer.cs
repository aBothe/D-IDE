namespace D_IDE
{
    partial class CCWindow
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
	    this.components = new System.ComponentModel.Container();
	    this.icons = new System.Windows.Forms.ImageList(this.components);
	    this.tTip = new System.Windows.Forms.ToolTip(this.components);
	    this.SuspendLayout();
	    // 
	    // icons
	    // 
	    this.icons.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
	    this.icons.ImageSize = new System.Drawing.Size(16, 16);
	    this.icons.TransparentColor = System.Drawing.Color.Transparent;
	    // 
	    // tTip
	    // 
	    this.tTip.AutomaticDelay = 0;
	    this.tTip.ShowAlways = true;
	    // 
	    // CCWindow
	    // 
	    this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
	    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
	    this.ClientSize = new System.Drawing.Size(228, 192);
	    this.ControlBox = false;
	    this.Name = "CCWindow";
	    this.ShowIcon = false;
	    this.ShowInTaskbar = false;
	    this.ResumeLayout(false);

	}

	#endregion

	private System.Windows.Forms.ImageList icons;
	private System.Windows.Forms.ToolTip tTip;
    }
}