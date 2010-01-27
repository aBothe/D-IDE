using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace ToolboxLibrary
{
	/// <summary>
    /// ToolboxUIManagerVS
	/// </summary>
	public class ToolboxUIManagerVS
	{
		private ToolBoxSvc m_toolbox;
		private System.Drawing.Design.ToolboxItem pointer; // a "null" tool
		public ToolboxUIManagerVS(ToolBoxSvc toolbox)
		{
			m_toolbox = toolbox;
			pointer = new System.Drawing.Design.ToolboxItem();
			pointer.DisplayName = "<Pointer>";
			pointer.Bitmap = new System.Drawing.Bitmap(16, 16);
		}
		private ToolBoxSvc Toolbox
		{
			get
			{
				return m_toolbox;
			}
		}
		public void FillToolbox()
		{
			CreateControls();
			ConfigureControls();
			UpdateToolboxItems(Toolbox.Tabs.Count - 1);
		}
		private void CreateControls()
		{
			Toolbox.Controls.Clear();
			Toolbox.ToolsListBox = new ListBox();
			Toolbox.TabPageArray = new Button[Toolbox.Tabs.Count];
		}
		private void ConfigureControls()
		{
			Toolbox.SuspendLayout();
			for (int i = Toolbox.Tabs.Count - 1; i >= 0; i--)
			{
				// 
				// Tab Button
				// 
				Button button = new Button();

				button.Dock = System.Windows.Forms.DockStyle.Top;
				button.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
				button.Location = new System.Drawing.Point(0, (i + 1) * 20);
				button.Name = Toolbox.Tabs[i].Name;
				button.Size = new System.Drawing.Size(Toolbox.Width, 20);
				button.TabIndex = i + 1;
				button.Text = Toolbox.Tabs[i].Name;
				button.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				button.Tag = i;
				button.Click += new EventHandler(button_Click);
				Toolbox.Controls.Add(button);
				Toolbox.TabPageArray[i] = button;
			}

			// 
			// toolboxTitleButton
			// 
			Button toolboxTitleButton = new Button();

			toolboxTitleButton.BackColor = System.Drawing.SystemColors.ActiveCaption;
			toolboxTitleButton.Dock = System.Windows.Forms.DockStyle.Top;
			toolboxTitleButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			toolboxTitleButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
			toolboxTitleButton.Location = new System.Drawing.Point(0, 0);
			toolboxTitleButton.Name = "toolboxTitleButton";
			toolboxTitleButton.Size = new System.Drawing.Size(Toolbox.Width, 20);
			toolboxTitleButton.TabIndex = 0;
			toolboxTitleButton.Text = "Toolbox";
			toolboxTitleButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			Toolbox.Controls.Add(toolboxTitleButton);

			// 
			// listBox
			// 
			ListBox listBox = new ListBox();

			listBox.BackColor = System.Drawing.SystemColors.ControlLight;
			listBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			listBox.ItemHeight = 18;
			listBox.Location = new System.Drawing.Point(0, (Toolbox.Tabs.Count + 1) * 20);
			listBox.Name = "ToolsListBox";
			listBox.Size = new System.Drawing.Size(Toolbox.Width, Toolbox.Height - (Toolbox.Tabs.Count + 1) * 20);
			listBox.TabIndex = Toolbox.Tabs.Count + 1;
			
			Toolbox.Controls.Add(listBox);
			UpdateToolboxItems(Toolbox.Tabs.Count - 1);
			Toolbox.ResumeLayout();
			Toolbox.ToolsListBox = listBox;
			Toolbox.SizeChanged += new EventHandler(Toolbox_SizeChanged);
		}

		private void UpdateToolboxItems(int tabIndex)
		{
			Toolbox.ToolsListBox.Items.Clear();
			Toolbox.ToolsListBox.Items.Add(pointer);
			if (Toolbox.Tabs.Count <= 0)
				return;

			ToolboxTab toolboxTab = Toolbox.Tabs[tabIndex];
			ToolboxItemCollection toolboxItems = toolboxTab.ToolboxItems;

			foreach (ToolboxItem toolboxItem in toolboxItems)
			{
				Type type = toolboxItem.Type;
				System.Drawing.Design.ToolboxItem tbi = new System.Drawing.Design.ToolboxItem(type);
				System.Drawing.ToolboxBitmapAttribute tba = TypeDescriptor.GetAttributes(type)[typeof(System.Drawing.ToolboxBitmapAttribute)] as System.Drawing.ToolboxBitmapAttribute;

				if (tba != null)
				{
					tbi.Bitmap = (System.Drawing.Bitmap)tba.GetImage(type);
				}

				Toolbox.ToolsListBox.Items.Add(tbi);
			}
		}
		private void button_Click(object sender, EventArgs e)
		{
			Button button = sender as Button;

			if (button == null)
				return;

			int index = (int)button.Tag;

			if (button.Dock == DockStyle.Top)
			{
				for (int i = index + 1; i < Toolbox.TabPageArray.Length; i++)
					Toolbox.TabPageArray[i].Dock = DockStyle.Bottom;
			}
			else
			{
				for (int i = 0; i <= index; i++)
					Toolbox.TabPageArray[i].Dock = DockStyle.Top;
			}

			Toolbox.ToolsListBox.Location = new System.Drawing.Point(0, (index + 2) * 20);
			UpdateToolboxItems(index);
		}
		private void Toolbox_SizeChanged(object sender, EventArgs e)
		{
			Toolbox.ToolsListBox.Size = new System.Drawing.Size(Toolbox.Width, Toolbox.Height - (Toolbox.Tabs.Count + 1) * 20);
		}
	}// class
}// namespace
