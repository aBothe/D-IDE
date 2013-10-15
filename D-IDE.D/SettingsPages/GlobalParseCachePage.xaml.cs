using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using D_IDE.Core;
using D_Parser.Misc;

namespace D_IDE.D
{
	/// <summary>
	/// Interaktionslogik für DSettingsPage.xaml
	/// </summary>
	public partial class GlobalParseCachePage : AbstractSettingsPage
	{
		public DMDSettingsPage ParentPage { get; private set; }
		public DMDConfig cfg { get; private set; }

		public GlobalParseCachePage(DMDSettingsPage DMDPage,DMDConfig Config)
		{
			InitializeComponent();

			this.ParentPage = DMDPage;
			this.cfg = Config;

			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			var Dirs = importPathBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			// If current dir count != the new dir count
			bool cacheUpdateRequired = Dirs.Length != cfg.ImportDirectories.Count;

			// If there's a new directory in it
			if (!cacheUpdateRequired)
				foreach (var path in Dirs)
					if (!cfg.ImportDirectories.Contains(path))
					{
						cacheUpdateRequired = true;
						break;
					}

			if (cacheUpdateRequired)
			{
				Cursor = Cursors.Wait;
				cfg.ParsingFinished += finishedAnalysis;
				cfg.ImportDirectories.Clear();
				cfg.ImportDirectories.AddRange(Dirs);
				cfg.ReparseImportDirectories();
			}

			return true;
		}

		void finishedAnalysis()
		{
			cfg.ParsingFinished -= finishedAnalysis;
			Dispatcher.Invoke(new Action(() => Cursor = Cursors.Arrow));
		}

		public override void LoadCurrent()
		{
			importPathBox.Text = string.Join(Environment.NewLine,cfg.ImportDirectories);
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
				importPathBox.AppendText(Environment.NewLine + dlg.SelectedPath);
		}
	}
}
