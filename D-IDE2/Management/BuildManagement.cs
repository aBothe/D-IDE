using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using System.Threading;

namespace D_IDE
{
	partial class IDEManager
	{
		public class BuildManagement
		{
			public static bool IsBuilding { get; protected set; }
			static AbstractBuildSupport curBuildSupp = null;

			/// <summary>
			/// Stops all build processes.
			/// </summary>
			public static void StopBuilding()
			{
				if (curBuildSupp != null)
					curBuildSupp.StopBuilding();
			}

			/// <summary>
			/// Builds the current solution incrementally.
			/// If no solution present, the current module will be built to a standalone executable.
			/// </summary>
			public static bool Build()
			{
				if (CurrentSolution != null)
					return Build(CurrentSolution, true);

				var br= BuildSingle();
				if (br != null)
					return br.Successful;

				return false;
			}

			public static void BuildAsync(bool BuildSingle=false,Action<bool> OnFinished=null)
			{
				var t = new Thread(() =>
				{

				});

				t.IsBackground = true;
				t.Start();
			}

			public static bool Build(Solution sln, bool Incrementally)
			{
				//TODO: Build from most independent to most dependent project
				/*
				 * TODO: How to combine D Projects?
				 * -- e.g. Project A is a class library
				 * -- Project B is an executable --> Interfacing?
				 * http://digitalmars.com/d/2.0/dll.html
				 */

				bool ret = true;
				// Enable build menu
				IsBuilding = true;
				IDEManager.Instance.MainWindow.RefreshMenu();

				IDEManager.Instance.MainWindow.LeftStatusText = "Build "+sln.Name;

				// Iterate through all projects
				// Note: see above
				foreach (var prj in sln)
				{
					if (!InternalBuild(prj, Incrementally))
					{
						ret = false;
						break;
					}
				}

				IDEManager.Instance.MainWindow.LeftStatusText = ret ? "Build successful" : "Build failed";

				// Disable build menu
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();
				ErrorManagement.RefreshErrorList();
				return ret;
			}

			public static bool Build(Project Project, bool Incrementally)
			{
				// Enable build menu
				IsBuilding = true;
				IDEManager.EditingManagement.SaveAllFiles();
				IDEManager.Instance.MainWindow.RefreshMenu();
				IDEManager.Instance.MainWindow.LeftStatusText = "Build " + Project.Name;
				IDEManager.Instance.MainWindow.ClearLog();

				// Build project with the interal method that's dedicated to build a project
				var r = InternalBuild(Project, Incrementally);

				// Disable build menu, Refresh error list
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();
				IDEManager.Instance.MainWindow.LeftStatusText = r ? "Build successful" : "Build failed";
				ErrorManagement.RefreshErrorList();
				return r;
			}

			static bool InternalBuild(Project Project, bool Incrementally)
			{
				// Important: Reset error list's unbound build result
				ErrorManagement.LastSingleBuildResult = null;

				// Select appropriate language binding
				bool isPrj = false;
				var lang = AbstractLanguageBinding.SearchBinding(Project.FileName, out isPrj);

				// If binding is able to build..
				if (lang != null && isPrj && lang.CanBuild && lang.BuildSupport.CanBuildProject(Project))
				{
					// If not building the project incrementally, cleanup project outputs first
					if(!Incrementally)
						CleanUpOutput(Project);

					// Set debug support
					if (lang.CanUseDebugging)
						DebugManagement.CurrentDebugSupport = lang.DebugSupport;
					else 
						DebugManagement.CurrentDebugSupport = null;

					// Increment build number if the user enabled it
					if (Project.AutoIncrementBuildNumber)
						if (Project.LastBuildResult!=null && Project.LastBuildResult.Successful)
							Project.Version.IncrementBuild();
						else //TODO: How to handle revision number?
							Project.Version.Revision++;

					// Build project
					lang.BuildSupport.BuildProject(Project);

					// Copy additional output files to target dir
					if (Project.LastBuildResult.Successful)
						Project.CopyCopyableOutputFiles();

					if (Project.LastBuildResult.NoBuildNeeded)
						ErrorLogger.Log("No project files were changed -- No compilation required", ErrorType.Information, ErrorOrigin.Build);

					return Project.LastBuildResult.Successful;
				}
				return false;
			}

			/// <summary>
			/// Builds currently edited document to single executable
			/// </summary>
			public static bool BuildSingle(out string CreatedExecutable)
			{
				CreatedExecutable=null;
				var succ = BuildSingle();
				if (succ != null)
				{
					CreatedExecutable = succ.TargetFile;
					return succ.Successful;
				}

				return false;
			}

			public static BuildResult BuildSingle()
			{
				AbstractEditorDocument ed = ed = Instance.CurrentEditor;

				if (ed == null)
					return null;

				// Save module
				ed.Save();

				// Select appropriate language binding
				string file = ed.AbsoluteFilePath;
				bool IsProject = false;
				var lang = AbstractLanguageBinding.SearchBinding(file, out IsProject);

				// Check if binding supports building
				if (lang == null || IsProject || !lang.CanBuild || !lang.BuildSupport.CanBuildFile(file))
					return null;

				// Set debug support
				if (lang.CanUseDebugging)
					DebugManagement.CurrentDebugSupport = lang.DebugSupport;
				else
					DebugManagement.CurrentDebugSupport = null;

				// Enable build menu
				IsBuilding = true;

				IDEManager.Instance.MainWindow.RefreshMenu();

				// Clear build output
				Instance.MainWindow.ClearLog();

				// Execute build
				var br = ErrorManagement.LastSingleBuildResult = lang.BuildSupport.BuildStandAlone(file);

				if (br.Successful && br.NoBuildNeeded)
					ErrorLogger.Log("File wasn't changed -- No compilation required", ErrorType.Information, ErrorOrigin.Build);


				// Update error list, Disable build menu
				ErrorManagement.RefreshErrorList();
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();

				return br;
			}

			public static void CleanUpOutput(Solution sln)
			{
				foreach (var prj in sln)
					CleanUpOutput(prj);
			}

			/// <summary>
			/// Deletes all project outputs
			/// </summary>
			public static void CleanUpOutput(Project Project)
			{
				// Delete target file
				DeleteTargets(Project.LastBuildResult);

				// Delete all compiled sources / copied files
				foreach (var src in Project.Files)
					DeleteTargets(src.LastBuildResult);
			}

			/// <summary>
			/// Deletes all ouput files of a build result
			/// </summary>
			static void DeleteTargets(BuildResult r)
			{
				if (r == null)
					return;
				try
				{
					if (File.Exists(r.TargetFile))
						File.Delete(r.TargetFile);

					if (r.AdditionalFiles != null)
						foreach (var f in r.AdditionalFiles)
							if (File.Exists(f))
								File.Delete(f);
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
			}
		}
	}
}
