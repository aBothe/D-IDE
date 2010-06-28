using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace D_IDE
{
	public partial class PropertyView : DockContent
	{
		public PropertyView()
		{
			InitializeComponent();

			
		}

		private void button1_Click(object sender, EventArgs e)
		{
			
		}

		private void button1_MouseDown(object sender, MouseEventArgs e)
		{
			if (!(D_IDEForm.thisForm.dockPanel.ActiveDocument is FXFormsDesigner)) return;
			FXFormsDesigner fd = (D_IDEForm.thisForm.dockPanel.ActiveDocument as FXFormsDesigner);

			IDesignerHost idh = (IDesignerHost)fd.surMgr.ActiveDesignSurface.GetService(typeof(IDesignerHost));

			IToolboxUser itu = (IToolboxUser)idh.GetDesigner(idh.RootComponent);
			itu.ToolPicked(new ToolboxItem(typeof(Button)));
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (!(D_IDEForm.thisForm.dockPanel.ActiveDocument is FXFormsDesigner)) return;
			FXFormsDesigner fd = (D_IDEForm.thisForm.dockPanel.ActiveDocument as FXFormsDesigner);

			IDesignerHost idh = (IDesignerHost)fd.surMgr.ActiveDesignSurface.GetService(typeof(IDesignerHost));

			IToolboxUser itu = (IToolboxUser)idh.GetDesigner(idh.RootComponent);
			itu.ToolPicked(new ToolboxItem(typeof(TextBox)));
		}	
	}
}
