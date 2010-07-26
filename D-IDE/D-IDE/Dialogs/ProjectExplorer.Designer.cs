﻿using WeifenLuo.WinFormsUI.Docking;
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
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.ProjectMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addNewFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDClassToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addExistingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.directoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.setAsActiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openInExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.removeProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FolderMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.DSourceMenu.SuspendLayout();
            this.ProjectMenu.SuspendLayout();
            this.FolderMenu.SuspendLayout();
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
            this.prjFiles.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.AfterLabelEdit);
            this.prjFiles.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.prjFiles_AfterCollapse);
            this.prjFiles.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.prjFiles_AfterExpand);
            this.prjFiles.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.prjFiles_ItemDrag);
            this.prjFiles.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.prjFiles_NodeMouseClick);
            this.prjFiles.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.prjFiles_NodeMouseDoubleClick);
            this.prjFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragDrop);
            this.prjFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragOver);
            this.prjFiles.DragOver += new System.Windows.Forms.DragEventHandler(this.prjFiles_DragOver);
            this.prjFiles.KeyUp += new System.Windows.Forms.KeyEventHandler(this.prjFiles_KeyUp);
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
            this.openInFormsEditorToolStripMenuItem,
            this.toolStripMenuItem8});
            this.DSourceMenu.Name = "contextMenuStrip1";
            this.DSourceMenu.Size = new System.Drawing.Size(187, 92);
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
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(186, 22);
            this.toolStripMenuItem8.Text = "Open in Explorer";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.openInExplorerToolStripMenuItem_Click);
            // 
            // ProjectMenu
            // 
            this.ProjectMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewFileToolStripMenuItem,
            this.addDClassToolStripMenuItem,
            this.addFolderToolStripMenuItem,
            this.addExistingToolStripMenuItem,
            this.toolStripSeparator1,
            this.setAsActiveToolStripMenuItem,
            this.propertiesToolStripMenuItem,
            this.openInExplorerToolStripMenuItem,
            this.toolStripSeparator2,
            this.removeProjectToolStripMenuItem});
            this.ProjectMenu.Name = "ProjectMenu";
            this.ProjectMenu.Size = new System.Drawing.Size(190, 192);
            // 
            // addNewFileToolStripMenuItem
            // 
            this.addNewFileToolStripMenuItem.Name = "addNewFileToolStripMenuItem";
            this.addNewFileToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.addNewFileToolStripMenuItem.Text = "Add New File";
            this.addNewFileToolStripMenuItem.Click += new System.EventHandler(this.addNewFileToolStripMenuItem_Click);
            // 
            // addDClassToolStripMenuItem
            // 
            this.addDClassToolStripMenuItem.Name = "addDClassToolStripMenuItem";
            this.addDClassToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.addDClassToolStripMenuItem.Text = "Add D Class (Module)";
            this.addDClassToolStripMenuItem.Click += new System.EventHandler(this.addDClassToolStripMenuItem_Click);
            // 
            // addFolderToolStripMenuItem
            // 
            this.addFolderToolStripMenuItem.Name = "addFolderToolStripMenuItem";
            this.addFolderToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.addFolderToolStripMenuItem.Text = "Add Folder (Package)";
            this.addFolderToolStripMenuItem.Click += new System.EventHandler(this.addFolderToolStripMenuItem_Click);
            // 
            // addExistingToolStripMenuItem
            // 
            this.addExistingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.directoryToolStripMenuItem});
            this.addExistingToolStripMenuItem.Name = "addExistingToolStripMenuItem";
            this.addExistingToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.addExistingToolStripMenuItem.Text = "Add Existing";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.fileToolStripMenuItem.Text = "File";
            this.fileToolStripMenuItem.Click += new System.EventHandler(this.addExistingToolStripMenuItem_Click);
            // 
            // directoryToolStripMenuItem
            // 
            this.directoryToolStripMenuItem.Name = "directoryToolStripMenuItem";
            this.directoryToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.directoryToolStripMenuItem.Text = "Directory";
            this.directoryToolStripMenuItem.Click += new System.EventHandler(this.directoryToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(186, 6);
            // 
            // setAsActiveToolStripMenuItem
            // 
            this.setAsActiveToolStripMenuItem.Name = "setAsActiveToolStripMenuItem";
            this.setAsActiveToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.setAsActiveToolStripMenuItem.Text = "Set as active";
            this.setAsActiveToolStripMenuItem.Click += new System.EventHandler(this.setAsActiveToolStripMenuItem_Click);
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.propertiesToolStripMenuItem.Text = "Properties";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
            // 
            // openInExplorerToolStripMenuItem
            // 
            this.openInExplorerToolStripMenuItem.Name = "openInExplorerToolStripMenuItem";
            this.openInExplorerToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.openInExplorerToolStripMenuItem.Text = "Open in Explorer";
            this.openInExplorerToolStripMenuItem.Click += new System.EventHandler(this.openInExplorerToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(186, 6);
            // 
            // removeProjectToolStripMenuItem
            // 
            this.removeProjectToolStripMenuItem.Name = "removeProjectToolStripMenuItem";
            this.removeProjectToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.removeProjectToolStripMenuItem.Text = "Remove project";
            this.removeProjectToolStripMenuItem.Click += new System.EventHandler(this.RemoveProject_Click);
            // 
            // FolderMenu
            // 
            this.FolderMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripSeparator3,
            this.toolStripMenuItem7});
            this.FolderMenu.Name = "FolderMenu";
            this.FolderMenu.Size = new System.Drawing.Size(190, 142);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(189, 22);
            this.toolStripMenuItem1.Text = "Add New File";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.addNewFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(189, 22);
            this.toolStripMenuItem2.Text = "Add D Class (Module)";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.addDClassToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(189, 22);
            this.toolStripMenuItem3.Text = "Add Folder (Package)";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.addFolderToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem5,
            this.toolStripMenuItem6});
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(189, 22);
            this.toolStripMenuItem4.Text = "Add Existing";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.addExistingToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem5.Text = "File";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.addExistingToolStripMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem6.Text = "Directory";
            this.toolStripMenuItem6.Click += new System.EventHandler(this.directoryToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(186, 6);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(189, 22);
            this.toolStripMenuItem7.Text = "Open in Explorer";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.openInExplorerToolStripMenuItem_Click);
            // 
            // ProjectExplorer
            // 
            this.AutoHidePortion = 0.15D;
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
            this.FolderMenu.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripMenuItem addDClassToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openInExplorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFolderToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip FolderMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
	}
}