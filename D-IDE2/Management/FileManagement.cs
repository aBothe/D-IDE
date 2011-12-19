using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using Microsoft.Win32;
using D_IDE.Dialogs;
using System.Windows;

namespace D_IDE
{
	partial class IDEManager
	{
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
					var absFile = Project.BaseDirectory + "\\" + file;

					if (File.Exists(absFile))
						return false;

					if (Project.Add(file)!=null)
					{
						// Set standard encoding to UTF8
						File.WriteAllText(absFile, "",Encoding.UTF8);
						Project.Save();
						Instance.UpdateGUI();
						return true;
					}
				}
				return false;
			}

			public static bool AddNewDirectoryToProject(Project Project, string RelativeDir, string DirName)
			{
				if (Project == null || String.IsNullOrEmpty(DirName))
					return false;
				string relDir = (String.IsNullOrEmpty(RelativeDir) ? "" : (RelativeDir + "\\")) + Util.PurifyDirName(DirName);
				var absDir = Project.BaseDirectory + "\\" + relDir;
				if (Directory.Exists(absDir) && Project.SubDirectories.Contains(relDir))
				{
					MessageBox.Show("Directory " + absDir + " already exists","Error");
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
					AddExistingSourceToProject(Project, RelativeDir, dlg.FileNames);
				}
			}

			public static void AddExistingSourceToProject(Project Project, string RelativeDir, params string[] Files)
			{
				var absPath = (Project.BaseDirectory + "\\" + RelativeDir).Trim('\\');
				foreach (var FileName in Files)
				{
					/*
					 * - Try to add the new file; if successful:
					 * - Physically copy the file if it's not in the target directory
					 */
					var newFile = absPath + "\\" + Path.GetFileName(FileName);

					if (Project.Add(newFile)!=null)
					{
						try
						{
							if (Path.GetDirectoryName(FileName) != absPath)
								File.Copy(FileName, newFile, true);
						}
						catch (Exception ex) { ErrorLogger.Log(ex); }
					}
				}
				if (Project.Save())
					Instance.UpdateGUI(); ;
			}

			public static bool AddExistingDirectoryToProject(string DirectoryPath, Project Project, string RelativeDir)
			{
				/*
				 * - If dir not controlled by prj, add it
				 * - Copy the directory and all its children
				 * - Save project
				 */
				var newDir_rel = Path.Combine(RelativeDir, Path.GetFileName(DirectoryPath));
				var newDir_abs = Project.ToAbsoluteFileName(newDir_rel);

				// If project dir is a subdirectory of DirectoryPath, return false
				if (Project.BaseDirectory.Contains(DirectoryPath) || DirectoryPath == Project.BaseDirectory)
				{
					MessageBox.Show("Project's base directory is part of " + DirectoryPath + " - cannot add it","File addition error");
					return false;
				}

				if (!Project.SubDirectories.Contains(newDir_rel))
					Project.SubDirectories.Add(newDir_rel);

				Util.CreateDirectoryRecursively(newDir_abs);

				foreach (var file in Directory.GetFiles(DirectoryPath, "*", SearchOption.AllDirectories))
				{
					// Note: Empty directories will be ignored.
					var newFile_rel = file.Substring(DirectoryPath.Length).Trim('\\');
					var newFile_abs = newDir_abs + "\\" + newFile_rel;

					if (Project.Add(newFile_abs)!=null)
					{
						try
						{
							if (file != newFile_abs)
								File.Copy(file, newFile_abs, true);
						}
						catch (Exception ex) { 
							ErrorLogger.Log(ex);
							if(MessageBox.Show("Stop adding files?","Error while adding files",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
								return false; 
						}
					}
				}
				Project.Save();
				Instance.UpdateGUI();
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
				var newFile_rel = (NewDirectory + "\\" + Path.GetFileName(FileName)).Trim('\\');
				var newFile_abs = tarprj.ToAbsoluteFileName(newFile_rel);

				if (File.Exists(newFile_abs) || tarprj.ContainsFile(newFile_abs))
					return false;

				try
				{
					File.Copy(oldFile, newFile_abs);
				}
				catch (Exception ex) { ErrorLogger.Log(ex); return false; }

				// Normally this should always return true since we've tested its non-existence before!
				tarprj.Add(newFile_abs);
				tarprj.Save();
				Instance.UpdateGUI();
				return true;
			}

			public static bool CopyDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				var srcDir_abs = Project.BaseDirectory + "\\" + RelativeDir;
				var destDir_abs = Path.Combine(TargetProject.BaseDirectory, NewDir, Path.GetFileName(RelativeDir));
				if (srcDir_abs == destDir_abs)
				{
					MessageBox.Show("Source and destination are equal","File copying error");
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
				Instance.CanUpdateGUI = false;
				if (CopyFile(Project, FileName, TargetProject, NewDirectory) && Project.Remove(FileName))
				{
					var oldDir_rel = Path.GetDirectoryName(Project.ToRelativeFileName(FileName));
					Win32.MoveToRecycleBin(Project.ToAbsoluteFileName(FileName));

					// If directory empty, keep it managed by the project
					if (!Project.SubDirectories.Contains(oldDir_rel))
						Project.SubDirectories.Add(oldDir_rel);
					Project.Save();
				}
				Instance.CanUpdateGUI = true;
				Instance.UpdateGUI();
				return false;
			}

			public static bool MoveDirectory(Project Project, string RelativeDir, Project TargetProject, string NewDir)
			{
				/*
				 * - Exclude src dir from prj
				 * - Move dir
				 * - Add new dir to dest prj
				 */
				Instance.CanUpdateGUI = false;
				if (ExcludeDirectoryFromProject(Project, RelativeDir))
				{
					Instance.CanUpdateGUI = true;
					var srcDir_abs = Project.BaseDirectory + "\\" + RelativeDir;
					var destDir_abs = Path.Combine(TargetProject.BaseDirectory, NewDir, Path.GetFileName(RelativeDir));

					// Theoretically it's not needed to move the dir explicitly - it'd be done by AddExistingDirectoryToProject();
					// However it'd be needed to delete the source directory
					try
					{
						if (srcDir_abs != destDir_abs)
							Directory.Move(srcDir_abs, destDir_abs);
					}
					catch (Exception ex) { ErrorLogger.Log(ex); return false; }

					return AddExistingDirectoryToProject(destDir_abs, TargetProject, NewDir);
				}
				Instance.CanUpdateGUI = true;
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
				var affectedFiles = (from f in prj.Files
									 where Path.GetDirectoryName(f.FileName).Contains(RelativePath)
									 select prj.ToAbsoluteFileName(f.FileName)).ToArray();

				foreach (var ed in Instance.Editors.Where(e => affectedFiles.Contains(e.AbsoluteFilePath)))
					ed.Close();

				foreach (var s in prj.SubDirectories.Where(d => d == RelativePath || d.Contains(RelativePath)).ToArray())
					prj.SubDirectories.Remove(s);

				foreach (var s in affectedFiles)
					prj.Remove(s);

				prj.Save();

				Instance.UpdateGUI();
				return true;
			}

			public static bool RemoveDirectoryFromProject(Project Project, string RelativePath)
			{
				if (Project == null || string.IsNullOrEmpty(RelativePath))
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
				foreach (var ed in Instance.Editors.Where(e => e.AbsoluteFilePath == absFile).ToArray())
					if (!ed.Close())
						return false;

				var r = Project.Remove(file);
				if (r)
				{
					Project.Save();
					Instance.MainWindow.RefreshProjectExplorer();
				}
				return r;
			}

			public static bool RemoveFileFromProject(Project Project, string file)
			{
				var r = ExludeFileFromProject(Project, file);
				try
				{
					if (r) Win32.MoveToRecycleBin(Project.ToAbsoluteFileName(file));
				}
				catch { }
				return r;
			}

			public static bool RenameFile(Project Project, string file, string NewFileName)
			{
				var absPath = Project.ToAbsoluteFileName(file);
				var newFilePath =Path.Combine(Project.BaseDirectory, Path.GetDirectoryName(NewFileName),Util.PurifyFileName(Path.GetFileName(NewFileName)));
				var ret = Util.MoveFile(absPath, newFilePath);
				if (ret)
				{
					Project.Remove(file);
					Project.Add(newFilePath);
					Project.Save();

					foreach (var e in Instance.Editors)
						if (e.AbsoluteFilePath == absPath)
						{
							e.AbsoluteFilePath = newFilePath;
							e.Reload();
						}
				}
				return ret;
			}

			public static bool RenameDirectory(Project Project, string dir, string NewDirName)
			{
				return true;
			}
		}
	}
}
