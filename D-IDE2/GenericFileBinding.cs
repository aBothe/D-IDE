using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Windows.Media.Imaging;

namespace D_IDE
{
	/// <summary>
	/// A binding that manages all generic file types
	/// </summary>
	internal class GenericFileBinding:AbstractLanguageBinding
	{
		public GenericFileBinding()
		{
			var exts=new string[]{".txt"};
			_moduletypes.Add(new FileTemplate() { 
				Name="Text file",
				Description="Empty text file",
				Extensions=exts,
				DefaultFilePrefix="TextFile",

				LargeImage=CommonIcons.txt32,
				SmallImage=CommonIcons.txt16
			});
		}

		public override string LanguageName{get { return "General"; }}
		public override object LanguageIcon	{get { return new BitmapImage(new Uri("../Resources/file.png",UriKind.Relative)); }		}
		
		List<FileTemplate> _moduletypes = new List<FileTemplate>();
		public override FileTemplate[] ModuleTemplates { get { return _moduletypes.ToArray(); } }
		
		public override bool ProjectsSupported{get { return false; }}
		public override bool CanUseDebugging { get { return false; } }
		public override bool CanUseCodeCompletion { get { return false; } }
		public override bool CanBuild { get { return false; } }
		public override bool CanBuildToSingleModule { get { return false; } }


		public override object SmallProjectIcon		{			get { throw new NotImplementedException(); }		}
		public override object LargeProjectIcon		{			get { throw new NotImplementedException(); }		}
		public override FileTemplate[] ProjectTemplates		{			get { throw new NotImplementedException(); }		}
		public override Project CreateEmptyProject(FileTemplate ProjectType)		{			throw new NotImplementedException();		}
		public override Project OpenProject(Solution sln,string FileName)		{			throw new NotImplementedException();		}
		public override Parser.Core.ILanguage Language { get { throw new NotImplementedException(); } }
		public override bool BuildProject(Project Project)		{			throw new NotImplementedException();		}
		public override BuildError[] BuildSingleModule(string FileName)		{			throw new NotImplementedException();		}
		public override string BuildSymbolValueString(Parser.Core.AbstractSyntaxTree ModuleTree, uint ScopedSrcLine, DebugEngineWrapper.DebugScopedSymbol sym)		{			throw new NotImplementedException();		}

		public override void SaveSettings(string SuggestedFileName)		{			throw new NotImplementedException();		}
		public override void LoadSettings(string SuggestedFileName)		{			throw new NotImplementedException();		}
		public override bool CanUseSettings		{			get { return false; }		}
		public override AbstractSettingsPage SettingsPage
		{
			get { throw new NotImplementedException(); }
		}
	}
}
