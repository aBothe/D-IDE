using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using D_IDE.Core;
using D_Parser.Dom;
using D_Parser.Parser;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

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
			var ms = new MemoryStream(DResources.D_xshd);
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
		public override bool CanBuild { get { return true; } }

		#region Projecting
		public override Project CreateEmptyProject(string name, string prjfile,FileTemplate FileType)
		{
			var prj=new DProject();
			prj.Name = name;
			prj.FileName = prjfile;

			switch (_ProjectTypes.IndexOf(FileType))
			{
				case 0: // Console app
					prj.OutputType = OutputTypes.Executable;

					var mainFile = prj.BaseDirectory+ "\\main.d";

					File.WriteAllText(mainFile,	DResources.helloWorldConsoleApp);

					prj.Add(mainFile);
					prj.Save();

					break;
				case 1: // Win32 app
					prj.OutputType = OutputTypes.CommandWindowLessExecutable;

					// Create main file
					var mainFile2 = prj.BaseDirectory + "\\main.d";

					File.WriteAllText(mainFile2, DResources.winsamp_d);
					prj.Add(mainFile2);

					// Add library references
					prj.LinkedLibraries.AddRange(new[]{"kernel32.lib","gdi32.lib"});

					// Create Resources-directory
					var resDir = prj.BaseDirectory + "\\Resources";
					Util.CreateDirectoryRecursively(resDir);
					prj.SubDirectories.Add(resDir);

					// Create manifest & resource file
					var manifest=resDir+"\\Win32.manifest";
					
					File.WriteAllText(manifest,DResources.Win32Manifest);
					var manifestModule=prj.Add(manifest);

					// Prevent compilation of the manifest file
					manifestModule.Action = SourceModule.BuildAction.None;

					var rc = resDir + "\\Resources.rc";

					File.WriteAllText(rc, DResources.defResource);
					prj.Add(rc);

					// Finally save changes to the project
					prj.Save();

					break;
				case 2: // DLL
					prj.OutputType = OutputTypes.DynamicLibary;

					// We have explicitly reference to phobos library when linking to a .dll
					prj.LinkedLibraries.Add("phobos.lib");

					break;
				case 3:// Lib
					prj.OutputType = OutputTypes.StaticLibrary;

					var libmainFile = prj.BaseDirectory + Path.DirectorySeparatorChar + prj.Name.Replace(' ','_')+".d";

					File.WriteAllText(libmainFile, DResources.libExample);

					prj.Add(libmainFile);
					prj.Save();
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
			ret.ParseDSources();
			return ret;
		}
		#endregion

		#region Editing

		public static bool IsDSource(string file)
		{
			return file.EndsWith(".d") || file.EndsWith(".di");
		}

		public override bool SupportsEditor(string file)
		{
			return IsDSource(file);
		}

		public override EditorDocument OpenFile(Project Project, string SourceFile)
		{
			return new DEditorDocument(SourceFile);
		}

		/// <summary>
		/// Searches in current solution and in the global cache for the given file and returns it.
		/// If not found, file will be parsed.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static IAbstractSyntaxTree GetFileSyntaxTree(string file)
		{
			DProject a = null;
			return GetFileSyntaxTree(file, out a);
		}

		public static IAbstractSyntaxTree GetFileSyntaxTree(string file,out DProject OwnerProject)
		{
			OwnerProject = null;
			if (CoreManager.CurrentSolution != null)
			{
				foreach (var prj in CoreManager.CurrentSolution)
				{
					var dprj = prj as DProject;
					if (dprj == null) continue;
					if (dprj.ContainsFile(file))
					{
						OwnerProject = dprj;
						return dprj.ParsedModules[file];
					}
				}
			}

			var ret=DSettings.Instance.dmd2.ASTCache.LookUpModulePath(file);
			if (ret == null)
				ret = DSettings.Instance.dmd1.ASTCache.LookUpModulePath(file);

			if (ret != null)
				return ret;

			return DParser.ParseFile(file);
		}

		#endregion

		#region Building
		readonly DBuildSupport _BuildSupport = new DBuildSupport();

		public override AbstractBuildSupport BuildSupport
		{
			get
			{
				return _BuildSupport;
			}
		}
		#endregion

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
			var fExits=File.Exists(SuggestedFileName);

			if (fExits)
			{
				var x = XmlTextReader.Create(SuggestedFileName);

				DSettings.Instance.Load(x);

				x.Close();
			}

			// Provide first-time-start support by finding existing D installations and insert them + their libraries into the configuration
			if (InitialSetup.ShallProvideConfigSuppport)
				InitialSetup.SetupInitialDmdConfig();
		}

		public override AbstractSettingsPage SettingsPage
		{
			get {
				return new DSettingsPage();
			}
		}
		#endregion
	}

	/// <summary>
	/// Helper class for initial D-specific compiler settings such as searching existing dmd installations.
	/// </summary>
	class InitialSetup
	{
		public static bool ShallProvideConfigSuppport
		{
			get
			{
				return (!File.Exists(DSettings.Instance.dmd1.BaseDirectory + DSettings.Instance.dmd1.SoureCompiler) &&
					!File.Exists(DSettings.Instance.dmd2.BaseDirectory + DSettings.Instance.dmd2.SoureCompiler)) &&
					GlobalProperties.Instance.IsFirstTimeStart;
			}
		}

		public static void SetupInitialDmdConfig()
		{
			var res = MessageBox.Show(
					"No compiler configuration detected.\r\n"+
					"Shall D-IDE search for existing DMD configurations right now?", 
					"First-time launch", 
					MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (res == MessageBoxResult.No)
				return;

			// 1) Find installations
			var dict = new Dictionary<DVersion, string>();

			foreach (var dVersion in new[] { DVersion.D1, DVersion.D2 })
			{
				var instDir = SearchForDmdInstallations(dVersion);

				if (!string.IsNullOrEmpty(instDir))
					dict.Add(dVersion, instDir);
			}
				
			// 2) Offer them to the user & Insert them
			foreach (var kv in dict)
			{
				var dmdName=kv.Key==DVersion.D2?"dmd2":"dmd1";

				if(MessageBox.Show(
					"A "+dmdName+" installation was found at \"" + kv.Value + "\". Use it?", 
					"Installation found", 
					MessageBoxButton.YesNo, MessageBoxImage.Question) 
					== MessageBoxResult.Yes)
				{
					var cfg=DSettings.Instance.DMDConfig(kv.Key);

					// Set new base directory
					cfg.BaseDirectory = kv.Value;

					// Insert library paths
					cfg.TryAddImportPaths();
				}
			}
		}

		static string SearchForDmdInstallations(DVersion Version = DVersion.D2)
		{
			string dmdRoot = Version == DVersion.D2 ? "dmd2" : "dmd";
			string windowsBinPath=Path.Combine(dmdRoot,"windows", "bin");

			// Search in %PATH% variable

			var PATH= Environment.GetEnvironmentVariable("path");
			var PATH_Directories = PATH.Split(';');

			foreach (var PATH_Dir in PATH_Directories)
				if (PATH_Dir.TrimEnd('\\').EndsWith(windowsBinPath) &&
					File.Exists(Path.Combine(PATH_Dir, "dmd.exe")))
					return PATH_Dir;

			// Search logical drives
			var usuallyUsedDirectories = new[] { "", "D\\" };

			var drives = Directory.GetLogicalDrives();
			
			foreach (var drv in drives)
				foreach (var usedDir in usuallyUsedDirectories)
				{
					var assumedDmdDir = Path.Combine(drv, usedDir, windowsBinPath );
					var assumendDmdExe = assumedDmdDir+ "\\dmd.exe";

					if (File.Exists(assumendDmdExe))
						return assumedDmdDir;
				}

			return null;
		}
	}
}
