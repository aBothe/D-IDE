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
		public static Project CreateNewProject(AbstractLanguageBinding Binding,FileTemplate ProjectType,string Name,string BaseDir)
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

		public static Solution CreateNewProjectAndSolution(			AbstractLanguageBinding Binding,			FileTemplate ProjectType,			string Name,			string BaseDir,			string SolutionName)
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
		/// Creates a new project and adds it to the current solution
		/// </summary>
		public static Project AddNewProjectToSolution(			Solution sln,			AbstractLanguageBinding Binding,			FileTemplate ProjectType,			string Name,			string BaseDir)
		{
			var prj = CreateNewProject(Binding, ProjectType, Name, BaseDir);
			sln.AddProject(prj);

			return prj;
		}

		public static Project AddNewProjectToCurrentSolution(AbstractLanguageBinding Binding,			FileTemplate ProjectType,			string Name,			string BaseDir)
		{
			return AddNewProjectToSolution(CurrentSolution,Binding,ProjectType,Name,BaseDir);
		}

		public static void ReassignProject(Project Project, Solution NewSolution)
		{
			
		}

		#region Project Dependencies dialog
		static void ShowProjectDependenciesDialog(Solution sln,Project Project)
		{

		}

		public static void ShowProjectDependenciesDialog(Project Project)
		{
			ShowProjectDependenciesDialog(Project.Solution,Project);
		}

		public static void ShowProjectDependenciesDialog(Solution sln)
		{
			ShowProjectDependenciesDialog(sln,null);
		}

		public static void ShowProjectPropertiesDialog(Project Project)
		{

		}
		#endregion
		}

		public class FileManagement
		{
			/// <summary>
			/// Opens a new-source dialog
			/// </summary>
			public static void AddNewSourceToProject(Project Project, string RelativeDir)
			{

			}

			public static void AddNewDirectoryToProject(Project Project, string RelativeDir, string DirName)
			{

			}


			/// <summary>
			/// Creates a dialog which asks the user to add a file
			/// </summary>
			public static void AddExistingSourceToProject(Project Project, string RelativeDir)
			{

			}

			public static void AddExistingSourceToProject(string FileName ,Project Project,string RelativeDir)
			{

			}

			public static void AddExistingDirectoryToProject(string DirectoryPath,Project Project, string RelativeDir)
			{

			}



			public static void CopyFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{

			}

			public static void CopyDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{

			}

			public static void MoveFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{

			}

			public static void MoveDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{

			}

			public static void ExcludeDirectoryFromProject(Project prj, string RelativePath)
			{
				if (prj.SubDirectories.Contains(RelativePath))
					prj.SubDirectories.Remove(RelativePath);

				foreach (var s in prj.Files)
				{

				}
			}



			public static void ExludeFileFromProject(Project Project, string file)
			{

			}

			public static void RemoveDirectoryFromProject(Project Project, string RelativePath)
			{

			}

			public static void RemoveFileFromProject(Project Project, string file)
			{

			}

			public static bool RenameFile(Project Project, string file, string NewFileName)
			{
				return true;
			}
		}

		public class BuildManagement
		{
			public static bool Build(Solution sln,bool Incrementally)
			{
				return false;
			}

			public static bool Build(Project Project, bool Incrementally)
			{
				return false;
			}

			/// <summary>
			/// Builds currently edited document to single executable
			/// </summary>
			/// <returns></returns>
			public static bool BuildSingle()
			{
				return false;
			}

			public static void CleanUpOutput(Solution sln)
			{

			}

			public static void CleanUpOutput(Project Project)
			{

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

			public static AbstractEditorDocument OpenFile(Project OwnerProject,string FileName)
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
					CurrentSolution =new Solution(FileName);

					foreach (var f in CurrentSolution.ProjectFiles)
						CurrentSolution.ProjectCache.Add(Project.LoadProjectFromFile(f));

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(prj,fn);

					return null;
				}


				var LoadedPrj = Project.LoadProjectFromFile(FileName);
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
						if(p.ContainsFile(FileName))
						{
							_prj=p;
							break;
						}

				// Check if file already open
				var absPath = _prj!=null?_prj.ToAbsoluteFileName(FileName):FileName;
				foreach (var doc in DockMgr.Documents)
					if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).AbsoluteFilePath == absPath)
					{
						doc.Activate();
						return doc as AbstractEditorDocument;
					}

				var newEd = new EditorDocument(_prj, absPath);
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
