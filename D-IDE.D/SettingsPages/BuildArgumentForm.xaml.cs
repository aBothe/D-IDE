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

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für BuildArgumentForm.xaml
	/// </summary>
	public partial class BuildArgumentForm :Window
	{
		DMDConfig.DBuildArguments buildArgs;
		
		public BuildArgumentForm(DMDConfig.DBuildArguments Arguments)
		{
			InitializeComponent();
			buildArgs = Arguments;

			LoadCurrent();
		}

		public void LoadCurrent()
		{
			Title = (buildArgs.IsDebug ? "Debug" : "Release") + " build arguments";

			Text_SrcCompiler.Text = buildArgs.SoureCompiler;
			Text_Win32Linker.Text = buildArgs.Win32ExeLinker;
			Text_ConsoleExe.Text = buildArgs.ExeLinker;
			Text_DllLinker.Text = buildArgs.DllLinker;
			Text_StaticLibLinker.Text = buildArgs.LibLinker;
		}

		public void Apply()
		{
			buildArgs.SoureCompiler = Text_SrcCompiler.Text;
			buildArgs.Win32ExeLinker = Text_Win32Linker.Text;
			buildArgs.ExeLinker = Text_ConsoleExe.Text;
			buildArgs.DllLinker = Text_DllLinker.Text;
			buildArgs.LibLinker = Text_StaticLibLinker.Text;
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			Apply();
			DialogResult = true;
		}

		private void button_Defaults_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("All custom settings will be lost. Continue?", "Reset build arguments", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.Yes)
			{
				buildArgs.Reset();
				LoadCurrent();
			}
		}
	}
}
