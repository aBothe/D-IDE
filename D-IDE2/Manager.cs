using Parser.Core;
using D_IDE.Core;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using D_IDE.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;

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

		public static IEnumerable<AbstractEditorDocument> Editors
		{
			get { return from e in DockMgr.Documents where e is AbstractEditorDocument select e as AbstractEditorDocument; }
		}

		/// <summary>
		/// There can be only one open solution. 
		/// Stand-alone modules are opened independently of any other open solutions, projects or modules
		/// </summary>
		public static Solution CurrentSolution { get; set; }
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

		public static bool Rename(Solution sln, string NewName)
		{
			// Prevent moving the project into an other directory
			if (NewName.Contains('\\'))
				return false;

			var newSolutionFileName = Path.ChangeExtension(Util.PurifyFileName(NewName), Solution.SolutionExtension);
			var ret= Util.MoveFile(sln.FileName,newSolutionFileName);
			if (ret)
			{
				sln.Name = NewName;
				sln.FileName = sln.BaseDirectory + "\\" + newSolutionFileName;
				MainWindow.UpdateTitle();
			}
			return ret;
		}

		public static bool Rename(Project prj, string NewName)
		{
			// Prevent moving the project into an other directory
			if (NewName.Contains('\\'))
				return false;

			var newSolutionFileName =Util.PurifyFileName( NewName)+ Path.GetExtension(prj.FileName);
			var ret = Util.MoveFile(prj.FileName, newSolutionFileName);
			if (ret)
			{
				prj.Name = NewName;
				prj.FileName = prj.BaseDirectory + "\\" + newSolutionFileName;
			}
			return ret;
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
				var sdlg = new NewSrcDlg();
				if (sdlg.ShowDialog().Value)
				{
					var file = (String.IsNullOrEmpty(RelativeDir) ? "" : (RelativeDir + "\\")) + sdlg.FileName;
					File.WriteAllText(Project.BaseDirectory+"\\"+file,"");
					Project.Add(file);
					Project.Save();
					MainWindow.UpdateGUIElements();
				}
			}

			public static void AddNewDirectoryToProject(Project Project, string RelativeDir, string DirName)
			{
				string absDir = Project.BaseDirectory + "\\" + (String.IsNullOrEmpty(RelativeDir) ? "" : (RelativeDir + "\\")) + DirName;
				Util.CreateDirectoryRecursively(absDir);
			}


			/// <summary>
			/// Creates a dialog which asks the user to add a file
			/// </summary>
			public static void AddExistingSourceToProject(Project Project, string RelativeDir)
			{
				var dlg = new OpenFileDialog();
				var absPath = (Project.BaseDirectory + "\\" + RelativeDir).Trim('\\');
				dlg.InitialDirectory = absPath;
				dlg.Multiselect = true;

				if (dlg.ShowDialog().Value)
				{
					/*
					 * - If not in the same directory, copy the selected file into ours
					 * - Add it to the project
					 */
					foreach (var file in dlg.FileNames)
					{
						var newFile=absPath+"\\"+Path.GetFileName(file);

						if (Path.GetDirectoryName(file) != absPath)
							File.Copy(file,newFile);

						Project.Add(newFile);
					}
					MainWindow.UpdateGUIElements();
				}
			}

			public static void AddExistingSourceToProject(string FileName ,Project Project,string RelativeDir)
			{

			}

			public static void AddExistingDirectoryToProject(string DirectoryPath,Project Project, string RelativeDir)
			{

			}



			public static bool CopyFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{
				return false;
			}

			public static bool CopyDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				return false;
			}

			public static bool MoveFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{
				return false;
			}

			public static bool MoveDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				return false;
			}

			public static void ExcludeDirectoryFromProject(Project prj, string RelativePath)
			{
				if (prj.SubDirectories.Contains(RelativePath))
					prj.SubDirectories.Remove(RelativePath);

				foreach (var s in prj.Files)
				{

				}
			}



			public static bool ExludeFileFromProject(Project Project, string file)
			{
				var absFile = Project.ToAbsoluteFileName(file);
				foreach (var ed in Editors)
					if (ed.AbsoluteFilePath == absFile)
						if (!ed.Close())
							return false;
				if (Project.Remove(file))
				{
					Project.Save();
					return true;
				}
				return false;
			}

			public static void RemoveDirectoryFromProject(Project Project, string RelativePath)
			{

			}

			public static bool RemoveFileFromProject(Project Project, string file)
			{
				var r = ExludeFileFromProject(Project, file);
				try{
					if (r) File.Delete(file);
				}catch {}
				return r;
			}

			public static bool RenameFile(Project Project, string file, string NewFileName)
			{
				var absPath=Project.ToAbsoluteFileName(file);
				var newFilePath = Util.PurifyFileName( NewFileName);
				var ret = Util.MoveFile(absPath, newFilePath);
				if (ret)
				{
					Project.Remove(file);
					Project.Add(Path.GetDirectoryName(absPath) + "\\" + newFilePath);
					Project.Save();

					foreach (var e in Editors)
						if (e.AbsoluteFilePath == absPath)
							e.FileName = Path.GetDirectoryName(absPath) + "\\" + newFilePath;
				}
				return ret;
			}

			public static bool RenameDirectory(Project Project, string dir, string NewDirName)
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
						CurrentSolution.ProjectCache.Add(Project.LoadProjectFromFile(CurrentSolution,f));

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(prj,fn);
					MainWindow.UpdateGUIElements();
					return null;
				}

				var langs=LanguageLoader.Bindings.Where(l => l.CanHandleProject(FileName)).ToArray();
				if (langs.Length>0)
				{
					/* 
					 * - Load project
					 * - Create anonymous solution that holds the project virtually
					 * - Open last opened files
					 */
					CurrentSolution = new Solution();
					CurrentSolution.FileName = Path.ChangeExtension(FileName, Solution.SolutionExtension);

					var LoadedPrj = langs[0].OpenProject(CurrentSolution, FileName);

					CurrentSolution.Name = LoadedPrj.Name;
					CurrentSolution.AddProject(LoadedPrj);

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(prj,fn);
					MainWindow.UpdateGUIElements();
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
				MainWindow.UpdateGUIElements();
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
						(doc as AbstractEditorDocument).Save();

				if (CurrentSolution != null)
				{
					CurrentSolution.Save();

					foreach (var p in CurrentSolution)
						p.Save();
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
