using System.Collections.Generic;
using D_IDE.Core;

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DSettingsPage.xaml
	/// </summary>
	public partial class DSettingsPage : AbstractSettingsPage
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
			sett.UseSemanticErrorHighlighting = checkBox_EnableSemanticErrorHighlighting.IsChecked.Value;
			sett.UseSemanticHighlighting = checkBox_UseSemanticHighlighting.IsChecked.Value;
			sett.EnableSmartIndentation = checkBox_SmartIndentation.IsChecked.Value;
			sett.CompletionOptions.ShowUFCSItems = checkBox_EnableUFCSCompletion.IsChecked.Value;

			return true;
		}

		public override void LoadCurrent()
		{
			var sett = DSettings.Instance;

			checkBox_UseCC.IsChecked=sett.UseCodeCompletion;
			checkBox_EnableBracketHighlighting.IsChecked = sett.EnableMatchingBracketHighlighting;
			checkBox_MethodInsight.IsChecked = sett.UseMethodInsight;
			checkBox_CodeInsertionOnNonLetter.IsChecked = sett.ForceCodeCompetionPopupCommit;
			checkBox_EnableSemanticErrorHighlighting.IsChecked = sett.UseSemanticErrorHighlighting;
			checkBox_UseSemanticHighlighting.IsChecked = sett.UseSemanticHighlighting;
			checkBox_SmartIndentation.IsChecked = sett.EnableSmartIndentation;
			checkBox_EnableUFCSCompletion.IsChecked = sett.CompletionOptions.ShowUFCSItems;
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
