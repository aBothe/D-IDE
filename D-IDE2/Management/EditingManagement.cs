using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;

namespace D_IDE
{
	partial class IDEManager
	{
		public class EditingManagement
		{
			static void AdjustLastFileList(string openedFile, bool IsPrj)
			{
				var l = IsPrj ? GlobalProperties.Instance.LastProjects : GlobalProperties.Instance.LastFiles;
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
						if (File.Exists(CurrentSolution.ToAbsoluteFileName(f)))
							CurrentSolution.ProjectCache.Add(Project.LoadProjectFromFile(CurrentSolution, f));

					foreach (var prj in CurrentSolution)
						foreach (var fn in prj.LastOpenedFiles)
							OpenFile(fn);

					Instance.UpdateGUI();
					Instance.MainWindow.Panel_ProjectExplorer.MainTree.ExpandAll();
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
						Instance.UpdateGUI();
					}
					else CurrentSolution = _oldSln;
					return null;
				}

				// Try to resolve owner project
				// - useful if relative path was given - enables
				Project _prj = null;
				if (CurrentSolution != null)
					foreach (var p in CurrentSolution.ProjectCache)
						if (p.ContainsFile(FileName))
						{
							_prj = p;
							break;
						}

				// Check if file already open
				var absPath = _prj != null ? _prj.ToAbsoluteFileName(FileName) : FileName;
				foreach (var doc in Instance.MainWindow.DockManager.Documents)
					if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).AbsoluteFilePath == absPath)
					{
						doc.Activate();
						return doc as AbstractEditorDocument;
					}

				AdjustLastFileList(absPath, false);

				var newEd = new EditorDocument(absPath);
				newEd.Show(Instance.MainWindow.DockManager);
				newEd.Activate();
				Instance.UpdateGUI();
				return newEd;
			}

			public static void SaveCurrentFile()
			{
				if (Instance.CurrentEditor != null)
					Instance.CurrentEditor.Save();
			}

			public static void SaveAllFiles()
			{
				foreach (var doc in Instance.MainWindow.DockManager.Documents)
					if (doc is AbstractEditorDocument)
						(doc as AbstractEditorDocument).Save();

				if (CurrentSolution != null)
				{
					CurrentSolution.Save();

					foreach (var p in CurrentSolution)
					{
						p.LastOpenedFiles.Clear();
						// Store last opened files
						foreach (var ed in Instance.Editors)
						{
							if (p.ContainsFile(ed.AbsoluteFilePath))
								p.LastOpenedFiles.Add(ed.FileName);
						}

						p.Save();
					}
				}
			}

			/// <summary>
			/// Saves the file under a new file name.
			/// Copies the file and does not affect the project.
			/// </summary>
			public static void SaveCurrentFileAs(string NewFilePath)
			{
				if (Instance.CurrentEditor == null) return;
				/*
				if (Instance.CurrentEditor.Project != null)
					IDEManager.FileManagement.RenameFile(
						Instance.CurrentEditor.Project,
						Instance.CurrentEditor.FileName, NewFilePath);
				*/
				Instance.CurrentEditor.FileName = NewFilePath;
				Instance.CurrentEditor.Save();
			}
		}
	}
}
