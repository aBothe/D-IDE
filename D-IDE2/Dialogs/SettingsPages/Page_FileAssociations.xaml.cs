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
using BrendanGrant.Helpers.FileAssociation;
using System.Security;

namespace D_IDE.Dialogs.SettingsPages
{
	/// <summary>
	/// Interaktionslogik für Page_General.xaml
	/// </summary>
	public partial class Page_FileAssociations :AbstractSettingsPage
	{
		public Page_FileAssociations()
		{
			InitializeComponent();
			LoadCurrent();
		}

		public override bool ApplyChanges()
		{
			
			return true;
		}
		
		public override string SettingCategoryName
		{
			get { return "File Associations"; }
		}

		public override void LoadCurrent()
		{
			var exts = new List<string>();
			var l = new List<FileAssocListItem>();

			exts.Add(".idesln");

			foreach (var b in LanguageLoader.Bindings)
			{
				if (b.ProjectsSupported)
					foreach (var pt in b.ProjectTemplates)
						foreach (var ext in pt.Extensions)
							if (!exts.Contains(ext))
								exts.Add(ext);

				foreach (var pt in b.ModuleTemplates)
					foreach (var ext in pt.Extensions)
						if (!exts.Contains(ext))
							exts.Add(ext);
			}

			foreach (var ext in exts)
				l.Add(new FileAssocListItem() { Extension=ext});

			List_FileExtensionAssociations.ItemsSource = l;
		}
	}

	internal class FileAssocListItem
	{
		//public FileTemplate Template { get; set; }
		public string Extension
		{
			get;
			set;
		}

		public string Description
		{ get { return Extension; } }
		/*{
			get {
				string ret = "";

				foreach (var ext in Template.Extensions)
					ret += ext+";";

				return ret.TrimEnd(';');
			}
		}*/

		public const string D_IDE_RegistryProgId = "D-IDE";

		public bool IsAssociated
		{
			get
			{
				return AssociationManager.IsAssociated(D_IDE_RegistryProgId, Extension);
			}

			set
			{
				try
				{
					if (value)
						AssociationManager.Associate(D_IDE_RegistryProgId, System.Reflection.Assembly.GetEntryAssembly().Location, Extension);
					else
						AssociationManager.RemoveAssociation(Extension);
				}
				catch (SecurityException)
				{
					MessageBox.Show("Please restart D-IDE in admin mode!","Insufficient access rights");
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
			}
		}
	}
}
