using System.Collections.ObjectModel;
using System.Windows;
using D_IDE.Core;
using D_IDE.D.CodeCompletion;
using System.Windows.Input;
using System;

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DSettingsPage.xaml
	/// </summary>
	public partial class GlobalParseCachePage : AbstractSettingsPage
	{
		public DMDSettingsPage ParentPage { get; private set; }
		public DMDConfig cfg { get; private set; }

		readonly ObservableCollection<ASTCollection> Dirs = new ObservableCollection<ASTCollection>();
		public GlobalParseCachePage(DMDSettingsPage DMDPage,DMDConfig Config)
		{
			InitializeComponent();

			this.ParentPage = DMDPage;
			this.cfg = Config;

			list_Dirs.ItemsSource = Dirs;
			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			cfg.ASTCache.ParsedGlobalDictionaries.Clear();
			cfg.ASTCache.ParsedGlobalDictionaries.AddRange(Dirs);
			cfg.ASTCache.UpdateEditorParseCache();

			return true;
		}

		public override void LoadCurrent()
		{
			Dirs.Clear();
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
			if (dlg.ShowDialog()==System.Windows.Forms.DialogResult.OK)
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
			Cursor=Cursors.Wait;
			try
			{
				foreach (var i in list_Dirs.Items)
				{
					var astColl = (i as ASTCollection);
					astColl.UpdateFromBaseDirectory();
				}
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
			finally
			{
				Cursor = Cursors.Arrow;
			}
		}
	}
}
