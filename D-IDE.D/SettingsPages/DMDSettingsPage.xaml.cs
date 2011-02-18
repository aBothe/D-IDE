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
		{/*
			if (!Directory.Exists(textBox_BaseDirectory.Text))
				return false;
			*/
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

		#region Browse buttons
		private void buttonBaseDirBrowse_Click(object sender, RoutedEventArgs e)
		{
			var od = new System.Windows.Forms.FolderBrowserDialog();
			od.SelectedPath = textBox_BaseDirectory.Text;
			if (od.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				textBox_BaseDirectory.Text = od.SelectedPath;
		}

		void ShowOpenExeDlg(TextBox tb)
		{
			var dlg = new OpenFileDialog();
			dlg.FileName = tb.Text;
			dlg.Filter = "Executables (*.exe;*.com)|*.exe;*.com";

			if (dlg.ShowDialog().Value)
				tb.Text = dlg.FileName;
		}

		private void buttonSrcCompBrowse_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_SrcCompiler);
		}

		private void button3_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_Win32Linker);
		}

		private void button4_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_ConsoleLinker);
		}

		private void button5_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_DllLinker);
		}

		private void button6_Click(object sender, RoutedEventArgs e)
		{
			ShowOpenExeDlg(textBox_LibLinker);
		}
		#endregion
	}
}
