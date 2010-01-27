using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using D_IDE.FormsDesigner;
using System.Drawing;
using System.ComponentModel.Design;

namespace D_IDE
{
	class FXFormsDesigner : DockContent
	{
		public HostSurfaceManager surMgr=new HostSurfaceManager();

		public Form editForm
		{
			get {
				if(surMgr.ActiveDesignSurface.ComponentContainer.Components.Count > 0 && surMgr.ActiveDesignSurface.ComponentContainer.Components[0] is Form)
					return (Form)surMgr.ActiveDesignSurface.ComponentContainer.Components[0];
				else 
					return null;
			}
		}

		public FXFormsDesigner()
			: base()
		{
			surMgr.AddService(typeof(System.Windows.Forms.PropertyGrid),Form1.thisForm.propView.propertyGrid);
			try
			{
				Control hsc = surMgr.GetNewHost(typeof(Form));
				hsc.BackColor = Color.White;
				hsc.Dock = DockStyle.Fill;
				this.Controls.Add(hsc);

				editForm.Text = "aaaaa";

				Button b1 = new Button();
				b1.Text = "Test";
				b1.Location = new Point(10, 10);
				b1.Width = 100;
				b1.Height = 40;
				b1.Visible = true;
				editForm.Controls.Add(b1);

				IDesignerHost idh = (IDesignerHost)this.surMgr.ActiveDesignSurface.GetService(typeof(IDesignerHost));
				idh.Container.Add(b1);
			}
			catch
			{
				MessageBox.Show("Error in creating new host");
			}
		}
	}
}
