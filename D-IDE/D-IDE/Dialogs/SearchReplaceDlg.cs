using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace D_IDE
{
	public partial class SearchReplaceDlg : Form
	{
		#region Initializer
		public SearchReplaceDlg()
		{
			InitializeComponent();
		}

		private void SearchReplaceDlg_FormClosing(object sender, FormClosingEventArgs e)
		{
			this.Visible = false;
			e.Cancel = true;
		}

		private void CloseClick(object sender, EventArgs e)
		{
			Visible = false;
		}
		#endregion

		public int currentOffset;
		public string searchText
		{
			get { return search.Text; }
			set { search.Text = value; }
		}

		public void Search(string stext)
		{
			if(String.IsNullOrEmpty(stext))
			{
				MessageBox.Show("Enter search string first!");
				return;
			}
			currentOffset = Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.Offset;

			string text = Form1.SelectedTabPage.txt.Text;

			currentOffset = text.IndexOf(stext,
				currentOffset,
				caseSensitivity.Checked ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
			if(currentOffset < 0)
			{
				MessageBox.Show("No match found!");
				currentOffset = 0;
				return;
			}
			currentOffset += stext.Length;

			Form1.SelectedTabPage.txt.ActiveTextAreaControl.TextArea.Focus();
			Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.Position =
				Form1.SelectedTabPage.txt.ActiveTextAreaControl.Document.OffsetToPosition(currentOffset);
			Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
		}

		public void FindNextClick(object sender, EventArgs e)
		{
			Search(searchText);
		}

		private void ReplaceClick(object sender, EventArgs e)
		{
			string stext = searchText;
			if(stext == null || stext.Length < 1)
			{
				MessageBox.Show("Enter search string first!");
				return;
			}
			string replacement = replace.Text;
			currentOffset = Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.Offset;

			string text = Form1.SelectedTabPage.txt.Text;

			currentOffset = text.IndexOf(stext,
				currentOffset,
				caseSensitivity.Checked ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
			if(currentOffset < 0)
			{
				MessageBox.Show("No match found!");
				currentOffset = 0;
				return;
			}

			text = text.Remove(currentOffset, stext.Length);
			text = text.Insert(currentOffset, replacement);
			Form1.SelectedTabPage.txt.Text = text;

			currentOffset += replacement.Length;

			Form1.SelectedTabPage.txt.ActiveTextAreaControl.TextArea.Focus();
			if(currentOffset < Form1.SelectedTabPage.txt.Text.Length)
				Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.Position =
					Form1.SelectedTabPage.txt.ActiveTextAreaControl.Document.OffsetToPosition(currentOffset);
			Form1.SelectedTabPage.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
		}

		private void ReplaceAllClick(object sender, EventArgs e)
		{
			string stext = searchText;
			if(stext == null || stext.Length < 1)
			{
				MessageBox.Show("Enter search string first!");
				return;
			}
			string replacement = replace.Text;
			currentOffset = 0;
			string text = Form1.SelectedTabPage.txt.Text;

			while(currentOffset > -1)
			{
				currentOffset = text.IndexOf(stext,
					currentOffset,
					caseSensitivity.Checked ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

				if(currentOffset < 0) break;

				text = text.Remove(currentOffset, stext.Length);
				text = text.Insert(currentOffset, replacement);
				
				currentOffset += replacement.Length;
			}
			currentOffset = 0;
			Form1.SelectedTabPage.txt.Text = text;

			Form1.SelectedTabPage.txt.ActiveTextAreaControl.TextArea.Focus();
		}
	}
}
