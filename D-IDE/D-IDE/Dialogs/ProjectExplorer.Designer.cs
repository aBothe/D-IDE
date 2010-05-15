using WeifenLuo.WinFormsUI.Docking;
namespace D_IDE
{
	partial class ProjectExplorer : DockContent
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectExplorer));
			this.prjFiles = new System.Windows.Forms.TreeView();
			this.fileIcons = new System.Windows.Forms.ImageList(this.components);
			this.DSourceMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openInFormsEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ProjectMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.addNewFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.addExistingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.directoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.setAsActiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.removeProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DSourceMenu.SuspendLayout();
			this.ProjectMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// prjFiles
			// 
			this.prjFiles.AllowDrop = true;
			this.prjFiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.prjFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.prjFiles.HideSelection = false;
			this.prjFiles.ImageIndex = 4;
			this.prjFiles.ImageList = this.fileIcons;
			this.prjFiles.Location = new System.Drawing.Point(0, 0);
			this.prjFiles.Name = "prjFiles";
			this.prjFiles.SelectedImageIndex = 4;
			this.prjFiles.Size = new System.Drawing.Size(221, 352);
			this.prjFiles.TabIndex = 0;
			this.prjFiles.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.prjFiles_NodeMouseDoubleClick);
			this.prjFiles.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.prjFiles_AfterCollapse);
			this.prjFiles.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.AfterLabelEdit);
			this.prjFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragDrop);
			this.prjFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragOver);
			this.prjFiles.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.prjFiles_NodeMouseClick);
			this.prjFiles.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.prjFiles_AfterExpand);
			this.prjFiles.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.prjFiles_ItemDrag);
			this.prjFiles.DragOver += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragOver);
			// 
			// fileIcons
			// 
			this.fileIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("fileIcons.ImageStream")));
			this.fileIcons.TransparentColor = System.Drawing.Color.Transparent;
			this.fileIcons.Images.SetKeyName(0, "dir");
			this.fileIcons.Images.SetKeyName(1, "dir_open");
			this.fileIcons.Images.SetKeyName(2, ".d");
			this.fileIcons.Images.SetKeyName(3, ".dproj");
			this.fileIcons.Images.SetKeyName(4, ".rc");
			// 
			// DSourceMenu
			// 
			this.DSourceMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.removeToolStripMenuItem,
            this.openInFormsEditorToolStripMenuItem});
			this.DSourceMenu.Name = "contextMenuStrip1";
			this.DSourceMenu.Size = new System.Drawing.Size(187, 70);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.openToolStripMenuItem.Text = "Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// removeToolStripMenuItem
			// 
			this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			this.removeToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.removeToolStripMenuItem.Text = "Remove";
			this.removeToolStripMenuItem.Click += new System.EventHandler(this.RemoveFile);
			// 
			// openInFormsEditorToolStripMenuItem
			// 
			this.openInFormsEditorToolStripMenuItem.Name = "openInFormsEditorToolStripMenuItem";
			this.openInFormsEditorToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.openInFormsEditorToolStripMenuItem.Text = "Open in Forms Editor";
			this.openInFormsEditorToolStripMenuItem.Click += new System.EventHandler(this.openInFormsEditorToolStripMenuItem_Click);
			// 
			// ProjectMenu
			// 
			this.ProjectMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewFileToolStripMenuItem,
            this.addExistingToolStripMenuItem,
            this.toolStripSeparator1,
            this.setAsActiveToolStripMenuItem,
            this.propertiesToolStripMenuItem,
            this.toolStripSeparator2,
            this.removeProjectToolStripMenuItem});
			this.ProjectMenu.Name = "ProjectMenu";
			this.ProjectMenu.Size = new System.Drawing.Size(158, 148);
			// 
			// addNewFileToolStripMenuItem
			// 
			this.addNewFileToolStripMenuItem.Name = "addNewFileToolStripMenuItem";
			this.addNewFileToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.addNewFileToolStripMenuItem.Text = "Add New File";
			this.addNewFileToolStripMenuItem.Click += new System.EventHandler(this.addNewFileToolStripMenuItem_Click);
			// 
			// addExistingToolStripMenuItem
			// 
			this.addExistingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.directoryToolStripMenuItem});
			this.addExistingToolStripMenuItem.Name = "addExistingToolStripMenuItem";
			this.addExistingToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.addExistingToolStripMenuItem.Text = "Add Existing";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
			this.fileToolStripMenuItem.Text = "File";
			this.fileToolStripMenuItem.Click += new System.EventHandler(this.addExistingToolStripMenuItem_Click);
			// 
			// directoryToolStripMenuItem
			// 
			this.directoryToolStripMenuItem.Name = "directoryToolStripMenuItem";
			this.directoryToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
			this.directoryToolStripMenuItem.Text = "Directory";
			this.directoryToolStripMenuItem.Click += new System.EventHandler(this.directoryToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(154, 6);
			// 
			// setAsActiveToolStripMenuItem
			// 
			this.setAsActiveToolStripMenuItem.Name = "setAsActiveToolStripMenuItem";
			this.setAsActiveToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.setAsActiveToolStripMenuItem.Text = "Set as active";
			this.setAsActiveToolStripMenuItem.Click += new System.EventHandler(this.setAsActiveToolStripMenuItem_Click);
			// 
			// propertiesToolStripMenuItem
			// 
			this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
			this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.propertiesToolStripMenuItem.Text = "Properties";
			this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(154, 6);
			// 
			// removeProjectToolStripMenuItem
			// 
			this.removeProjectToolStripMenuItem.Name = "removeProjectToolStripMenuItem";
			this.removeProjectToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.removeProjectToolStripMenuItem.Text = "Remove project";
			this.removeProjectToolStripMenuItem.Click += new System.EventHandler(this.removeProjectToolStripMenuItem_Click);
			// 
			// ProjectExplorer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(221, 352);
			this.Controls.Add(this.prjFiles);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)));
			this.HideOnClose = true;
			this.Name = "ProjectExplorer";
			this.TabText = "Project Files";
			this.Text = "ProjectExplorer";
			this.DSourceMenu.ResumeLayout(false);
			this.ProjectMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.TreeView prjFiles;
		private System.Windows.Forms.ContextMenuStrip DSourceMenu;
		private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip ProjectMenu;
		private System.Windows.Forms.ToolStripMenuItem addNewFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem addExistingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem setAsActiveToolStripMenuItem;
		public System.Windows.Forms.ImageList fileIcons;
		private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openInFormsEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem directoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem removeProjectToolStripMenuItem;
	}
}