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
using Microsoft.Win32;

namespace D_IDE.Dialogs.SettingsPages
{
	public partial class Page_Debugging : AbstractSettingsPage
	{
		public Page_Debugging()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override string SettingCategory
		{
			get
			{
				return "Debugging";
			}
		}

		public override void ApplyChanges()
		{
			GlobalProperties.Instance.VerboseDebugOutput = cb_VerboseDbgOutput.IsChecked.Value;
			GlobalProperties.Instance.SkipUnknownCode = cb_SkipUnresolvableCode.IsChecked.Value;
			GlobalProperties.Instance.ShowDebugConsole = cb_ShowDbgConsole.IsChecked.Value;

			GlobalProperties.Instance.UseExternalDebugger = cb_UseExtDebugger.IsChecked.Value;
			GlobalProperties.Instance.ExternalDebugger_Bin = tb_ExtDbg_Path.Text;
			GlobalProperties.Instance.ExternalDebugger_Arguments = tb_ExtDbg_Params.Text;
		}

		public override void LoadCurrent()
		{
			cb_VerboseDbgOutput.IsChecked = GlobalProperties.Instance.VerboseDebugOutput;
			cb_SkipUnresolvableCode.IsChecked = GlobalProperties.Instance.SkipUnknownCode;
			cb_ShowDbgConsole.IsChecked = GlobalProperties.Instance.ShowDebugConsole;

			cb_UseExtDebugger.IsChecked = GlobalProperties.Instance.UseExternalDebugger;
			tb_ExtDbg_Path.Text = GlobalProperties.Instance.ExternalDebugger_Bin;
			tb_ExtDbg_Params.Text = GlobalProperties.Instance.ExternalDebugger_Arguments;
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog();
			dlg.FileName = tb_ExtDbg_Path.Text;
			dlg.CheckFileExists = true;

			if (dlg.ShowDialog().Value)
				tb_ExtDbg_Path.Text = dlg.FileName;
		}

		private void cb_UseExtDebugger_Checked(object sender, RoutedEventArgs e)
		{
			tb_ExtDbg_Path.IsEnabled = tb_ExtDbg_Params.IsEnabled = cb_UseExtDebugger.IsChecked.Value;
		}
	}
}
