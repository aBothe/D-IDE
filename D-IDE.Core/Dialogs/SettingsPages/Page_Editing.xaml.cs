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

		public override string SettingCategory
		{
			get
			{
				return "Editing";
			}
		}

		public override void RestoreDefaults()
		{
			
		}

		public override void ApplyChanges()
		{
			
		}

		public override void LoadCurrent()
		{

		}
	}
}
