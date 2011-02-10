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

namespace D_IDE.Core.Dialogs
{
	/// <summary>
	/// Interaktionslogik für GlobalSettingsDlg.xaml
	/// </summary>
	public partial class ProjectSettingsDlg : Window
	{
		public readonly List<AbstractProjectSettingsPage> SettingsPages = new List<AbstractProjectSettingsPage>();
		public readonly Project Project;

		public ProjectSettingsDlg(Project Project)
		{
			InitializeComponent();

			this.Project = Project;

			BuildCategoryArray();
		}

		#region Category Tree
		public void BuildCategoryArray()
		{
			SettingsPages.Clear();

			SettingsPages.Add(new Page_General_Prj());

			var pgs = Project.LanguageSpecificProjectSettings;
			if (pgs != null && pgs.Length > 0)
				foreach (var p in pgs)
					SettingsPages.Add(p);

			foreach (var p in SettingsPages)
				p.LoadCurrent(Project);

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

		TreeViewItem _BuildCategoryNode(AbstractProjectSettingsPage Page)
		{
			var ret = new TreeViewItem();
			ret.Tag = Page;
			ret.Header = Page.SettingCategoryName;

			// There are no subcategories? -- Because we have enough tree space ;-)

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
			ApplySettings();
			DialogResult = true;
		}

		public void ApplySettings()
		{
			foreach (var sp in SettingsPages)
				sp.ApplyChanges(Project);
		}
		#endregion
	}
}
