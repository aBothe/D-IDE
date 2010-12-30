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
using System.ComponentModel;
using Parser.Core;
using D_IDE.Core;
using System.Collections;
using System.Reflection;
using Microsoft.Win32;
using System.IO;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für NewProjectDlg.xaml
	/// </summary>
	public partial class NewProjectDlg : Window, INotifyPropertyChanged
	{
		#region Properties
		string _PrjName;
		FileTemplate _FileType;

		public string ProjectName 
		{ 
			get { return _PrjName; }
			set	{
				if (TextBox_SolutionName != null&& String.IsNullOrEmpty(SolutionName) || SolutionName == _PrjName)
					SolutionName = value;

				_PrjName = value;
				PropChanged("CreationAllowed");
			}
		}
		public string ProjectDir
		{
			get { return TextBox_ProjectDir.Text; }
			set { TextBox_ProjectDir.Text = value; PropChanged("CreationAllowed"); }
		}
		public bool CreateProjectDir
		{
			get { return Check_CreateSolutionDir.IsChecked.Value; }
			set { Check_CreateSolutionDir.IsChecked = value; }
		}
		public string SolutionName
		{
			get { return TextBox_SolutionName.Text; }
			set { TextBox_SolutionName.Text = value; PropChanged("CreationAllowed"); }
		}
		public bool AddToCurrentSolution
		{
			get { return ComboBox_CreateSolution.SelectedIndex == 1; }
			set { ComboBox_CreateSolution.SelectedIndex = value ? 1 : 0; }
		}

		public AbstractLanguageBinding SelectedLanguageBinding		
		{
			get {
				return List_Languages.SelectedItem as AbstractLanguageBinding;
			}	
		}
		public FileTemplate SelectedProjectType		{			
			get { 
				return _FileType; 
			}
			set
			{
				if (SelectedProjectType != null)
				{
					string DummyName = SelectedProjectType.DefaultFilePrefix + "1";

					if (String.IsNullOrEmpty(ProjectName) || ProjectName == DummyName)
					{
						ProjectName = value.DefaultFilePrefix + "1";
						PropChanged("ProjectName");
					}
				}
				_FileType = value;
			}
		}

		public enum DialogMode
		{
			CreateNew,
			Add
		}

		public DialogMode NewProjectDlgMode
		{
			set {
				// Create only
				if (value.HasFlag(DialogMode.CreateNew) && !value.HasFlag(DialogMode.Add))
				{
					ComboBox_CreateSolution.SelectedIndex = 0;
					ComboBox_CreateSolution.IsEnabled = false;
					TextBox_SolutionName.IsEnabled = true;
				}
				// Add only
				else if (!value.HasFlag(DialogMode.CreateNew) && value.HasFlag(DialogMode.Add))
				{
					ComboBox_CreateSolution.SelectedIndex = 1;
					ComboBox_CreateSolution.IsEnabled = false;
					TextBox_SolutionName.IsEnabled = false;
				}
				// Both
				else
				{
					ComboBox_CreateSolution.IsEnabled = true;
					TextBox_SolutionName.IsEnabled = true;
				}
				PropChanged("CreationAllowed");
			}
		}

		public object Languages		
		{			
			get			
			{	
				// Only show languages that support and have projects
				return from b in LanguageLoader.Bindings 
					   where b.ProjectsSupported && b.ProjectTemplates.Length>0
					   select b;			
			}		
		}
		public object FileTypes
		{
			get
			{
				var o = SelectedLanguageBinding;
				if (o != null)
					return o.ProjectTemplates;
				return null;
			}
		}
		
		#endregion

		public NewProjectDlg(DialogMode NewProjectDialogMode)
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

			NewProjectDlgMode = NewProjectDialogMode;
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
				SelectedProjectType = List_FileTypes.Items[0] as FileTemplate;
				List_FileTypes.SelectedIndex = 0;
			}
		}

		private void ExploreProjectDir(object sender, RoutedEventArgs e)
		{
			var fd = new System.Windows.Forms.FolderBrowserDialog();
			fd.SelectedPath = ProjectDir;

			if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				ProjectDir = fd.SelectedPath;
		}

		private void ComboBox_CreateSolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TextBox_SolutionName.IsEnabled = !AddToCurrentSolution;
		}

		/// <summary>
		/// Validates all input fields and returns true if everything's ok
		/// </summary>
		public bool CreationAllowed
		{
			get { 
				return 
					SelectedLanguageBinding!=null &&
					SelectedProjectType!=null&&
					!String.IsNullOrEmpty(ProjectName) && 
					Directory.Exists(ProjectDir) && 
					(AddToCurrentSolution?true:!String.IsNullOrEmpty(SolutionName));
			}
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

        private void List_FileTypes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(CreationAllowed)
                DialogResult = true;
        }
	}
}
