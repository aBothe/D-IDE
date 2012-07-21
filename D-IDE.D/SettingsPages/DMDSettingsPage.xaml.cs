using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using D_IDE.Core;

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DMDSettingsPage.xaml
	/// </summary>
	public partial class DMDSettingsPage : AbstractSettingsPage
	{
		DMDConfig Config;

		public DMDSettingsPage(DMDConfig Configuration)
		{
			InitializeComponent();
			this.Config = Configuration;
			List_DefaultLibs.ItemsSource = Libs;

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

			Config.DefaultLinkedLibraries.Clear();
			Config.DefaultLinkedLibraries.AddRange(Libs);

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

			Libs.Clear();
			foreach (var l in Config.DefaultLinkedLibraries)
				Libs.Add(l);

			if (Libs.Count > 0)
				List_DefaultLibs.SelectedIndex = 0;
		}

		public override string SettingCategoryName
		{
			get
			{
				return "dmd"+(int)Config.Version;
			}
		}

		List<AbstractSettingsPage> _subCats = new List<AbstractSettingsPage>();
		public override IEnumerable<Core.AbstractSettingsPage> SubCategories
		{
			get
			{
				if (_subCats.Count < 1)
				{
					_subCats.Add(new GlobalParseCachePage(this,Config));
				}

				return _subCats;
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

		#region Def Libs
		ObservableCollection<string> Libs = new ObservableCollection<string>();
		private void List_DefaultLibs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			text_CurLib.Text = List_DefaultLibs.SelectedValue as string;
		}

		private void button_AddLib_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(text_CurLib.Text))
				Libs.Add(text_CurLib.Text);
			text_CurLib.Text = "";
		}

		private void button_ApplyLib_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(text_CurLib.Text))
				Libs[List_DefaultLibs.SelectedIndex] = text_CurLib.Text;
		}

		/// <summary>
		/// Removes currently selected library from the list. Selects the following library so there's a library reference selected even after deletion.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_DeleteLib_Click(object sender, RoutedEventArgs e)
		{
			if(List_DefaultLibs.SelectedIndex>=0)
			{
				int i=List_DefaultLibs.SelectedIndex;
				Libs.RemoveAt(i);
				
				if(Libs.Count>i)
					List_DefaultLibs.SelectedIndex=i;
				else if(Libs.Count>0)
					List_DefaultLibs.SelectedIndex=Libs.Count;
			}
		}
		#endregion

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
