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
using System.Windows.Shapes;
using D_IDE.Dialogs.SettingsPages;
using D_IDE.Core;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für GlobalSettingsDlg.xaml
	/// </summary>
	public partial class GlobalSettingsDlg : Window
	{
		public readonly List<AbstractSettingsPage> SettingsPages = new List<AbstractSettingsPage>();
		
		public GlobalSettingsDlg()
		{
			InitializeComponent();

			BuildCategoryArray();
		}

		#region Category Tree
		public void BuildCategoryArray()
		{
			SettingsPages.Clear();

			SettingsPages.Add(new Page_General());
			SettingsPages.Add(new Page_Editing());
			SettingsPages.Add(new Page_Building());
			SettingsPages.Add(new Page_Debugging());

			foreach (var lang in from l in LanguageLoader.Bindings where l.CanUseSettings select l)
				if(lang.SettingsPage!=null)
					SettingsPages.Add(lang.SettingsPage);

			RefreshCategoryTree();
		}

		public void RefreshCategoryTree()
		{
			CategoryTree.BeginInit();

			CategoryTree.Items.Clear();

			foreach (var pg in SettingsPages)
				if(pg!=null)
					CategoryTree.Items.Add(_BuildCategoryNode(pg));

			// If nothing selected and page control empty, select first item
			if (PropPageHost.Content == null && CategoryTree.Items.Count > 0)
				SetSettingPage(CategoryTree.Items[0] as TreeViewItem);

			CategoryTree.EndInit();
		}

		public void SetSettingPage(TreeViewItem n)
		{
			PropPageHost.Content = n.Tag;
		}

		TreeViewItem _BuildCategoryNode(AbstractSettingsPage Page)
		{
			var ret = new TreeViewItem();
			ret.Tag = Page;
			ret.Header = Page.SettingCategoryName;

			var subCategories = Page.SubCategories;
			if(subCategories!=null && subCategories.Length>0)
				foreach(var sc in subCategories)
					ret.Items.Add(_BuildCategoryNode(sc));

			return ret;
		}

		private void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SetSettingPage(e.NewValue as TreeViewItem);
		}
		#endregion

		#region Settings logic
		private void buttonApply_Click(object sender, RoutedEventArgs e)
		{
			if(ApplySettings())
				DialogResult = true;
		}

		public bool ApplySettings()
		{
			foreach (var sp in SettingsPages)
				if (!ApplySettings(sp))
					return false;
			return true;
		}

		bool ApplySettings(AbstractSettingsPage p)
		{
			if (!p.ApplyChanges())
				return false;

			if (p.SubCategories != null && p.SubCategories.Length > 0)
				foreach (var ssp in p.SubCategories)
					if (!ApplySettings(ssp))
						return false;
			return true;
		}

		public void RestoreDefaults()
		{
			foreach (var sp in SettingsPages) RestoreDefaults(sp);
		}

		void RestoreDefaults(AbstractSettingsPage p)
		{
			p.RestoreDefaults();
			if (p.SubCategories != null && p.SubCategories.Length > 0)
				foreach (var ssp in p.SubCategories)
					RestoreDefaults(ssp);
		}
		
		private void buttonRestore_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure?", "Restoring defaults", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				RestoreDefaults();
			}
		}
		#endregion
	}
}
