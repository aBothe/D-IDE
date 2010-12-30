using Parser.Core;
using D_IDE.Core;
using System.IO;
using System.Linq;

namespace D_IDE
{
	class IDEManager
	{
		#region Properties
		public static MainWindow MainWindow;
		public static AvalonDock.DockingManager DockMgr
		{
			get { return MainWindow.DockMgr; }
		}
		public static AbstractEditorDocument CurrentEditor
		{
			get	{return DockMgr.ActiveDocument as AbstractEditorDocument;}
		}
		/// <summary>
		/// There can be only one open solution. 
		/// Stand-alone modules are opened independently of any other open solutions, projects or modules
		/// </summary>
		public static Solution CurrentSolution;
		#endregion

		public class ProjectManagement
		{
		/// <summary>
		/// Creates a new project.
		/// Doesn't add it to the current solution.
		/// Doesn't modify the current solution.
		/// </summary>
		public static IProject CreateNewProject(ILanguageBinding Binding,SourceFileType ProjectType,string Name,string BaseDir)
		{
			var prj = Binding.CreateEmptyProject(ProjectType);
			prj.Name = Name;
			prj.FileName =
				BaseDir.Trim('\\', ' ', '\t') + "\\" +
				Path.ChangeExtension(Util.PurifyFileName(Name), ProjectType.Extensions[0]);

			// Set the output dir to 'bin' by default
			// Perhaps we'll change this default value some time later in the global settings
			prj.OutputDirectory = "bin";

			return prj;
		}

		public static Solution CreateNewProjectAndSolution(			ILanguageBinding Binding,			SourceFileType ProjectType,			string Name,			string BaseDir,			string SolutionName)
		{
			var sln = new Solution();
			sln.Name = SolutionName;
			sln.FileName =
				BaseDir.Trim('\\',' ','\t')+ "\\" +
				Path.ChangeExtension(Util.PurifyFileName(Name), Solution.SolutionExtension);

			AddNewProjectToSolution(sln, Binding, ProjectType, Name, BaseDir);

			return sln;
		}

		/// <summary>
		/// Central method to load a project whereas its file extension is used to identify
		/// the generic project type.
		/// </summary>
		public static IProject LoadProjectFromFile(string FileName)
		{
			string ls = FileName.ToLower();

			foreach (var lang in from l in LanguageLoader.Bindings where l.ProjectsSupported select l)
				foreach (var pt in lang.ProjectTypes)
					foreach (var ext in pt.Extensions)
						if (ls.EndsWith(ext))
							return lang.OpenProject(FileName);
			return null;
		}

		/// <summary>
		/// Creates a new project and adds it to the current solution
		/// </summary>
		public static IProject AddNewProjectToSolution(			Solution sln,			ILanguageBinding Binding,			SourceFileType ProjectType,			string Name,			string BaseDir)
		{
			var prj = CreateNewProject(Binding, ProjectType, Name, BaseDir);
			sln.AddProject(prj);

			return prj;
		}

		public static IProject AddNewProjectToCurrentSolution(ILanguageBinding Binding,			SourceFileType ProjectType,			string Name,			string BaseDir)
		{
			return AddNewProjectToSolution(CurrentSolution,Binding,ProjectType,Name,BaseDir);
		}

		public static void ReassignProject(IProject Project, Solution NewSolution)
		{
			
		}

		public static string ProjectBaseDirectory(IProject Project)
		{
			return System.IO.Path.GetDirectoryName(Project.FileName);
		}

		#region Project Dependencies dialog
		static void ShowProjectDependenciesDialog(Solution sln,IProject Project)
		{

		}

		public static void ShowProjectDependenciesDialog(IProject Project)
		{
			ShowProjectDependenciesDialog(Project.Solution,Project);
		}

		public static void ShowProjectDependenciesDialog(Solution sln)
		{
			ShowProjectDependenciesDialog(sln,null);
		}

		public static void ShowProjectPropertiesDialog(IProject Project)
		{

		}
		#endregion
		}

		public class FileManagement
		{
			/// <summary>
			/// Opens a new-source dialog
			/// </summary>
			public static void AddNewSourceToProject(IProject Project, string RelativeDir)
			{

			}

			public static void AddNewDirectoryToProject(IProject Project, string RelativeDir, string DirName)
			{

			}



			public static void AddExistingSourceToProject(string FileName ,IProject Project,string RelativeDir)
			{

			}

			public static void AddExistingDirectoryToProject(string DirectoryPath,IProject Project, string RelativeDir)
			{

			}



			public static void CopyFile(IProject Project, string FileName, IProject TargetProject, string NewDirectory)
			{

			}

			public static void CopyDirectory(IProject Project, string RelativeDir, IProject TargetProject, string NewDir)
			{

			}

			public static void MoveFile(IProject Project, string FileName, IProject TargetProject, string NewDirectory)
			{

			}

