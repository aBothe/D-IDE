using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using DebugEngineWrapper;

namespace D_IDE
{
	public partial class CallStackWin : DockContent
	{
		public CallStackWin()
		{
			InitializeComponent();
		}

		public void Clear()
		{
			list.Items.Clear();
		}

		public new void Update()
		{
			Clear();
			try
			{
				if (Form1.thisForm.IsDebugging)
					foreach (StackFrame sf in Form1.thisForm.dbg.CallStack)
					{
						
						string n = Form1.thisForm.dbg.Symbols.GetNameByOffset(sf.InstructionOffset);
						if (n == String.Empty) continue;
						int i = n.LastIndexOf("!");

						ListViewItem lvi = new ListViewItem(n.Substring(0, i));
						lvi.Tag = sf;
						lvi.SubItems.Add(n.Substring(i + 1));
						string fn;
						uint ln;
						if (Form1.thisForm.dbg.Symbols.GetLineByOffset(sf.InstructionOffset, out fn, out ln))
						{
							lvi.SubItems.Add(fn);
							lvi.SubItems.Add((ln).ToString());
						}
						list.Items.Add(lvi);
					}
			}catch{}
		}

		private void list_DoubleClick(object sender, EventArgs e)
		{
			if (list.SelectedItems.Count < 1) return;

			//StackFrame sf = (StackFrame)list.SelectedItems[0].Tag;
			if (list.SelectedItems[0].SubItems.Count >= 4)
			{
				BreakpointWin.NavigateToPosition(list.SelectedItems[0].SubItems[2].Text, Convert.ToInt32(list.SelectedItems[0].SubItems[3].Text)-1);
			}
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Update();
		}
	}
}
