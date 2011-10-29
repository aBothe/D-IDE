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
using System.IO;
using Microsoft.Win32;
using D_IDE.Core;
using System.Collections.ObjectModel;

namespace D_IDE.ResourceFiles
{
	public partial class ResSettingsPage : AbstractSettingsPage
	{
		public ResSettingsPage()
		{
			InitializeComponent();

			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			var Config = ResConfig.Instance;
			Config.ResourceCompilerPath = textBox_ResCmpPath.Text;
			Config.ResourceCompilerArguments = textBox_CmpArgs.Text;

			return true;
		}

		public override void LoadCurrent()
		{
			var Config = ResConfig.Instance;
			textBox_ResCmpPath.Text = Config.ResourceCompilerPath;
			textBox_CmpArgs.Text = Config.ResourceCompilerArguments;
		}

		public override string SettingCategoryName
		{
			get
			{
				return "Resource Compiler";
			}
		}

		void ShowOpenExeDlg(TextBox tb)
		{
			var dlg = new OpenFileDialog();
			dlg.FileName = tb.Text;
			dlg.Filter = "Executables (*.exe;*.com)|*.exe;*.com";

			if (dlg.ShowDialog().Value)
				tb.Text = dlg.FileName;
		}

		private void buttonResCmpPath_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_ResCmpPath);
		}

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			textBox_CmpArgs.Text = ResConfig.DefaultArgumentString;
		}
	}
}
