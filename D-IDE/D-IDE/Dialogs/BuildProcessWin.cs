using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using D_Parser;
using WeifenLuo.WinFormsUI.Docking;

namespace D_IDE
{
	public partial class BuildProcessWin : DockContent
	{
		public BuildProcessWin()
		{
			InitializeComponent();
		}

		public void Log(string m)
		{
			if (tbox.TextLength < 1) tbox.Text = m;
			if (!String.IsNullOrEmpty(m))
				tbox.Text = m + (m.EndsWith("\r\n")?"":"\r\n") + tbox.Text;

			tbox.Refresh();
		}

		private void clearListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			tbox.ResetText();
		}

		public void Clear()
		{
			tbox.ResetText();
		}
	}

	public class OutputWin : BuildProcessWin
	{
		public OutputWin()
			: base()
		{
		}
	}
}
