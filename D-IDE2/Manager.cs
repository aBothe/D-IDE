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



			public static string GetAbsoluteFileName(IProject Project, string file)
			{
				if (Path.IsPathRooted(file))
					return file;
				return ProjectManagement.ProjectBaseDirectory(Project) + file;
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
			public static EditorDocument OpenFile(IProject Project, string FileName)
			{
				return null;
			}

			public static EditorDocument OpenFile(string FileName)
			{
				return OpenFile(null, FileName);
			}
		}
	}
}
