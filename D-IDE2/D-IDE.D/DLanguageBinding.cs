using System;
using System.Collections.Generic;
using D_IDE.Core;
using Parser.Core;
using D_Parser;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace D_IDE.D
{
	public class DLanguageBinding:ILanguageBinding
	{
		public IDEInterface IDE
		{
			get { return IDEInterface.Current; }
			set { IDEInterface.Current = value; }
		}

		BitmapImage _LanguageIcon = Util.FromDrawingImage(DIcons.dproj);
		public object LanguageIcon
		{
			get
			{
				return _LanguageIcon; 
			}
		}

		public DLanguageBinding()
		{
			// Files
			var exts = new string[] { ".d",".di" };
			var img = Util.FromDrawingImage(DIcons.dfile);
			_FileTypes.Add(new SourceFileType
			{
				Name="D Module",
				Description = "D Source Module",
				Extensions = exts,
				SmallImage = img,
				LargeImage=img,
				DefaultFilePrefix="Module"
			});

			//Projects
			exts = new string[] { ".dprj" };
			img = Util.FromDrawingImage(DIcons.cmd);
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Console Application",
				Description = "Console-based application",
				Extensions = exts,
				SmallImage = img,
				LargeImage = img,
				DefaultFilePrefix="ConsoleApp"
			});

			img = Util.FromDrawingImage(DIcons.Generic_Application);
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Window Application",
				Description = "Win32-based application",
				Extensions = exts,
				SmallImage = img,
				LargeImage = img,
				DefaultFilePrefix="Win32App"
			});

			img = Util.FromDrawingImage(DIcons.dll48);
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Dynamic Link Library",
				Description = "Win32 DLL project",
				Extensions = exts,
				SmallImage = img,
				LargeImage = img,
				DefaultFilePrefix="DynamicLinkLib"
			});

			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Static Link Library",
				Description = "Project which outputs a .lib file",
				Extensions = exts,
				SmallImage = img,
				LargeImage = img,
				DefaultFilePrefix="StaticLib"
			});
		}

		public string LanguageName	{	get { return "D"; }	}

		List<SourceFileType> _FileTypes = new List<SourceFileType>();
		List<SourceFileType> _ProjectTypes = new List<SourceFileType>();
		public SourceFileType[] ModuleTypes { get { return _FileTypes.ToArray(); } }
		public SourceFileType[] ProjectTypes { get { return _ProjectTypes.ToArray(); } }

		public bool ProjectsSupported{get { return true; }}
		public bool CanUseDebugging{get {return true; }}
		public bool CanUseCodeCompletion{get { return true; }}
		public bool CanBuild{get { return true; }}

		public bool CanBuildToSingleModule{	get { return true; }}

		DLanguage _Language=new DLanguage();
		public ILanguage Language{get { return _Language; }}

		public object GetNodeIcon(Parser.Core.INode Node)
		{
			throw new NotImplementedException();
		}

		public IProject CreateEmptyProject(SourceFileType FileType)
		{
			throw new NotImplementedException();
		}

		public IProject OpenProject(string FileName)
		{
			throw new NotImplementedException();
		}

		public IModule CreateEmptyModule(SourceFileType FileType)
		{
			throw new NotImplementedException();
		}

		public IModule OpenModule(string FileName)
		{
			throw new NotImplementedException();
		}

		public IDebugProvider DebugProvider
		{
			get { throw new NotImplementedException(); }
		}
	}
}
