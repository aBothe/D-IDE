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
	internal class GenericFileBinding:ILanguageBinding
	{
		public IDEInterface IDE
		{
			get
			{
				return IDEInterface.Current;
			}
			set{}
		}

		public GenericFileBinding()
		{
			var exts=new string[]{".txt"};
			_moduletypes.Add(new SourceFileType() { 
				Name="Text file",
				Description="Empty text file",
				Extensions=exts,
				DefaultFilePrefix="TextFile",
				LargeImage=new BitmapImage(new Uri("../Resources/txt32.png",UriKind.Relative)),
				SmallImage=new BitmapImage(new Uri("../Resources/txt16.png",UriKind.Relative))
			});

			_moduletypes.Add(new SourceFileType()
			{
				Name="a file",
				DefaultFilePrefix="afile"
			});
		}

		public string LanguageName
		{
			get { return "General"; }
		}

		public object LanguageIcon
		{
			get { return new BitmapImage(new Uri("../Resources/file.png",UriKind.Relative)); }
		}

		List<SourceFileType> _moduletypes = new List<SourceFileType>();
		public SourceFileType[] ModuleTypes
		{
			get { return _moduletypes.ToArray(); }
		}

		public SourceFileType[] ProjectTypes		{			get { throw new NotImplementedException(); }		}
		public bool ProjectsSupported{get { return false; }}
		public bool CanUseDebugging{get { return false; }}
		public bool CanUseCodeCompletion{get { return false; }}
		public bool CanBuild{			get { return false; }		}
		public bool CanBuildToSingleModule		{			get { return false; }		}
		public Parser.Core.ILanguage Language		{			get { return null; }		}
		public object GetNodeIcon(Parser.Core.INode Node)		{			return null;		}
		public IProject CreateEmptyProject(SourceFileType ProjectType)		{			return null;		}
		public IProject OpenProject(string FileName)		{			return null;		}
		public IModule CreateEmptyModule(SourceFileType FileType)		{			throw new NotImplementedException();}
		public IModule OpenModule(string FileName)		{			throw new NotImplementedException();		}
		public IDebugProvider DebugProvider		{			get { return null; }		}
	}
}
