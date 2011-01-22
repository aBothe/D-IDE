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
		public static bool CanUpdateGUI = true;
		public static void UpdateGUI()
		{
			if (CanUpdateGUI)
				MainWindow.UpdateGUIElements();
		}

		public static MainWindow MainWindow;
		public static AvalonDock.DockingManager DockMgr
		{
			get { return MainWindow.DockMgr; }
		}
		public static AbstractEditorDocument CurrentEditor
		{
			get { return DockMgr.ActiveDocument as AbstractEditorDocument; }
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
			public static Project CreateNewProject(AbstractLanguageBinding Binding, FileTemplate ProjectType, string Name, string BaseDir)
			{
				/*
				 * Enforce the creation of a new project directory
				 */
				var baseDir = BaseDir.Trim('\\', ' ', '\t') + "\\" + Util.PurifyDirName(Name);
				if (Directory.Exists(baseDir))
				{
					ErrorLogger.Log(new Exception("Project directory exists already"));
					return null;
				}

				Util.CreateDirectoryRecursively(baseDir);

				var prj = Binding.CreateEmptyProject(ProjectType);
				prj.Name = Name;
				prj.FileName =
					baseDir + "\\" + 
					Path.ChangeExtension(Util.PurifyFileName(Name), ProjectType.Extensions[0]);

				// Set the output dir to 'bin' by default
				// Perhaps we'll change this default value some time later in the global settings
				prj.OutputDirectory = "bin";

				return prj;
			}

			/// <summary>
			/// Note: The current solution will not be touched
			/// </summary>
			public static Solution CreateNewProjectAndSolution(AbstractLanguageBinding Binding, FileTemplate ProjectType, string Name, string BaseDir, string SolutionName)
			{
				var baseDir = BaseDir.Trim('\\', ' ', '\t');
				var sln = new Solution();
				sln.Name = SolutionName;
				sln.FileName =
					baseDir + "\\" +
					Path.ChangeExtension(Util.PurifyFileName(Name), Solution.SolutionExtension);

				AddNewProjectToSolution(sln, Binding, ProjectType, Name, baseDir);
				
				return sln;
			}

			/// <summary>
			/// Creates a new project and adds it to the current solution
			/// </summary>
			public static Project AddNewProjectToSolution(Solution sln, AbstractLanguageBinding Binding, FileTemplate ProjectType, string Name, string BaseDir)
			{
				var prj = CreateNewProject(Binding, ProjectType, Name, BaseDir);
				if (prj != null)
				{
					prj.Save();
					sln.AddProject(prj);
					sln.Save();
				}
				return prj;
			}

			/// <summary>
			/// Adds a new project to the current solution
			/// </summary>
			public static Project AddNewProjectToSolution(AbstractLanguageBinding Binding, FileTemplate ProjectType, string Name, string BaseDir)
			{
				return AddNewProjectToSolution(CurrentSolution, Binding, ProjectType, Name, BaseDir);
			}

			public static bool AddExistingProjectToSolution(Solution sln, string Projectfile)
			{
				/*
				 * a) Check if project already existing
				 * b) Add to solution; if succeeded:
				 * c) Try to load project; if succeeded:
				 * d) Save solution
				 */

				// a)
				if (sln.ContainsProject(Projectfile)) {
					ErrorLogger.Log(new ProjectException(sln[Projectfile],"Project already part of solution"));
					return false; }
				// b)
				if (!sln.AddProject(Projectfile))
					return false;
				// c)
				var prj = Project.LoadProjectFromFile(sln, Projectfile);
				if (prj == null) return false; // Perhaps it's a project format that's simply not supported
				
				// d)
				sln.Save();

				MainWindow.UpdateGUIElements();
				return true;
			}

			/// <summary>
			/// Opens a dialog which asks the user to select one or more project files
			/// </summary>
			/// <returns></returns>
			public static bool AddExistingProjectToSolution(Solution sln)
			{
				var of = new OpenFileDialog();
				of.InitialDirectory = sln.BaseDirectory;

				// Build filter string
				string tfilter = "";
				var all_exts = new List<string>();
				foreach (var lang in from l in LanguageLoader.Bindings where l.ProjectsSupported select l)
				{
					tfilter+="|"+lang.LanguageName+" projects|";
					var exts=new List<string>();

					foreach (var t in lang.ProjectTemplates)
						if (t.Extensions != null)
							foreach (var ext in t.Extensions)
							{
								if (!exts.Contains("*"+ext))
									exts.Add("*" + ext);
								if (!all_exts.Contains("*" + ext))
									all_exts.Add("*" + ext);
							}

					tfilter += string.Join(";",exts);
				}
				tfilter = "All supported projects|" + string.Join(";", all_exts) + tfilter+ "|All files|*.*";
				of.Filter = tfilter;

				of.Multiselect = true;

				var r=true;
				if (of.ShowDialog().Value)
				{
					foreach (var file in of.FileNames)
						if (!AddExistingProjectToSolution(sln, file))
							r = false;
				}

				return r;
			}

			public static void ReassignProject(Project Project, Solution NewSolution)
			{

			}

			public static bool Rename(Solution sln, string NewName)
			{
				// Prevent moving the project into an other directory
				if (String.IsNullOrEmpty(NewName) || NewName.Contains('\\'))
					return false;

				/*
				 * - Try to rename the solution file
				 * - Rename the solution
				 * - Save it
				 */

				var newSolutionFileName = Path.ChangeExtension(Util.PurifyFileName(NewName), Solution.SolutionExtension);
				var ret = Util.MoveFile(sln.FileName, newSolutionFileName);
				if (ret)
				{
					sln.Name = NewName;
					sln.FileName = sln.BaseDirectory + "\\" + newSolutionFileName;
					MainWindow.UpdateTitle();
					sln.Save();
				}
				return ret;
			}

			public static bool Rename(Project prj, string NewName)
			{
				// Prevent moving the project into an other directory
				if (String.IsNullOrEmpty(NewName) || NewName.Contains('\\'))
					return false;

				/*
				 * - Try to rename the project file
				 * - If successful, remove old project file from solution
				 * - Rename the project and it's filename
				 * - Add the 'new' project to the solution
				 * - Save everything
				 */

				var newSolutionFileName = Util.PurifyFileName(NewName) + Path.GetExtension(prj.FileName);
				var ret = Util.MoveFile(prj.FileName, newSolutionFileName);
				if (ret)
				{
					prj.Solution.ExcludeProject(prj.FileName);					
					prj.Name = NewName;
					prj.FileName = prj.BaseDirectory + "\\" + newSolutionFileName;
					prj.Solution.AddProject(prj);

					prj.Solution.Save();
					prj.Save();
				}
				return ret;
			}

			/// <summary>
			/// (Since we don't want to remove a whole project we still can exclude them from solutions)
			/// </summary>
			/// <param name="prj"></param>
			public static void ExcludeProject(Project prj)
			{
				/*
				 * - Close open editors that are related to prj
				 * - Remove reference from solution
				 * - Save solution
				 */

				foreach (var ed in Editors.Where(e => e.Project == prj))
					ed.Close();

				var sln = prj.Solution;
				sln.ExcludeProject(prj.FileName);
				sln.Save();

				MainWindow.UpdateGUIElements();
			}

			/// <summary>
			/// (Since we don't want to remove a whole project we still can exclude them from solutions)
			/// </summary>
			public static void ExcludeProject(Solution sln,string prjFile)
			{
				/*
				 * - Remove reference from solution
				 * - Save solution
				 */

				sln.ExcludeProject(prjFile);
				sln.Save();

				MainWindow.UpdateGUIElements();
			}

			#region Project Dependencies dialog
			static void ShowProjectDependenciesDialog(Solution sln, Project Project)
			{

			}

			public static void ShowProjectDependenciesDialog(Project Project)
			{
				ShowProjectDependenciesDialog(Project.Solution, Project);
			}

			public static void ShowProjectDependenciesDialog(Solution sln)
			{
				ShowProjectDependenciesDialog(sln, null);
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
			public static bool AddNewSourceToProject(Project Project, string RelativeDir)
			{
				var sdlg = new NewSrcDlg();
				if (sdlg.ShowDialog().Value)
				{
					var file = (String.IsNullOrEmpty(RelativeDir) ? "" : (RelativeDir + "\\")) + sdlg.FileName;
					var absFile=Project.BaseDirectory + "\\" + file;

					if (File.Exists(absFile))
						return false;

					if (Project.Add(file))
					{
						File.WriteAllText(absFile, "");
						Project.Save();
						UpdateGUI();
						return true;
					}
				}
				return false;
			}

			public static bool AddNewDirectoryToProject(Project Project, string RelativeDir, string DirName)
			{
				if (Project==null || String.IsNullOrEmpty(DirName))
					return false;
				string relDir = (String.IsNullOrEmpty(RelativeDir) ? "" : (RelativeDir + "\\")) + Util.PurifyDirName( DirName);
				var absDir = Project.BaseDirectory + "\\" + relDir;
				if (Directory.Exists(absDir) && Project.SubDirectories.Contains(relDir))
				{
					ErrorLogger.Log(new System.IO.IOException("Directory "+absDir+" already exists"));
					return false;
				}

				Project.SubDirectories.Add(relDir);
				Util.CreateDirectoryRecursively(absDir);
				Project.Save();
				//UpdateGUI();
				return true;
			}


			/// <summary>
			/// Creates a dialog which asks the user to add a file
			/// </summary>
			public static void AddExistingSourceToProject(Project Project, string RelativeDir)
			{
				var dlg = new OpenFileDialog();
				dlg.InitialDirectory = Project.BaseDirectory + "\\" + RelativeDir;
				dlg.Multiselect = true;

				if (dlg.ShowDialog().Value)
				{
					AddExistingSourceToProject(Project, RelativeDir,dlg.FileNames);
				}
			}

			public static void AddExistingSourceToProject(Project Project, string RelativeDir,params string[] Files)
			{
				var absPath = (Project.BaseDirectory + "\\" + RelativeDir).Trim('\\');
				foreach (var FileName in Files)
				{
					/*
					 * - Try to add the new file; if successful:
					 * - Physically copy the file if it's not in the target directory
					 */
					var newFile = absPath + "\\" + Path.GetFileName(FileName);

					if (Project.Add(newFile))
					{
						try
						{
							if (Path.GetDirectoryName(FileName) != absPath)
								File.Copy(FileName, newFile,true);
						}
						catch (Exception ex) { ErrorLogger.Log(ex); }
					}
				}
				if (Project.Save())
					UpdateGUI();
			}

			public static bool AddExistingDirectoryToProject(string DirectoryPath, Project Project, string RelativeDir)
			{
				/*
				 * - If dir not controlled by prj, add it
				 * - Copy the directory and all its children
				 * - Save project
				 */
				var newDir_rel =Path.Combine( RelativeDir, Path.GetFileName(DirectoryPath));
				var newDir_abs = Project.ToAbsoluteFileName(newDir_rel);

				// If project dir is a subdirectory of DirectoryPath, return false
				if (Project.BaseDirectory.Contains(DirectoryPath) || DirectoryPath==Project.BaseDirectory)
				{
					ErrorLogger.Log(new Exception("Project's base directory is part of "+DirectoryPath+" - can't add it"));
					return false;
				}

				if (!Project.SubDirectories.Contains(newDir_rel))
					Project.SubDirectories.Add(newDir_rel);

				Util.CreateDirectoryRecursively(newDir_abs);

				foreach (var file in Directory.GetFiles(DirectoryPath,"*",SearchOption.AllDirectories))
				{
					// Note: Empty directories will be ignored.
					var newFile_rel = file.Substring(DirectoryPath.Length).Trim('\\');
					var newFile_abs =newDir_abs + "\\" + newFile_rel;
					
					if (Project.Add(newFile_abs))
					{
						try
						{
							if(file!=newFile_abs)
								File.Copy(file, newFile_abs, true);
						}
						catch (Exception ex) {if(! ErrorLogger.Log(ex)) return false; }
					}
				}
				Project.Save();
				UpdateGUI();
				return true;
			}

			public static bool CopyFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{
				var tarprj = (Project == TargetProject || TargetProject == null) ? Project : TargetProject;
				/*
				 * - Build file paths
				 * - Try to copy file; if succesful:
				 * - Add to new project
				 * - Save target project
				 */
				var oldFile = Project.ToAbsoluteFileName(FileName);
				var newFile_rel =(NewDirectory + "\\" + Path.GetFileName(FileName)).Trim('\\');
				var newFile_abs = tarprj.ToAbsoluteFileName(newFile_rel);

				if(File.Exists(newFile_abs) || tarprj.ContainsFile(newFile_abs))
					return false;

				try{
					File.Copy(oldFile,newFile_abs);
				}catch(Exception ex){ErrorLogger.Log(ex); return false;}

				// Normally this should always return true since we've tested its non-existence before!
				tarprj.Add(newFile_abs);
				tarprj.Save();
				UpdateGUI();
				return true;
			}

			public static bool CopyDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				var srcDir_abs = Project.BaseDirectory + "\\" + RelativeDir;
				var destDir_abs = Path.Combine(TargetProject.BaseDirectory,NewDir,Path.GetFileName(RelativeDir));
				if (srcDir_abs == destDir_abs)
				{
					ErrorLogger.Log(new Exception("Source and destination are equal"));
					return false;
				}
				return AddExistingDirectoryToProject(srcDir_abs.Trim('\\'), TargetProject, NewDir);
			}

			public static bool MoveFile(Project Project, string FileName, Project TargetProject, string NewDirectory)
			{
				/*
				 * - Copy file
				 * - Delete old one from project
				 * - Delete old physically
				 */
				CanUpdateGUI = false;
				if (CopyFile(Project, FileName, TargetProject, NewDirectory) && Project.Remove(FileName))
				{
					var oldDir_rel = Path.GetDirectoryName(Project.ToRelativeFileName(FileName));
					Win32.MoveToRecycleBin(Project.ToAbsoluteFileName(FileName));

					// If directory empty, keep it managed by the project
					if (!Project.SubDirectories.Contains(oldDir_rel))
						Project.SubDirectories.Add(oldDir_rel);
					Project.Save();
				}
				CanUpdateGUI = true;
				UpdateGUI();
				return false;
			}

			public static bool MoveDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				/*
				 * - Exclude src dir from prj
				 * - Move dir
				 * - Add new dir to dest prj
				 */
				CanUpdateGUI = false;
				if(ExcludeDirectoryFromProject(Project,RelativeDir))
				{
					CanUpdateGUI = true;
					var srcDir_abs = Project.BaseDirectory + "\\" + RelativeDir;
					var destDir_abs =Path.Combine(TargetProject.BaseDirectory,NewDir,Path.GetFileName(RelativeDir));

					// Theoretically it's not needed to move the dir explicitly - it'd be done by AddExistingDirectoryToProject();
					// However it'd be needed to delete the source directory
					try
					{
						if(srcDir_abs!=destDir_abs)
							Directory.Move(srcDir_abs, destDir_abs);
					}
					catch (Exception ex) { ErrorLogger.Log(ex); return false; }

					return AddExistingDirectoryToProject(destDir_abs, TargetProject, NewDir);
				}
				CanUpdateGUI = true;
				return false;
			}

			public static bool ExcludeDirectoryFromProject(Project prj, string RelativePath)
			{
				if (prj == null || string.IsNullOrEmpty(RelativePath))
					return false;

				/*
				 * - Delete all subdirectory references
				 * - Delete all files that are inside of these directories
				 */
				var affectedFiles=(from f in prj.Files 
								   where Path.GetDirectoryName( f.FileName).Contains(RelativePath) 
								   select prj.ToAbsoluteFileName( f.FileName)).ToArray();

				foreach(var ed in Editors.Where(e=>affectedFiles.Contains(e.AbsoluteFilePath)))
					ed.Close();

				foreach (var s in prj.SubDirectories.Where(d=>d== RelativePath || d.Contains(RelativePath)).ToArray())
					prj.SubDirectories.Remove(s);

				foreach (var s in affectedFiles)
					prj.Remove(s);

				prj.Save();

				UpdateGUI();
				return true;
			}

			public static bool RemoveDirectoryFromProject(Project Project, string RelativePath)
			{
				if (Project== null || string.IsNullOrEmpty(RelativePath))
					return false;
				try
				{
					if (ExcludeDirectoryFromProject(Project, RelativePath))
						Win32.MoveToRecycleBin(Project.BaseDirectory + "\\" + RelativePath);
				}
				catch (Exception ex) { ErrorLogger.Log(ex); return false; }
				return true;
			}

			public static bool ExludeFileFromProject(Project Project, string file)
			{
				var absFile = Project.ToAbsoluteFileName(file);
				// Close (all) editor(s) that represent our file
				foreach (var ed in Editors.Where(e=>e.AbsoluteFilePath==absFile).ToArray())
					if(!ed.Close())
						return false;

				var r = Project.Remove(file);
				if (r)
				{
					Project.Save();
					MainWindow.UpdateProjectExplorer();
				}
				return r;
			}

			public static bool RemoveFileFromProject(Project Project, string file)
			{
				var r = ExludeFileFromProject(Project, file);
				try
				{
					if (r) Win32.MoveToRecycleBin(Project.ToAbsoluteFileName( file));
				}
				catch { }
				return r;
			}

			public static bool RenameFile(Project Project, string file, string NewFileName)
			{
				var absPath = Project.ToAbsoluteFileName(file);
				var newFilePath = Util.PurifyFileName(NewFileName);
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
			public static bool Build(Solution sln, bool Incrementally)
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
			static void AdjustLastFileList(string openedFile, bool IsPrj)
			{
				var l = IsPrj? GlobalProperties.Instance.LastProjects:GlobalProperties.Instance.LastFiles;
				if (l.Contains(openedFile))
					l.Remove(openedFile);
				l.Insert(0, openedFile);
				while (l.Count > 10)
					l.RemoveAt(10);
			}

			/// <summary>
			/// Central method to open a file OR a project
			/// </summary>
			/// <returns>Editor instance if a source file was opened</returns>
			public static AbstractEditorDocument OpenFile(string FileName)
			{
				/*
				 * 1. Solution check
				 * 2. Project file check
				 * 3. Normal file check
				 */
				var ext = Path.GetExtension(FileName);

				if (ext == Solution.SolutionExtension)
				{
					/*
					 * - Load solution
					 * - Load all of its projects
					 * - Open last opened files
					 */
					CurrentSolution = new Solution(FileName);

					AdjustLastFileList(FileName, true);

					foreach (var f in CurrentSolution.ProjectFiles)
						if(File.Exists(CurrentSolution.ToAbsoluteFileName( f)))
							CurrentSolution.ProjectCache.Add(Project.LoadProjectFromFile(CurrentSolution, f));

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(fn);

					MainWindow.UpdateGUIElements();
					MainWindow.Panel_ProjectExplorer.MainTree.ExpandAll();
					return null;
				}

				var langs = LanguageLoader.Bindings.Where(l => l.CanHandleProject(FileName)).ToArray();
				if (langs.Length > 0)
				{
					/* 
					 * - Load project
					 * - Create anonymous solution that holds the project virtually
					 * - Open last opened files
					 */
					var _oldSln = CurrentSolution;
					CurrentSolution = new Solution();
					CurrentSolution.FileName = Path.ChangeExtension(FileName, Solution.SolutionExtension);

					var LoadedPrj = langs[0].OpenProject(CurrentSolution, FileName);
					if (LoadedPrj != null)
					{
						AdjustLastFileList(FileName, true);
						CurrentSolution.Name = LoadedPrj.Name;
						CurrentSolution.AddProject(LoadedPrj);

						foreach (var prj in CurrentSolution)
							foreach (var fn in prj.LastOpenedFiles)
								OpenFile(prj.ToAbsoluteFileName(fn));
						MainWindow.UpdateGUIElements();
					}
					else CurrentSolution = _oldSln;
					return null;
				}

				// Try to resolve owner project
				// - useful if relative path was given - enables
				Project _prj =null;
				if (CurrentSolution != null)
					foreach (var p in CurrentSolution.ProjectCache)
						if (p.ContainsFile(FileName))
						{
							_prj = p;
							break;
						}

				// Check if file already open
				var absPath = _prj != null ? _prj.ToAbsoluteFileName(FileName) : FileName;
				foreach (var doc in DockMgr.Documents)
					if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).AbsoluteFilePath == absPath)
					{
						doc.Activate();
						return doc as AbstractEditorDocument;
					}

				AdjustLastFileList(absPath, false);

				var newEd = new EditorDocument(absPath);
				newEd.Show(DockMgr);
				newEd.Activate();
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
