using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using System.Text;
using System.Windows.Forms;
using D_Parser;
using ICSharpCode.TextEditor;

namespace D_IDE
{
	public partial class ClassHierarchy : DockContent
	{
		public ClassHierarchy()
		{
			InitializeComponent();
		}

		public TreeNode SearchTN(DataType dt)
		{
			foreach(TreeNode tn in hierarchy.Nodes)
			{
				if(tn.Tag == dt) return tn;
				return SearchTN(tn,dt);
			}
			return null;
		}
		public TreeNode SearchTN(TreeNode env,DataType dt)
		{
			if(env == null) return null;
			foreach(TreeNode tn in env.Nodes)
			{
				if(tn.Tag == dt) return tn;
				return SearchTN(tn, dt);
			}
			return null;
		}

		bool hadClicked = false;
		private void hierarchy_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			DocumentInstanceWindow diw = Form1.SelectedTabPage;
			if(e.Node.Tag==null || diw == null) return;
			DataType dt = (DataType)e.Node.Tag;
			
			try
			{
				diw.txt.ActiveTextAreaControl.Caret.Position = new TextLocation(dt.startLoc.Column-1, dt.startLoc.Line-1);
				diw.txt.ActiveTextAreaControl.TextArea.Focus();
				diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
			}
			catch
			{
				return;
			}
			hadClicked = true;
		}

		private void hierarchy_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
		{
			if(hadClicked)
			{
				e.Cancel = true;
				hadClicked = false;
			}
		}
	}
}
