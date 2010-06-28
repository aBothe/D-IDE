using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace D_IDE
{
	public partial class GoToLineDlg : Form
	{
		public GoToLineDlg()
		{
			InitializeComponent();
		}

		private void GoToLineDlg_FormClosing(object sender, FormClosingEventArgs e)
		{
			Visible = false;
			e.Cancel = true;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if(D_IDEForm.SelectedTabPage == null || textBox1.Text.Length<1) return;

			try
			{
				D_IDEForm.SelectedTabPage.txt.ActiveTextAreaControl.Caret.Line = Convert.ToInt32(textBox1.Text) - 1;
			}
			catch { }
			D_IDEForm.SelectedTabPage.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
		}

		private void GoToLineDlg_Enter(object sender, EventArgs e)
		{
			textBox1.Focus();
		}
	}
}
