using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für FeedbackDialog.xaml
	/// </summary>
	public partial class FeedbackDialog : Window
	{
		const string feedbackUrl = "http://d-ide.sourceforge.net/contrib_feedback.php";

		public FeedbackDialog()
		{
			InitializeComponent();

			input_Message_TextChanged(this, null);
		}

		private void input_Message_TextChanged(object sender, TextChangedEventArgs e)
		{
			textBlock_CharCount.Text = input_Message.Text.Length.ToString() + "/" + input_Message.MaxLength + " chars";
		}

		private void button_Send_Click(object sender, RoutedEventArgs e)
		{
			var wc = new WebClient();

			var mail = input_MailAddress.Text;
			var message = input_Message.Text.Trim();
			var versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3);


			// Check mail address
			if(!string.IsNullOrWhiteSpace(mail) && !Regex.IsMatch(mail,@"^[\w\.\-]+@[a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]{1,})*(\.[a-zA-Z]{2,3}){1,2}$"))
			{
				MessageBox.Show("Please leave address field empty or enter a valid e-mail address","Validation error");
				return;
			}

			// Null-check of the entered message
			if(string.IsNullOrEmpty(message))
			{
				MessageBox.Show("Please enter a message","Validation error");
				return;
			}

			// Upload the form data
			var answer=wc.UploadValues(feedbackUrl, "POST", new System.Collections.Specialized.NameValueCollection {
				{"mail", mail},
				{"message", message},
				{"version",versionString}
			});

			var answerString = Encoding.ASCII.GetString(answer);

			// Check answer string
			if (answerString.Length > 0)
				MessageBox.Show(answerString, "Error sending feedback", MessageBoxButton.OK, MessageBoxImage.Error);
			else
			{
				MessageBox.Show("Thank you!","Message sent",MessageBoxButton.OK,MessageBoxImage.Information);
				Close();
			}
		}

		private void button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (input_Message.Text.Length > 0 && 
				MessageBox.Show(
					"Discard entered feedback message?", 
					"Discard feedback", 
					MessageBoxButton.YesNo, 
					MessageBoxImage.Question, 
					MessageBoxResult.Yes) ==
				MessageBoxResult.No)
				return;

			Close();
		}
	}
}
