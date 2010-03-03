using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace D_IDE
{
	public partial class InputDlg : Form
	{
		public InputDlg()
		{
			InitializeComponent();
			Description = "Enter input string here:";
		}

		public string Description
		{
			get { return DescriptionLabel.Text; }
			set { DescriptionLabel.Text = value; }
		}

		public uint MaxInputLength
		{
			get { return (uint)textBox1.MaxLength; }
			set { textBox1.MaxLength = (int)value; }
		}

		public string InputString
		{
			get { return textBox1.Text; }
			set { textBox1.Text = value; }
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (textBox1.Text == "" && textBox1.MaxLength > 0)
			{
				DialogResult = DialogResult.None;
				return;
			}
			DialogResult = DialogResult.OK;
		}
	}
}
