using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using D_IDE.Core;
using D_Parser;
using DebugEngineWrapper;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Parser.Core;
using System.Xml;
using System.IO;
using System.Diagnostics;

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

			// Associate highlighting definitions
			var ms = new MemoryStream(DIcons.d_xshd);
			var hi = HighlightingLoader.Load(new XmlTextReader(ms), HighlightingManager.Instance);
			HighlightingManager.Instance.RegisterHighlighting(
				"D", new[] { ".d", ".di" }, hi);
			ms.Close();
		}

		public override string LanguageName	{	get { return "D"; }	}

		List<FileTemplate> _FileTypes = new List<FileTemplate>();
		List<FileTemplate> _ProjectTypes = new List<FileTemplate>();

		public override bool ProjectsSupported{get { return true; }}
		public override bool CanUseDebugging { get { return true; } }
		public override bool CanUseCodeCompletion { get { return true; } }
		public override bool CanBuild { get { return true; } }

		#region Code Completion
		DLanguage _Language=new DLanguage();
		public override ILanguage Language { get { return _Language; } }
		#endregion

		#region Projecting
		public override Project CreateEmptyProject(FileTemplate FileType)
		{
			var prj=new DProject();

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
		#endregion

		readonly DBuildSupport _BuildSupport = new DBuildSupport();

		public override IBuildSupport BuildSupport
		{
			get
			{
				return _BuildSupport;
			}
		}

		#region Debugging
		GenericDebugSupport DDebugging ;
		public override GenericDebugSupport DebugSupport
		{
			get
			{
				if (DDebugging == null)
					DDebugging = new DDebugSupport();
				return DDebugging;
			}
		}
		#endregion

		#region Settings
		public override bool CanUseSettings	{get { return true; }}

		public override void SaveSettings(string SuggestedFileName)
		{
			var x = XmlTextWriter.Create(SuggestedFileName);

			DSettings.Instance.Save(x);

			x.Close();
		}

		public override void LoadSettings(string SuggestedFileName)
		{
			if (!File.Exists(SuggestedFileName))
				return;

			var x = XmlTextReader.Create(SuggestedFileName);

			DSettings.Instance.Load(x);

			x.Close();
		}

		public override AbstractSettingsPage SettingsPage
		{
			get { return null; }
		}
		#endregion
	}
}
