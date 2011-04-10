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
	/// Interaktionslogik für Page_Editing.xaml
	/// </summary>
	public partial class Page_Editing : AbstractSettingsPage
	{
		public Page_Editing()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override string SettingCategoryName
		{
			get
			{
				return "Editing";
			}
		}

		public override void RestoreDefaults()
		{
			CommonEditorSettings.Instance.RestoreDefaults();
		}

		public override bool ApplyChanges()
		{
			var ces=CommonEditorSettings.Instance;
			ces.FontFamily=comboBox_FontFamily.SelectedItem as FontFamily;
			ces.Typeface = comboBox_FontStyle.SelectedItem as FamilyTypeface;
			ces.FontSize = fontSizeSlider.Value;

			ces.AssignAllOpenEditors();

			return true;
		}

		public override void LoadCurrent()
		{
			var ces=CommonEditorSettings.Instance;
			comboBox_FontFamily.SelectedItem = ces.FontFamily;
			comboBox_FontStyle.SelectedItem = ces.Typeface;
			fontSizeSlider.Value = ces.FontSize;
		}

		private void comboBox_FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (comboBox_FontStyle!=null && comboBox_FontStyle.Items.Count > 0)
				comboBox_FontStyle.SelectedIndex = 0;
		}
	}
}
