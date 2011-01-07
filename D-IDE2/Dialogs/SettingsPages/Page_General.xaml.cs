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
			LoadCurrent();
		}

		public override void ApplyChanges()
		{
			GlobalProperties.AllowMultipleProgramInstances = cb_MultipleInstances.IsChecked.Value;
			GlobalProperties.Current.WatchForUpdates = cb_Updates.IsChecked.Value;
			GlobalProperties.Current.ShowStartPage = cb_StartPage.IsChecked.Value;
			GlobalProperties.Current.RetrieveNews = cb_RetrieveBlog.IsChecked.Value;
			IDEInterface.StoreSettingsAtUserFiles = cb_UserSpecSettings.IsChecked.Value;

			GlobalProperties.Current.DefaultProjectDirectory = tb_PrjRepo.Text;
			GlobalProperties.Current.OpenLastPrj = cb_OpenLastSln.IsChecked.Value;
			GlobalProperties.Current.OpenLastFiles = cb_OpenLastFiles.IsChecked.Value;
		}
		
		public override string SettingCategory
		{
			get { return "General"; }
		}

		public override void LoadCurrent()
		{
			cb_MultipleInstances.IsChecked = GlobalProperties.AllowMultipleProgramInstances;
			cb_Updates.IsChecked = GlobalProperties.Current.WatchForUpdates;
			cb_StartPage.IsChecked = GlobalProperties.Current.ShowStartPage;
			cb_RetrieveBlog.IsChecked = GlobalProperties.Current.RetrieveNews;
			cb_UserSpecSettings.IsChecked = IDEInterface.StoreSettingsAtUserFiles;

			tb_PrjRepo.Text = GlobalProperties.Current.DefaultProjectDirectory;
			cb_OpenLastSln.IsChecked = GlobalProperties.Current.OpenLastPrj;
			cb_OpenLastFiles.IsChecked = GlobalProperties.Current.OpenLastFiles;
		}

		private void bt_Search_PrjRepo_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new System.Windows.Forms.FolderBrowserDialog();
			dlg.ShowNewFolderButton = true;
			dlg.SelectedPath = tb_PrjRepo.Text;
			
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				tb_PrjRepo.Text = dlg.SelectedPath;
		}
	}
}
