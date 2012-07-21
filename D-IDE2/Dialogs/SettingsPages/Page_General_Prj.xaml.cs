using System;
using System.Windows;
using D_IDE.Core;

namespace D_IDE.Dialogs.SettingsPages
{
	/// <summary>
	/// Interaktionslogik für Page_General.xaml
	/// </summary>
	public partial class Page_General_Prj : AbstractProjectSettingsPage
	{
		public Project Project { get; protected set; }
		public Page_General_Prj()
		{
			InitializeComponent();
		}

		public override bool ApplyChanges(Project prj)
		{
			prj.Name = Text_ProjectName.Text;

			if (!string.IsNullOrWhiteSpace(Text_OutputFile.Text) || Text_OutputFile.Text != prj.OutputFile)
				prj.OutputFile = Text_OutputFile.Text;
			
			try
			{
				prj.Version.Major = Convert.ToInt32(Text_Major.Text);
				prj.Version.Minor = Convert.ToInt32(Text_Minor.Text);
				prj.Version.Build = Convert.ToInt32(Text_Build.Text);
				prj.Version.Revision = Convert.ToInt32(Text_Revision.Text);
			}
			catch (Exception ex) { MessageBox.Show(ex.ToString()); return false; }
			prj.AutoIncrementBuildNumber = Check_AutoIncrBuild.IsChecked.Value;

			prj.ExecutingArguments = textBox_ExecArgs.Text;
			return true;
		}
		
		public override string SettingCategoryName
		{
			get { return "General"; }
		}

		public override void LoadCurrent(Project prj)
		{
			Project = prj;
			Text_BaseDir.Text = prj.BaseDirectory;
			Text_ProjectName.Text = prj.Name;
			Text_OutputFile.Text = prj.OutputFile;

			Text_Major.Text = prj.Version.Major.ToString();
			Text_Minor.Text = prj.Version.Minor.ToString();
			Text_Build.Text = prj.Version.Build.ToString();
			Text_Revision.Text = prj.Version.Revision.ToString();
			Check_AutoIncrBuild.IsChecked = prj.AutoIncrementBuildNumber;

			textBox_ExecArgs.Text = prj.ExecutingArguments;
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			Project.OutputFile = null;
			Text_OutputFile.Text = Project.OutputFile;
		}
	}
}
