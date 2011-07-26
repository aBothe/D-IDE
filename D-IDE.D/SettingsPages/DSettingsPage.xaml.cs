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

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DSettingsPage.xaml
	/// </summary>
	public partial class DSettingsPage : D_IDE.Core.AbstractSettingsPage
	{
		public DSettingsPage()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			var sett = DSettings.Instance;

			sett.UseCodeCompletion = checkBox_UseCC.IsChecked.Value;
			sett.EnableMatchingBracketHighlighting = checkBox_EnableBracketHighlighting.IsChecked.Value;
			sett.UseMethodInsight = checkBox_MethodInsight.IsChecked.Value;
			sett.ForceCodeCompetionPopupCommit = checkBox_CodeInsertionOnNonLetter.IsChecked.Value;

			return true;
		}

		public override void LoadCurrent()
		{
			var sett = DSettings.Instance;

			checkBox_UseCC.IsChecked=sett.UseCodeCompletion;
			checkBox_EnableBracketHighlighting.IsChecked = sett.EnableMatchingBracketHighlighting;
			checkBox_MethodInsight.IsChecked = sett.UseMethodInsight;
			checkBox_CodeInsertionOnNonLetter.IsChecked = sett.ForceCodeCompetionPopupCommit;
		}

		public override string SettingCategoryName
		{
			get
			{
				return "D Settings";
			}
		}

		List<AbstractSettingsPage> subpages = new List<AbstractSettingsPage>();
		public override IEnumerable< AbstractSettingsPage> SubCategories
		{
			get
			{
				if (subpages.Count < 1)
				{
					subpages.Add( new DMDSettingsPage(DSettings.Instance.dmd1));
					subpages.Add( new DMDSettingsPage(DSettings.Instance.dmd2));
				}
				return subpages;
			}
		}
	}
}
