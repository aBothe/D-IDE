using System;
using System.Windows;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für CrashDialog.xaml
	/// </summary>
	public partial class CrashDialog : Window
	{
		public Exception Exception{get;protected set;}

		public void Update()
		{
			textBox_errorMsg.Text=Exception.Message+" ("+Exception.Source+")\r\n\r\n"+Exception.StackTrace;
		}

		public CrashDialog(Exception exception)
		{
			InitializeComponent();

			Exception=exception;
			Update();
		}

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			IDELogger.SendErrorReport(textBox_errorMsg.Text, text_userComment.Text);
			Close();
		}
	}
}
