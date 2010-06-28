using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using ICSharpCode.TextEditor.Document;

namespace D_IDE
{
	public partial class BreakpointWin : DockContent
	{
		public BreakpointWin()
		{
			InitializeComponent();
		}

		/// <summary>
		/// This is the main array for storing Breakpoints
		/// </summary>
		public Dictionary<string, List<DIDEBreakpoint>> Breakpoints = new Dictionary<string, List<DIDEBreakpoint>>();

		public void Clear()
		{
			list.Items.Clear();
		}

        /// <summary>
        /// Updates the breakpoint list
        /// </summary>
		public new void Update()
		{
			Clear();

			foreach (string file in Breakpoints.Keys)
			{
				foreach (DIDEBreakpoint dbp in Breakpoints[file])
				{
					ListViewItem lvi = new ListViewItem(Path.GetFileName(file));
					lvi.Tag = dbp;
					lvi.SubItems.Add(dbp.line.ToString());
					list.Items.Add(lvi);
				}
			}
		}

		public bool Remove(string file, int line)
		{
			if (GetBreakpointAt(file,line)==null)	return false;

			Breakpoints[file].Remove(GetBreakpointAt(file,line));

			Update();
			return true;
		}

		public DIDEBreakpoint GetBreakpointAt(string file, int line)
		{
			if (!Breakpoints.ContainsKey(file)) return null;

			foreach (DIDEBreakpoint dbp in Breakpoints[file])
			{
				if (dbp.line == line) return dbp;
			}
			return null;
		}

		public bool AddBreakpoint(string file, int line)
		{
			if (GetBreakpointAt(file,line)!=null) return false;

			if (!Breakpoints.ContainsKey(file))
				Breakpoints.Add(file, new List<DIDEBreakpoint>());
			
			Breakpoints[file].Add(new DIDEBreakpoint(file, line));

			Update();
			return true;
		}

		public static void NavigateToPosition(string file, int line)
		{
			DocumentInstanceWindow diw = D_IDEForm.thisForm.Open(file);
			if (diw != null)
			{
				diw.txt.ActiveTextAreaControl.Caret.Position = new ICSharpCode.TextEditor.TextLocation(0, line);
				diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
			}
		}

		private void list_DoubleClick(object sender, EventArgs e)
		{
			if (list.SelectedItems.Count < 1) return;

			DIDEBreakpoint dbp = (DIDEBreakpoint)list.SelectedItems[0].Tag;
			NavigateToPosition(dbp.file, dbp.line-1);
		}
	}
}
