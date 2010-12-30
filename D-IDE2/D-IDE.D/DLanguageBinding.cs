using System;
using System.Collections.Generic;
using D_IDE.Core;
using Parser.Core;
using D_Parser;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using DebugEngineWrapper;

namespace D_IDE.D
{
	public class DLanguageBinding:AbstractLanguageBinding
	{
		public override object LanguageIcon{get	{return DIcons.dproj32; }}

		public DLanguageBinding()
		{
			// Files
			var exts = new string[] { ".d",".di" };
			_FileTypes.Add(new FileTemplate
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
			_ProjectTypes.Add(new FileTemplate
			{
				Name = "Console Application",
				Description = "Console-based application",
				Extensions = exts,
				SmallImage = DIcons.dproj16,
				LargeImage = DIcons.dproj32,
				DefaultFilePrefix="ConsoleApp"
			});

			var img2 =DIcons.Generic_Application;
			_ProjectTypes.Add(new FileTemplate
			{
				Name = "Window Application",
				Description = "Win32-based application",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
				DefaultFilePrefix="Win32App"
			});

			img2 = DIcons.dll48;
			_ProjectTypes.Add(new FileTemplate
			{
				Name = "Dynamic Link Library",
				Description = "Win32 DLL project",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
				DefaultFilePrefix="DynamicLinkLib"
			});

			_ProjectTypes.Add(new FileTemplate
			{
				Name = "Static Link Library",
				Description = "Project which outputs a .lib file",
				Extensions = exts,
				SmallImage = img2,
				LargeImage = img2,
				DefaultFilePrefix="StaticLib"
			});
		}

		public override string LanguageName	{	get { return "D"; }	}

		List<FileTemplate> _FileTypes = new List<FileTemplate>();
		List<FileTemplate> _ProjectTypes = new List<FileTemplate>();

		public override bool ProjectsSupported{get { return true; }}
		public override bool CanUseDebugging { get { return true; } }
		public override bool CanUseCodeCompletion { get { return true; } }
		public override bool CanBuild { get { return true; } }

		public override bool CanBuildToSingleModule { get { return true; } }


		DLanguage _Language=new DLanguage();
		public override ILanguage Language { get { return _Language; } }

		public override Project CreateEmptyProject(FileTemplate FileType)
		{
			var prj=new Project();

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

		public override object SmallProjectIcon		{			get { return DIcons.dproj16; }		}
		public override object LargeProjectIcon		{			get { return DIcons.dproj32; }		}
		public override FileTemplate[] ModuleTemplates { get { return _FileTypes.ToArray(); } }
		public override FileTemplate[] ProjectTemplates	{get { return _ProjectTypes.ToArray(); }}

		public override Project OpenProject(Solution sln,string FileName)
		{
			var ret = new DProject(sln,FileName);
			ret.ReloadProject();
			return ret;
		}

		public override bool BuildProject(Project Project)
		{
			throw new NotImplementedException();
		}

		public override BuildError[] BuildSingleModule(string FileName)
		{
			throw new NotImplementedException();
		}

		public override string BuildSymbolValueString(AbstractSyntaxTree ModuleTree, uint ScopedSrcLine, DebugScopedSymbol sym)
		{
			throw new NotImplementedException();
		}
	}
}
