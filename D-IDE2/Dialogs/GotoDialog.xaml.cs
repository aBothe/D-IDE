using System;
using System.Windows;
using System.Windows.Input;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für GotoDialog.xaml
	/// </summary>
	public partial class GotoDialog : Window
	{
		public GotoDialog()
		{
			InitializeComponent();

			textBox1.Focus();
		}

		private void textBox1_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!char.IsDigit(e.Text[0]))
				e.Handled = true;
		}

		public int EnteredNumber
		{
			get { return Convert.ToInt32( textBox1.Text); }
			set { textBox1.Text = value.ToString(); }
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			DialogResult =!string.IsNullOrEmpty(textBox1.Text);
			Close();
		}
	}
}
