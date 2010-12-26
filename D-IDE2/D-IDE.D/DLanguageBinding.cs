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
		public object LanguageIcon
		{
			get
			{
				return DIcons.dproj; 
			}
		}

		public DLanguageBinding()
		{
			// Files
			var exts = new string[] { ".d",".di" };
			_FileTypes.Add(new SourceFileType
			{
				Name="D Module",
				Description = "D Source Module",
				Extensions = exts,
				SmallImage = DIcons.dfile16,
				LargeImage = DIcons.dfile32,
				DefaultFilePrefix="Module"
			});

			//Projects
			exts = new string[] { ".dprj" }; // All projects of the D language have the same extension
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Console Application",
				Description = "Console-based application",
				Extensions = exts,
				SmallImage = DIcons.dproj16,
				LargeImage = DIcons.dproj32,
				DefaultFilePrefix="ConsoleApp"
			});

			var img2 =DIcons.Generic_Application;
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Window Application",
				Description = "Win32-based application",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
				DefaultFilePrefix="Win32App"
			});

			img2 = DIcons.dll48;
			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Dynamic Link Library",
				Description = "Win32 DLL project",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
				DefaultFilePrefix="DynamicLinkLib"
			});

			_ProjectTypes.Add(new SourceFileType
			{
				Name = "Static Link Library",
				Description = "Project which outputs a .lib file",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
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
			var prj=new DProject(FileType);

			switch (_ProjectTypes.IndexOf(FileType))
			{
				case 0: // Console app
					prj.OutputType = OutputTypes.Executable;
					break;
				case 1: // Win32 app
					prj.OutputType = OutputTypes.CommandWindowLessExecutable;
					break;
				case 2: // DLL
					prj.OutputType = OutputTypes.DynamicLibary;
					break;
				case 3:// Lib
					prj.OutputType = OutputTypes.Other;
					break;
				default:
					return null;
			}

			return prj;
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
