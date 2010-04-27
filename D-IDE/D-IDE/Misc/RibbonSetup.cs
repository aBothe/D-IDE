using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace D_IDE.Misc
{
    class RibbonSetup
    {
        public Form1 Form;
        public RibbonButton LastFiles;

        public RibbonSetup(Form1 Form)
        {
            this.Form = Form;
            SetupRibbon();
        }

        void SetupRibbon()
        {
            Ribbon ribbon= Form.Helper.Ribbon = new Ribbon();
            
            // Both titlebar-drawn elements aren't needed - remove them
            ribbon.QuickAccessVisible = false;
            ribbon.OrbVisible = false;

            // Important: Set ribbon bar to become drawn inside of the main form
            ribbon.BorderMode = RibbonWindowMode.InsideWindow;

            // Because no orb bar shall is displayed we don't need a top margin
            Padding TabMargin = ribbon.TabsMargin;
            TabMargin.Top = 0;
            ribbon.TabsMargin = TabMargin;

            // Add all items to the ribbon bar
            InitMenuItems();

            // Add ribbon to the main form
            Form.Controls.Add(ribbon);

            // Set docking panel top to the height of the ribbon bar
            Form.dockPanel.Top = ribbon.MinimumSize.Height;
        }

        void InitMenuItems()
        {
            Ribbon r = Form.Helper.Ribbon;

            #region File tab
            RibbonTab FileTab = new RibbonTab(r, "File");

            #region File
            RibbonPanel pan1 = new RibbonPanel("File");
            RibbonButton but= new RibbonButton("New File",MenuIcons.New_48x48,Form.NewSourceFile);
            but.Style = RibbonButtonStyle.SplitDropDown;
            but.DropDownItems.Add(new RibbonButton("New File", Form.NewSourceFile));
            but.DropDownItems.Add(new RibbonButton("New Project",Form.NewProject));
            but.DropDownItems.Add(new RibbonSeparator());
            but.DropDownItems.Add(new RibbonButton("Add Existing", Form.AddExistingFile));
            pan1.Items.Add(but);
            //pan1.Items.Add(new RibbonButton("Add Existing",MenuIcons.Add_48x48,Form.AddExistingFile));
            pan1.Items.Add(new RibbonSeparator());
            LastFiles = new RibbonButton("Open", MenuIcons.Open_48x48, Form.OpenFile);
            LastFiles.Style = RibbonButtonStyle.SplitDropDown;
            pan1.Items.Add(LastFiles);
            pan1.Items.Add(new RibbonSeparator());

            RibbonButton but2 = new RibbonButton();
            but2.Style = System.Windows.Forms.RibbonButtonStyle.SplitDropDown;
            but2.Image = MenuIcons.Save_48x48;
            but2.Text = "Save";
            but2.Click += Form.SaveFile;

            RibbonItem i = new RibbonButton();
            i.Text = "Save";
            i.Click += Form.SaveFile;
            but2.DropDownItems.Add(i);
            i = new RibbonButton();
            i.Text = "Save as";
            i.Click += Form.SaveAs;
            but2.DropDownItems.Add(i);

            pan1.Items.Add(but2);
            pan1.Items.Add(new RibbonButton("Save All", MenuIcons.Save_48x48, Form.SaveAll));

            FileTab.Panels.Add(pan1);
            #endregion

            r.Tabs.Add(FileTab);
            #endregion
            #region Edit tab
            RibbonTab EditTab = new RibbonTab(r, "Edit");

            pan1 = new RibbonPanel("Clipboard");
            pan1.Items.Add(new RibbonButton("Copy",MenuIcons.Copy_48x48,Form.copyTBSButton_Click));
            pan1.Items.Add(new RibbonButton("Cut", MenuIcons.Cut_48x48, Form.cutTBSButton_Click));
            pan1.Items.Add(new RibbonButton("Paste", MenuIcons.Paste_48x48, Form.pasteTBSButton_Click));
            EditTab.Panels.Add(pan1);

            pan1 = new RibbonPanel("Search & Replace");
            pan1.Items.Add(new RibbonButton("Search", MenuIcons.Search_48x48, Form.searchReplaceToolStripMenuItem_Click));
            pan1.Items.Add(new RibbonButton("Find Next", Form.findNextToolStripMenuItem_Click));
            pan1.Items.Add(new RibbonSeparator());
            pan1.Items.Add(new RibbonButton("Go to line", Form.GotoLine));
            EditTab.Panels.Add(pan1);

            pan1 = new RibbonPanel("Undo actions");
            pan1.Items.Add(new RibbonButton("Undo", MenuIcons.Undo_48x48, delegate(Object o, EventArgs ea)
                {
                    if (Form1.SelectedTabPage != null)
                        Form1.SelectedTabPage.txt.Undo();
                }));
            pan1.Items.Add(new RibbonButton("Redo", MenuIcons.Redo_48x48, delegate(Object o, EventArgs ea)
            {
                if (Form1.SelectedTabPage != null)
                    Form1.SelectedTabPage.txt.Redo();
            }));
            EditTab.Panels.Add(pan1);

            r.Tabs.Add(EditTab);
            #endregion
            #region View tab
            RibbonTab ViewTab = new RibbonTab(r, "View");

            r.Tabs.Add(ViewTab);
            #endregion
            #region Project tab
            RibbonTab BuildTab = new RibbonTab(r, "Build");

            pan1 = new RibbonPanel("Build");
            pan1.Items.Add(new RibbonButton("Build Single",Form.BuildSingle));
            pan1.Items.Add(new RibbonButton("Build Project", Form.buildToolStripMenuItem_Click));
            BuildTab.Panels.Add(pan1);

            pan1 = new RibbonPanel("Debugging");
            pan1.Items.Add(new RibbonButton("Continue debugging", MenuIcons.Play_48x48, Form.dbgContinueClick));
            pan1.Items.Add(new RibbonButton("Stop", MenuIcons.Stop_48x48,Form.dbgStopButtonTS_Click));
            BuildTab.Panels.Add(pan1);

            r.Tabs.Add(BuildTab);
            #endregion
            #region Help tab
            RibbonTab HelpTab = new RibbonTab(r, "Help");

            r.Tabs.Add(HelpTab);
            #endregion
        }
    }
}
