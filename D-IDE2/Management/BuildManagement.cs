﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;

namespace D_IDE
{
	partial class IDEManager
	{
		public class BuildManagement
		{
			public static bool IsBuilding { get; protected set; }
			static IBuildSupport curBuildSupp = null;
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

				return BuildSingle();
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
				IsBuilding = true;
				IDEManager.Instance.MainWindow.RefreshMenu();
				foreach (var prj in sln)
				{
					if (!InternalBuild(prj, Incrementally))
					{
						ret = false;
						break;
					}
				}
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();
				return ret;
			}

			public static bool Build(Project Project, bool Incrementally)
			{
				IsBuilding = true;
				IDEManager.Instance.MainWindow.RefreshMenu();
				var r = InternalBuild(Project, Incrementally);
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();
				return r;
			}

			static bool InternalBuild(Project Project, bool Incrementally)
			{
				// Save all files that belong to Project
				foreach (var ed in Instance.Editors.Where(e=>Project.ContainsFile(e.AbsoluteFilePath)))
					ed.Save();

				Instance.MainWindow.ClearLog();
				// Important: Reset error list's unbound build result
				ErrorManagement.LastSingleBuildResult = null;

				bool isPrj = false;
				var lang = AbstractLanguageBinding.SearchBinding(Project.FileName, out isPrj);

				if (lang != null && isPrj && lang.CanBuild)
				{
					if (Project.AutoIncrementBuildNumber)
						if (Project.LastBuildResult!=null && Project.LastBuildResult.Successful)
							Project.Version.IncrementBuild();
						else
							Project.Version.Revision++;

					lang.BuildSupport.BuildProject(Project);

					if (Project.LastBuildResult.Successful)
						Project.CopyCopyableOutputFiles();

					ErrorManagement.RefreshErrorList();

					return Project.LastBuildResult.Successful;
				}
				return false;
			}

			public static bool BuildSingle()
			{
				string _u = null;
				return BuildSingle(out _u);
			}

			/// <summary>
			/// Builds currently edited document to single executable
			/// </summary>
			/// <returns></returns>
			public static bool BuildSingle(out string CreatedExecutable)
			{
				CreatedExecutable = null;
				var ed = Instance.CurrentEditor;
				if (ed == null)
					return false;

				ed.Save();

				string file = ed.AbsoluteFilePath;
				bool IsProject = false;
				var lang = AbstractLanguageBinding.SearchBinding(file, out IsProject);

				if (lang == null || IsProject || !lang.CanBuild)
					return false;

				IsBuilding = true;
				IDEManager.Instance.MainWindow.RefreshMenu();

				Instance.MainWindow.ClearLog();

				var br = ErrorManagement.LastSingleBuildResult = lang.BuildSupport.BuildStandAlone(file);
				CreatedExecutable = br.TargetFile;

				ErrorManagement.RefreshErrorList();
				IsBuilding = false;
				IDEManager.Instance.MainWindow.RefreshMenu();
				return br.Successful;
			}

			public static void CleanUpOutput(Solution sln)
			{
				foreach (var prj in sln)
					CleanUpOutput(prj);
			}

			public static void CleanUpOutput(Project Project)
			{
				// Delete target file
				DeleteTargets(Project.LastBuildResult);

				// Delete all compiled sources / copied files
				foreach (var src in Project.Files)
					DeleteTargets(src.LastBuildResult);
			}

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
