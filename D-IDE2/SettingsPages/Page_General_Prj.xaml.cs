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
using System.Windows.Navigation;
using System.Windows.Shapes;
using D_IDE.Core;

namespace D_IDE.Dialogs.SettingsPages
{
	/// <summary>
	/// Interaktionslogik für Page_General.xaml
	/// </summary>
	public partial class Page_General_Prj :AbstractProjectSettingsPage
	{
		public Page_General_Prj()
		{
			InitializeComponent();
		}

		public override void ApplyChanges(Project prj)
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
			catch (Exception ex) { MessageBox.Show(ex.ToString()); }
			prj.AutoIncrementBuildNumber = Check_AutoIncrBuild.IsChecked.Value;
		}
		
		public override string SettingCategoryName
		{
			get { return "General"; }
		}

		public override void LoadCurrent(Project prj)
		{
			Text_BaseDir.Text = prj.BaseDirectory;
			Text_ProjectName.Text = prj.Name;
			Text_OutputFile.Text = prj.OutputFile;

			Text_Major.Text = prj.Version.Major.ToString();
			Text_Minor.Text = prj.Version.Minor.ToString();
			Text_Build.Text = prj.Version.Build.ToString();
			Text_Revision.Text = prj.Version.Revision.ToString();
			Check_AutoIncrBuild.IsChecked = prj.AutoIncrementBuildNumber;
		}
	}
}
