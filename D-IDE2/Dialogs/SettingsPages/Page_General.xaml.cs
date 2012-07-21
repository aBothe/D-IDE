using System.Collections.Generic;
using System.Windows;
using D_IDE.Core;

namespace D_IDE.Dialogs.SettingsPages
{
	/// <summary>
	/// Interaktionslogik für Page_General.xaml
	/// </summary>
	public partial class Page_General : AbstractSettingsPage
	{
		public Page_General()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			GlobalProperties.AllowMultipleProgramInstances = cb_MultipleInstances.IsChecked.Value;
			GlobalProperties.Instance.WatchForUpdates = cb_Updates.IsChecked.Value;
			GlobalProperties.Instance.ShowStartPage = cb_StartPage.IsChecked.Value;
			GlobalProperties.Instance.RetrieveNews = cb_RetrieveBlog.IsChecked.Value;
			IDEInterface.StoreSettingsAtUserFiles = cb_UserSpecSettings.IsChecked.Value;

			GlobalProperties.Instance.DefaultProjectDirectory = tb_PrjRepo.Text;
			GlobalProperties.Instance.OpenLastPrj = cb_OpenLastSln.IsChecked.Value;
			GlobalProperties.Instance.OpenLastFiles = cb_OpenLastFiles.IsChecked.Value;
			GlobalProperties.Instance.ShowSpeedInfo = cb_DisplaySpeedInfo.IsChecked.Value;
			return true;
		}
		
		public override string SettingCategoryName
		{
			get { return "General"; }
		}

		public override void LoadCurrent()
		{
			cb_MultipleInstances.IsChecked = GlobalProperties.AllowMultipleProgramInstances;
			cb_Updates.IsChecked = GlobalProperties.Instance.WatchForUpdates;
			cb_StartPage.IsChecked = GlobalProperties.Instance.ShowStartPage;
			cb_RetrieveBlog.IsChecked = GlobalProperties.Instance.RetrieveNews;
			cb_UserSpecSettings.IsChecked = IDEInterface.StoreSettingsAtUserFiles;

			tb_PrjRepo.Text = GlobalProperties.Instance.DefaultProjectDirectory;
			cb_OpenLastSln.IsChecked = GlobalProperties.Instance.OpenLastPrj;
			cb_OpenLastFiles.IsChecked = GlobalProperties.Instance.OpenLastFiles;
			cb_DisplaySpeedInfo.IsChecked = GlobalProperties.Instance.ShowSpeedInfo;
		}

		private void bt_Search_PrjRepo_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new System.Windows.Forms.FolderBrowserDialog();
			dlg.ShowNewFolderButton = true;
			dlg.SelectedPath = tb_PrjRepo.Text;
			
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				tb_PrjRepo.Text = dlg.SelectedPath;
		}

		Page_FileAssociations fassoc = new Page_FileAssociations();
		public override IEnumerable<AbstractSettingsPage> SubCategories
		{
			get
			{
				yield return fassoc;
			}
		}

		private void button_ShowCfgDir_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer",IDEInterface.ConfigDirectory);
		}
	}
}
