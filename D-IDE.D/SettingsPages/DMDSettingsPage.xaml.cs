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

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DMDSettingsPage.xaml
	/// </summary>
	public partial class DMDSettingsPage : D_IDE.Core.AbstractSettingsPage
	{
		DMDConfig Config;

		public DMDSettingsPage(DMDConfig Configuration)
		{
			InitializeComponent();
			this.Config = Configuration;

			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			if (!Directory.Exists(textBox_BaseDirectory.Text))
				return false;

			Config.BaseDirectory = textBox_BaseDirectory.Text;

			Config.SoureCompiler = textBox_SrcCompiler.Text;
			Config.Win32ExeLinker = textBox_Win32Linker.Text;
			Config.ExeLinker = textBox_ConsoleLinker.Text;
			Config.DllLinker = textBox_DllLinker.Text;
			Config.LibLinker = textBox_LibLinker.Text;

			return true;
		}

		public override void LoadCurrent()
		{
			textBox_BaseDirectory.Text = Config.BaseDirectory;

			textBox_SrcCompiler.Text = Config.SoureCompiler;
			textBox_Win32Linker.Text = Config.Win32ExeLinker;
			textBox_ConsoleLinker.Text = Config.ExeLinker;
			textBox_DllLinker.Text = Config.DllLinker;
			textBox_LibLinker.Text = Config.LibLinker;
		}

		public override string SettingCategoryName
		{
			get
			{
				return "dmd"+(int)Config.Version;
			}
		}

		public void ShowBuildArgConfig(bool IsDebug)
		{
			var dlg = new BuildArgumentForm(Config.BuildArguments(IsDebug));
			dlg.ShowDialog();
		}

		private void button_DbgArgs_Click(object sender, RoutedEventArgs e)
		{
			ShowBuildArgConfig(true);
		}

		private void button_ResArgs_Click(object sender, RoutedEventArgs e)
		{
			ShowBuildArgConfig(false);
		}
	}
}
