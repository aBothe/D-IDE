using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using D_IDE.Core;
using D_IDE.D.CodeCompletion;

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DSettingsPage.xaml
	/// </summary>
	public partial class GlobalParseCachePage : AbstractSettingsPage
	{
		public DMDConfig cfg { get; set; }

		readonly ObservableCollection<ASTCollection> Dirs = new ObservableCollection<ASTCollection>();
		public GlobalParseCachePage(DMDConfig Config)
		{
			InitializeComponent();

			this.cfg = Config;

			list_Dirs.ItemsSource = Dirs;
			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			cfg.ASTCache.ParsedGlobalDictionaries.Clear();
			cfg.ASTCache.ParsedGlobalDictionaries.AddRange(Dirs);

			return true;
		}

		public override void LoadCurrent()
		{
			foreach (var pd in cfg.ASTCache)
				Dirs.Add(pd);
		}

		public override string SettingCategoryName
		{
			get
			{
				return "Library Paths";
			}
		}

		private void button_AddDir_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new System.Windows.Forms.FolderBrowserDialog();
			if (dlg.ShowDialog()==DialogResult.OK)
			{
				var ac = new ASTCollection(dlg.SelectedPath);

				if (System.Windows.MessageBox.Show("Parse " + ac.BaseDirectory + " ?", "Add new library path", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					ac.UpdateFromBaseDirectory();

				Dirs.Add(ac);
			}
		}

		private void button_DelDir_Click(object sender, RoutedEventArgs e)
		{
			Dirs.Remove(list_Dirs.SelectedItem as ASTCollection);
		}

		private void button_Reparse_Click(object sender, RoutedEventArgs e)
		{
			if (list_Dirs.SelectedIndex < 0)
				return;

			(list_Dirs.SelectedItem as ASTCollection).UpdateFromBaseDirectory();
		}
	}
}
