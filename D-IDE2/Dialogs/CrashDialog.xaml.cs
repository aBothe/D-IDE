using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
