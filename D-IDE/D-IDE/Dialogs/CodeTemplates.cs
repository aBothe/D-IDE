using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using ICSharpCode.TextEditor.Document;

namespace D_IDE.Dialogs
{
    public partial class CodeTemplates : DockContent
    {
        public CodeTemplates()
        {
            InitializeComponent();

            foreach (var variable in CodeTemplate.Variables)
                AddVar(variable.Key, variable.Value);
            foreach (var template in CodeTemplate.Templates)
                AddTemplate(template.Key, template.Value);


            tecTmplContent.TextEditorProperties.AllowCaretBeyondEOL = false;
            tecTmplContent.TextEditorProperties.AutoInsertCurlyBracket = true;
            tecTmplContent.TextEditorProperties.BracketMatchingStyle = BracketMatchingStyle.After;
            tecTmplContent.TextEditorProperties.ConvertTabsToSpaces = false;
            tecTmplContent.TextEditorProperties.DocumentSelectionMode = DocumentSelectionMode.Normal;
            tecTmplContent.TextEditorProperties.EnableFolding = true;
            tecTmplContent.TextEditorProperties.IsIconBarVisible = false;
            tecTmplContent.TextEditorProperties.LineViewerStyle = LineViewerStyle.FullRow;

            tecTmplContent.TextEditorProperties.ShowEOLMarker = false;
            tecTmplContent.TextEditorProperties.ShowHorizontalRuler = false;
            tecTmplContent.TextEditorProperties.ShowInvalidLines = false;
            tecTmplContent.TextEditorProperties.ShowLineNumbers = true;
            tecTmplContent.TextEditorProperties.ShowMatchingBracket = true;
            tecTmplContent.TextEditorProperties.ShowTabs = false;
            tecTmplContent.TextEditorProperties.ShowSpaces = false;
            tecTmplContent.TextEditorProperties.ShowVerticalRuler = false;

            tecTmplContent.SetHighlighting("D");

            tecTmplContent.TextEditorProperties.AutoInsertCurlyBracket = true;
            tecTmplContent.TextEditorProperties.IndentStyle = IndentStyle.Smart;

            tecTmplContent.Text = "//Click on a template to view its content.\r\n//Click on Save to save this content with the given name.";
        }

        /// <summary>
        /// Closes the form
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Adds a Template
        /// </summary>
        private void AddTemplate(string name, string content)
        {
            foreach (ListViewItem item in lvTemplates.Items)
            {
                if (item.Text == name)
                {
                    item.Tag = content;
                    return;
                }
            }
            ListViewItem litem = new ListViewItem(new string[] { name });
            litem.Tag = content;
            lvTemplates.Items.Add(litem);
        }
        /// <summary>
        /// Adds a var
        /// </summary>
        private void AddVar(string name, string content)
        {
            switch (name)
            {
                case "_module": content = "{Current Module}"; break;
                case "_package": content = "{Current Package (no leading, but trailing dot)}"; break;
                case "_date": content = "{Current Date}"; break;
                case "_filename": content = "{Current File (relative)}"; break;
            }

            foreach (ListViewItem item in lvVars.Items)
            {
                if (item.Text == name)
                {
                    item.SubItems[1].Text = content;
                    return;
                }
            }
            ListViewItem litem = new ListViewItem(new string[] { name, content });
            lvVars.Items.Add(litem);
        }

        /// <summary>
        /// Saves a var
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tbVarName.Text.Length == 0)
            {
                MessageBox.Show("Please enter at least some name!");
                return;
            }
            if (tbVarName.Text.StartsWith("_"))
            {
                MessageBox.Show("Thou may not define system variables!");
                return;
            }
            AddVar(tbVarName.Text, tbVarContent.Text);
            CodeTemplate.SetVar(tbVarName.Text, tbVarContent.Text);
        }

        /// <summary>
        /// Click on var
        /// </summary>
        private void lvVars_MouseClick(object sender, MouseEventArgs e)
        {
            if (lvVars.SelectedItems.Count > 0)
            {
                if (lvVars.SelectedItems[0].Text.StartsWith("_")) return;
                tbVarName.Text = lvVars.SelectedItems[0].Text;
                tbVarContent.Text = lvVars.SelectedItems[0].SubItems[1].Text;
            }
        }

        /// <summary>
        /// Delete a var
        /// </summary>
        private void btnVarDelete_Click(object sender, EventArgs e)
        {
            if (tbVarName.Text.StartsWith("_"))
            {
                MessageBox.Show("Thou may not delete system variables!");
                return;
            }
            CodeTemplate.DeleteVar(tbVarName.Text);
            foreach (ListViewItem item in lvVars.Items)
            {
                if (item.Text == tbVarName.Text)
                {
                    lvVars.Items.Remove(item);
                    break;
                }
            }
        }

        /// <summary>
        /// Click on template
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvTemplates_MouseClick(object sender, MouseEventArgs e)
        {
            if (lvTemplates.SelectedItems.Count > 0)
            {
                tbTmplName.Text = lvTemplates.SelectedItems[0].Text;
                tecTmplContent.Text = lvTemplates.SelectedItems[0].Tag as string;
            }
        }

        /// <summary>
        /// Save template
        /// </summary>
        private void btnTmplSave_Click(object sender, EventArgs e)
        {
            if (tbTmplName.Text.Length == 0)
            {
                MessageBox.Show("Please enter at least some name!");
                return;
            }
            AddTemplate(tbTmplName.Text, tecTmplContent.Text);
            CodeTemplate.SetVar(tbTmplName.Text, tecTmplContent.Text);
        }

        /// <summary>
        /// Delete Template
        /// </summary>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (tbTmplName.Text.StartsWith("_"))
            {
                MessageBox.Show("Thou may not delete system templates!");
                return;
            }
            CodeTemplate.DeleteTemplate(tbTmplName.Text);
            foreach (ListViewItem item in lvTemplates.Items)
            {
                if (item.Text == tbTmplName.Text)
                {
                    lvTemplates.Items.Remove(item);
                    break;
                }
            }
        }
    }
}
