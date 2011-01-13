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
	public partial class Page_Building : AbstractSettingsPage
	{
		public Page_Building()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override string SettingCategory
		{
			get
			{
				return "Building";
			}
		}

		public override void ApplyChanges()
		{
			GlobalProperties.Instance.DoAutoSaveOnBuilding = cb_SaveBeforeBuild.IsChecked.Value;
			GlobalProperties.Instance.DefaultBinariesPath = tb_DefBinPath.Text;
		}

		public override void LoadCurrent()
		{
			cb_SaveBeforeBuild.IsChecked = GlobalProperties.Instance.DoAutoSaveOnBuilding;
			tb_DefBinPath.Text = GlobalProperties.Instance.DefaultBinariesPath;
		}
	}
}
