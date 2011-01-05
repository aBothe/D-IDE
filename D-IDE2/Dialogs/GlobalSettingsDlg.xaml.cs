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

			SettingsPages.Add( new Page_General());

			RefreshCategoryTree();
		}

		public void RefreshCategoryTree()
		{
			CategoryTree.BeginInit();

			CategoryTree.Items.Clear();

			foreach (var pg in SettingsPages)
				CategoryTree.Items.Add(_BuildCategoryNode( pg));

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
			ret.Header = Page.SettingCategory;

			ret.MouseDown += delegate(object sender, MouseButtonEventArgs e)
			{
				SetSettingPage(ret);
			};

			var subCategories = Page.SubCategories;
			if(subCategories!=null && subCategories.Length>0)
				foreach(var sc in subCategories)
					ret.Items.Add(_BuildCategoryNode(sc));

			return ret;
		}
	}
}