			public static void MoveDirectory(IProject Project, string RelativeDir, IProject TargetProject, string NewDir)
			{

			}

			public static void ExcludeDirectoryFromProject(IProject prj, string RelativePath)
			{
				if (prj.SubDirectories.Contains(RelativePath))
					prj.SubDirectories.Remove(RelativePath);

				foreach (var s in prj.Files)
				{

				}
			}



			public static void ExludeFileFromProject(IProject Project, string file)
			{

			}

			public static void RemoveDirectoryFromProject(IProject Project, string RelativePath)
			{

			}

			public static void RemoveFileFromProject(IProject Project, string file)
			{

			}



			public static string ToAbsoluteFileName(IProject Project, string file)
			{
				if (Path.IsPathRooted(file))
					return file;
				return ProjectManagement.ProjectBaseDirectory(Project)+ "\\" + file;
			}

			public static string ToRelativeFileName(IProject Project, string file)
			{
				if (Path.IsPathRooted(file) && Project != null)
					return file.Remove(0, IDEManager.ProjectManagement.ProjectBaseDirectory(Project).Length).Trim('\\');
				return file;
			}

			public static bool ContainsFile(IProject Project, string file)
			{
				var path = ToRelativeFileName(Project, file);
				return Project.Files.Contains(path);
			}

			public static IModule GetModule(IProject Project, string file)
			{
				var relPath = ToRelativeFileName(Project, file);

				foreach (var m in Project.ModuleCache)
				{
					if (ToRelativeFileName(Project,m.FileName) == relPath)
						return m;
				}
				
				return null;
			}

			public static bool RenameFile(IProject Project, string file, string NewFileName)
			{
				return true;
			}
		}

		public class BuildManagement
		{
			public static bool BuildSolution(Solution sln)
			{
				return false;
			}

			public static bool BuildProject(IProject Project)
			{
				return false;
			}

			public static bool BuildSingleModule(IModule Module)
			{
				return false;
			}
		}

		public class EditingManagement
		{
			/// <summary>
			/// Central method to open a file OR a project
			/// </summary>
			/// <returns>Editor instance if a source file was opened</returns>
			public static AbstractEditorDocument OpenFile(string FileName)
			{
				return OpenFile(null, FileName);
			}

			public static AbstractEditorDocument OpenFile(IProject OwnerProject,string FileName)
			{
				/*
				 * 1. Solution check
				 * 2. Project file check
				 * 3. Solution file check
				 */
				var ext = Path.GetExtension(FileName);

				if (ext == Solution.SolutionExtension)
				{
					/*
					 * - Load solution and load all of its projects
					 * - Open last opened files
					 */
					CurrentSolution = Solution.LoadFromFile(FileName);

					foreach (var f in CurrentSolution.ProjectFiles)
						CurrentSolution.ProjectCache.Add(ProjectManagement.LoadProjectFromFile(f));

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(prj,fn);

					return null;
				}


				var LoadedPrj = IDEManager.ProjectManagement.LoadProjectFromFile(FileName);
				if (LoadedPrj != null)
				{
					/* 
					 * - Load project
					 * - Create anonymous solution that holds the project virtually
					 * - Open last opened files
					 */
					CurrentSolution = new Solution();
					CurrentSolution.Name = LoadedPrj.Name;
					CurrentSolution.AddProject(LoadedPrj);

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(prj,fn);

					return null;
				}

				// Try to resolve owner project
				var _prj = OwnerProject;
				if(_prj==null && CurrentSolution!=null)
					foreach(var p in CurrentSolution.ProjectCache)
						if(FileManagement.ContainsFile(p,FileName))
						{
							_prj=p;
							break;
						}

				// Check if file already open
				var relPath = FileManagement.ToRelativeFileName(_prj, FileName);
				foreach (var doc in DockMgr.Documents)
					if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).RelativeFilePath == relPath)
					{
						doc.Activate();
						return doc as AbstractEditorDocument;
					}

				var newEd = new EditorDocument(_prj, relPath);
				newEd.Show(DockMgr);

				return newEd;
			}

			public static void SaveCurrentFile()
			{
				if (CurrentEditor != null)
					CurrentEditor.Save();
			}

			public static void SaveAllFiles()
			{
				foreach (var doc in DockMgr.Documents)
					if (doc is AbstractEditorDocument)
					{
						(doc as AbstractEditorDocument).Save();
					}
			}

			/// <summary>
			/// Saves the file under a new file name.
			/// Renames it in its project if possible.
			/// </summary>
			public static void SaveCurrentFileAs(string NewFilePath)
			{
				if (CurrentEditor == null) return;

				if (CurrentEditor.Project != null)
					IDEManager.FileManagement.RenameFile(
						CurrentEditor.Project,
						CurrentEditor.FileName, NewFilePath);
				else
				{
					CurrentEditor.FileName = NewFilePath;
					CurrentEditor.Save();
				}
			}
		}
	}
}
