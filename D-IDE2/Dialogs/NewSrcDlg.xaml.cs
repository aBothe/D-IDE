using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using D_IDE.Core;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für NewProjectDlg.xaml
	/// </summary>
	public partial class NewSrcDlg : Window, INotifyPropertyChanged
	{
		#region Properties
		string _FileName;
		FileTemplate _FileType;

		public string FileName 
		{ 
			get { return _FileName; }
			set	{
				_FileName = value;
				PropChanged("CreationAllowed");
			}
		}

		public AbstractLanguageBinding SelectedLanguageBinding		{get {return List_Languages.SelectedItem as AbstractLanguageBinding;}	}
		public FileTemplate SelectedFileType		{			
			get { 
				return _FileType; 
			}
			set
			{
                if (value == null) return;
				if (SelectedFileType != null)
				{
					string defExt = (SelectedFileType.Extensions!=null&& SelectedFileType.Extensions.Length>0)? SelectedFileType.Extensions[0]:"";
					string DummyName = SelectedFileType.DefaultFilePrefix + "1"+defExt;

					if (String.IsNullOrEmpty(FileName) || FileName == DummyName)
					{
						FileName = value.DefaultFilePrefix + "1"+
							((value.Extensions != null && value.Extensions.Length > 0) ? value.Extensions[0] : "");
						PropChanged("FileName");
					}
				}
				_FileType = value;
			}
		}



		public object Languages		{			get			{				return LanguageLoader.Bindings;			}		}
		public object FileTypes
		{
			get
			{
				var o = SelectedLanguageBinding;
				if (o != null)
					return o.ModuleTemplates;
				return null;
			}
		}
		
		#endregion

		public NewSrcDlg()
		{
			DataContext = this;
			InitializeComponent();

			if (List_Languages.Items.Count > 0)
				List_Languages.SelectedIndex = 0;

			if (List_FileTypes.Items.Count > 0)
			{
				_FileType = List_FileTypes.Items[0] as FileTemplate;
				List_FileTypes.SelectedIndex = 0;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void PropChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		private void View_Languages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			PropChanged("FileTypes");

			if (List_FileTypes.Items.Count > 0)
			{
				SelectedFileType = List_FileTypes.Items[0] as FileTemplate;
				//List_FileTypes.SelectedIndex = 0;
			}
		}

		/// <summary>
		/// Validates all input fields and returns true if everything's ok
		/// </summary>
		public bool CreationAllowed
		{
			get { 
				return 
					SelectedLanguageBinding!=null &&
					SelectedFileType!=null&&
					!String.IsNullOrEmpty(FileName) &&
					// Primitive file name validation
					FileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars())<0;
			}
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

        private void List_Languages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CreationAllowed)
                DialogResult = true;
        }
	}
}
