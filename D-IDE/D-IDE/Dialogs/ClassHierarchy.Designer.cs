using WeifenLuo.WinFormsUI.Docking;
namespace D_IDE
{
	public partial class ClassHierarchy : DockContent
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
            this.hierarchy = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // hierarchy
            // 
            this.hierarchy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hierarchy.HideSelection = false;
            this.hierarchy.Location = new System.Drawing.Point(0, 0);
            this.hierarchy.Name = "hierarchy";
            this.hierarchy.ShowNodeToolTips = true;
            this.hierarchy.Size = new System.Drawing.Size(291, 365);
            this.hierarchy.TabIndex = 0;
            this.hierarchy.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.hierarchy_BeforeCollapse);
            this.hierarchy.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.NodeMouseClick);
            this.hierarchy.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.hierarchy_NodeMouseDoubleClick);
            // 
            // ClassHierarchy
            // 
            this.AutoHidePortion = 0.15D;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(291, 365);
            this.Controls.Add(this.hierarchy);
            this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)));
            this.HideOnClose = true;
            this.Name = "ClassHierarchy";
            this.TabText = "Outline";
            this.Text = "Outline";
            this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.TreeView hierarchy;

	}
}