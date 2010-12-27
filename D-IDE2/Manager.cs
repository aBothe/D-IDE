using Parser.Core;
using D_IDE.Core;
using System.IO;

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
				BaseDir + "\\" +
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
				BaseDir + "\\" +
				Path.ChangeExtension(Util.PurifyFileName(Name), Solution.SolutionExtension);

			AddNewProjectToSolution(sln, Binding, ProjectType, Name, BaseDir);

			return sln;
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
		
		
		public static void ExludeFileFromProject(IProject Project, string file)
		{

		}

		public static void RemoveDirectoryFromProject(IProject Project, string RelativePath)
		{

		}

		public static void RemoveFileFromProject(IProject Project, string file)
		{

		}

		#region Project Dependencies dialogue
		static void ShowProjectDependenciesDialogue(Solution sln,IProject Project)
		{

		}

		public static void ShowProjectDependenciesDialogue(IProject Project)
		{
			ShowProjectDependenciesDialogue(Project.Solution,Project);
		}

		public static void ShowProjectDependenciesDialogue(Solution sln)
		{
			ShowProjectDependenciesDialogue(sln,null);
		}
		#endregion
		}

		#region Module & File management
		public static EditorDocument OpenFile(IProject Project, string FileName)
		{
			return null;
		}

		public static EditorDocument OpenFile(string FileName)
		{
			return OpenFile(null, FileName);
		}
		#endregion
	}
}
