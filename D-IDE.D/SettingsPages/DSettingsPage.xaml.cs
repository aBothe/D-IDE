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
		}

		public override bool ApplyChanges()
		{
			return true;
		}

		public override void LoadCurrent()
		{
			
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
