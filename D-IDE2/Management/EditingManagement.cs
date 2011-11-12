using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using System.Windows.Threading;
using System.Threading;

namespace D_IDE
{
	partial class IDEManager
	{
		public class EditingManagement
		{
			static bool allDocsReadOnly;
			/// <summary>
			/// Make all documents read only
			/// </summary>
			public static bool AllDocumentsReadOnly
			{
				get { return allDocsReadOnly; }
				set
				{
					allDocsReadOnly = value;

					// Ensure thread-safety
					if (Util.IsDispatcherThread)
						foreach (var ed in from e in Instance.Editors where e is EditorDocument select e as EditorDocument)
							ed.Editor.IsReadOnly = value;
					else 
						IDEManager.Instance.MainWindow.Dispatcher.Invoke(new Action(() => {
						foreach (var ed in from e in Instance.Editors where e is EditorDocument select e as EditorDocument)
							ed.Editor.IsReadOnly = value;
					}));
				}
			}

			/// <summary>
			/// Adds a new entry to the globally shared LastFiles/LastProjects-List
			/// </summary>
			/// <param name="openedFile">The file which got opened recently</param>
			/// <param name="IsPrj">Add it to the LastProjects List?</param>
			public static void AdjustLastFileList(string openedFile, bool IsPrj)
			{
				var l = IsPrj ? GlobalProperties.Instance.LastProjects : GlobalProperties.Instance.LastFiles;
				if (l.Contains(openedFile))
					l.Remove(openedFile);
				l.Insert(0, openedFile);
				while (l.Count > 10)
					l.RemoveAt(l.Count-1);
			}

			/// <summary>
			/// Central method to open a file/project/solution
			/// </summary>
			/// <returns>Editor instance (if a source file was opened)</returns>
			public static AbstractEditorDocument OpenFile(string FileName)
			{
				if (string.IsNullOrWhiteSpace(FileName))
					return null;
				/*
				 * 1) Solution check
				 * 2) Project file check
				 * 3) Normal file check
				 */
				var ext = Path.GetExtension(FileName);

				// 1)
				if (ext == Solution.SolutionExtension)
				{
					if (!File.Exists(FileName))
					{
						ErrorLogger.Log("Solution "+FileName+" not found!",ErrorType.Error,ErrorOrigin.System);
						return null;
					}

					// Before load a new solution, close all related edited files
					if(CurrentSolution!=null)
						CloseFilesRelatedTo(CurrentSolution);

					/*
					 * - Load solution
					 * - Load all of its projects
					 * - Open last opened files
					 */
					var sln =CurrentSolution = new Solution(FileName);

					AdjustLastFileList(FileName, true);

					foreach (var f in sln.ProjectFiles)
						if (File.Exists(sln.ToAbsoluteFileName(f)))
							Project.LoadProjectFromFile(sln, f);

					foreach (var prj in sln)
						if (prj != null && prj.LastOpenedFiles.Count > 0)
						    foreach (var fn in prj.LastOpenedFiles)
							    OpenFile(fn);

					Instance.UpdateGUI();
					Instance.MainWindow.Panel_ProjectExplorer.MainTree.ExpandAll();
					return null;
				}

				// 2)
				var langs = LanguageLoader.Bindings.Where(l => l.CanHandleProject(FileName)).ToArray();
				if (langs.Length > 0)
				{
					if (!File.Exists(FileName))
					{
						ErrorLogger.Log("Project " + FileName + " not found!", ErrorType.Error, ErrorOrigin.System);
						return null;
					}

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
							if(prj!=null && prj.LastOpenedFiles.Count>0)
							foreach (var fn in prj.LastOpenedFiles)
								OpenFile(prj.ToAbsoluteFileName(fn));
						Instance.UpdateGUI();
					}
					else CurrentSolution = _oldSln;
					return null;
				}

				//3)

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

                // Make file path absolute
				var absPath = _prj != null ? _prj.ToAbsoluteFileName(FileName) : FileName;

                // Add file to recently used files
                AdjustLastFileList(absPath, false);

                // Check if file already open -- Allow only one open instance of a file!
				foreach (var doc in Instance.MainWindow.DockManager.Documents)
					if (doc is AbstractEditorDocument && (doc as AbstractEditorDocument).AbsoluteFilePath == absPath)
					{
						// Activate the wanted item and return it
						doc.Activate();
						IDEManager.Instance.MainWindow.DockManager.ActiveDocument = doc;
						return doc as AbstractEditorDocument;
					}

				EditorDocument newEd = null;

				foreach(var lang in LanguageLoader.Bindings)
					if (lang.SupportsEditor(absPath))
					{
						newEd = lang.OpenFile(_prj, absPath);
						break;
					}

				if (newEd == null)
					newEd = new EditorDocument(absPath);

				// Set read only state if e.g. debugging currently
				newEd.Editor.IsReadOnly = AllDocumentsReadOnly;
				newEd.Show(Instance.MainWindow.DockManager);

				try
				{
					Instance.MainWindow.DockManager.ActiveDocument = newEd;
					newEd.Activate();
				}
				catch
				{
					
				}
				Instance.UpdateGUI();
				newEd.Editor.Focus();
				return newEd;
			}

			/// <summary>
			/// Opens a file and moves caret to Line,Col. Scrolls down the view if needed.
			/// </summary>
			public static AbstractEditorDocument OpenFile(string FileName, int Line, int Col)
			{
				var ret = OpenFile(FileName);
				var ed = ret as EditorDocument;

				if (ed == null)
					return ret;

				if (Line >= ed.Editor.Document.LineCount && Col >= ed.Editor.Document.Lines[ed.Editor.Document.LineCount].Length)
					ed.Editor.CaretOffset = ed.Editor.Document.TextLength;
				else
					ed.Editor.CaretOffset = ed.Editor.Document.GetOffset(Line, Col);

				ed.Editor.ScrollTo(Line,Col);
				ed.Editor.Focus();

				return ed;
			}

			public static AbstractEditorDocument OpenFile(string FileName, int Offset)
			{
				var ret = OpenFile(FileName);
				var ed = ret as EditorDocument;

				if (ed == null)
					return ret;

				if (Offset > ed.Editor.Document.TextLength)
					Offset = ed.Editor.Document.TextLength;

				if (Offset >= 0)
				{
					ed.Editor.CaretOffset = Offset;
					var loc = ed.Editor.Document.GetLocation(Offset);
					ed.Editor.ScrollTo(loc.Line, loc.Column);
					/*
					var visLine=ed.Editor.TextArea.TextView.GetOrConstructVisualLine(ed.Editor.Document.GetLineByNumber(loc.Line));
					if(visLine!=null)
						ed.Editor.ScrollToVerticalOffset(visLine.VisualTop-50);*/
				}
				ed.Editor.Focus();

				return ed;
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


			public static void CloseFilesRelatedTo(Solution Solution)
			{
				if(Solution!=null)
					foreach (var prj in Solution)
						CloseFilesRelatedTo(prj);
			}

			public static void CloseFilesRelatedTo(Project Project)
			{
				if (Project == null)
					return;

				foreach (var ed in IDEManager.Instance.Editors)
					if (ed.HasProject && ed.Project == Project)
						ed.Close();
			}
		}
	}
}
