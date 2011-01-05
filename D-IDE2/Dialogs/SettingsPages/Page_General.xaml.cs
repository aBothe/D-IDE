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
	public partial class Page_General :AbstractSettingsPage
	{
		public Page_General()
		{
			InitializeComponent();
		}

		public override void ApplyChanges()
		{
			throw new NotImplementedException();
		}
		
		public override string SettingCategory
		{
			get { return "General"; }
		}

		public override AbstractSettingsPage[] SubCategories
		{
			get { return null; }
		}
	}
}
