using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using D_IDE.Core;
using System.IO;
using Parser.Core;

namespace D_IDE.D
{
	public class DBuildSupport:IBuildSupport
	{
		#region Properties
		readonly List<GenericError> TempBuildErrorList = new List<GenericError>();
		Process TempPrc = null;

		DVersion DMDVersion = DVersion.D2;
		DMDConfig CurrentDMDConfig
		{
			get { return DSettings.Instance.DMDConfig(DMDVersion); }
		}
		#endregion

		public BuildResult BuildProject(Project prj)
		{
			// Build outputs/target paths
			string objectDirectory = prj.BaseDirectory+"\\obj";
			Util.CreateDirectoryRecursively(objectDirectory);

			// Compile d sources to object files
			

			// Link files

			return null;
		}

		public BuildResult BuildSingleModule(string FileName, string OutputDirectory, bool Link)
		{
			TempBuildErrorList.Clear();
			var br = new BuildResult() { SourceFile=FileName};

			if (Link)
			{
				string exe=string.IsNullOrEmpty(OutputDirectory)?
					Path.ChangeExtension(FileName, ".exe"):
					OutputDirectory + "\\" + Path.GetFileName(FileName) + ".exe";

				br.TargetFile = exe;

				if (File.Exists(exe))
					File.Delete(exe);

				var dmd = DSettings.Instance.dmd2;
				var dmd_exe = dmd.SoureCompiler;

				// Always enable it to use environment paths to find dmd.exe
				if (!Path.IsPathRooted(dmd_exe) && Directory.Exists(dmd.BaseDirectory))
					dmd_exe = Path.Combine(dmd.BaseDirectory, dmd.SoureCompiler);

				TempPrc = FileExecution.ExecuteSilentlyAsync(
					dmd_exe,
					BuildDSourceCompileArgumentString(CurrentDMDConfig. SingleCompilationArguments, FileName, ""), // Compile our program always in debug mode
					Path.GetDirectoryName(FileName),
					OnOutput, OnError, OnExit
					);

				if (TempPrc != null && !TempPrc.HasExited)
					TempPrc.WaitForExit(10000);

				if (File.Exists(exe))
				{
					var br = CreatePDBFromExe(exe);


				}

				TempBuildErrorList.ToArray();
			}
		}

		/// <summary>
		/// Calls the cv2pdb program
		/// </summary>
		public BuildResult CreatePDBFromExe(string Executable)
		{
			var _p = TempBuildErrorList.ToArray();

			string pdb = Path.ChangeExtension(Executable, ".pdb");
			var br = new BuildResult() { SourceFile=Executable,TargetFile=pdb};

			if (!File.Exists(Executable))
			{
				br.BuildErrors = new[] { 
					new GenericError(){Message="Debug information database creation failed - "+Executable+" does not exist"}
					};
				return br;
			}

			bool res = false;

			if (File.Exists(pdb))
				File.Delete(pdb); // Enforce recreation of the database

			string cv2pdb = DSettings.Instance.cv2pdb_exe;

			// By default, check if there's a cv2pdb.exe at the program's main directory
			if (!Path.IsPathRooted(cv2pdb) && File.Exists(Util.ApplicationStartUpPath + "\\" + cv2pdb))
				cv2pdb = Util.ApplicationStartUpPath + "\\" + cv2pdb;

			IDEInterface.Log("Create debug information database " + pdb);
			try
			{
				var prc = FileExecution.ExecuteSilentlyAsync(cv2pdb, "\"" + Executable + "\"", Path.GetDirectoryName(Executable),
				OnOutput, OnError, delegate()
				{
					if (File.Exists(pdb))
					{
						IDEInterface.Log("Database created successfully");
						res = true;
					}
					else
						TempBuildErrorList.Add(new GenericError()
						{
							Message = "Database creation failed",
							FileName = pdb,
							Type = GenericError.ErrorType.Warning
						});
				});

				if (prc != null && !prc.HasExited)
					prc.WaitForExit(10000); // A time out of 10 seconds should be enough
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

			br.BuildErrors = TempBuildErrorList.ToArray();
			TempBuildErrorList.Clear();
			TempBuildErrorList.AddRange(_p);

			br.Successful = res;

			return br;
		}

		#region Message Callbacks
		void OnExit()
		{
			IDEInterface.Log("Process ended with code " + TempPrc.ExitCode.ToString());
		}

		static void OnOutput(string s)
		{
			IDEInterface.Log(s);
		}

		void OnError(string s)
		{
			int to = s.IndexOf(".d(");
			if (to < 0)
			{
				TempBuildErrorList.Add(new BuildError(s));
			}
			else
			{
				to += 2;
				string FileName = s.Substring(0, to);
				to += 1;
				int to2 = s.IndexOf("):", to);
				if (to2 < 0) return;
				int lineNumber = Convert.ToInt32(s.Substring(to, to2 - to));
				string errmsg = s.Substring(to2 + 2).Trim();

				TempBuildErrorList.Add(new BuildError(errmsg, FileName, new CodeLocation(0, lineNumber)));
			}
		}
		#endregion

		/// <summary>
		/// Builds an argument string for compiling a source to an object file
		/// </summary>
		/// <param name="input"></param>
		/// <param name="srcFile"></param>
		/// <param name="objDir"></param>
		/// <returns></returns>
		public static string BuildDSourceCompileArgumentString(string input, string srcFile, string objDir)
		{
			string obj =Path.Combine(objDir, Path.GetFileNameWithoutExtension(srcFile)+".obj");

			return ParseArgumentString(input, new Dictionary<string, string>{
				{"$src",srcFile},
				{"$objDir",objDir},
				{"$obj",obj},
				{"$filename",Path.GetFileNameWithoutExtension(srcFile)}
			});
		}

		/// <summary>
		/// Builds a string for linking several objects to one target file.
		/// Note: Additionally imported libraries must be passed to the 'Objects' parameter
		/// </summary>
		/// <param name="input"></param>
		/// <param name="targetFile"></param>
		/// <param name="Objects"></param>
		/// <returns></returns>
		public static string BuildDLinkerArgumentString(string input, string targetFile, params string[] Objects)
		{
			string objs = "";

			if(Objects!=null && Objects.Length>0)
				objs="\"" + string.Join("\" \"", Objects) + "\"";

			return ParseArgumentString(input, new Dictionary<string, string>{
				{"$objs",objs},
				{"$targetDir",Path.GetDirectoryName(targetFile)},
				{"$target",targetFile},
				{"$exe",Path.ChangeExtension(targetFile,".exe")},
				{"$dll",Path.ChangeExtension(targetFile,".dll")},
				{"$lib",Path.ChangeExtension(targetFile,".lib")},
			});
		}

		public static string ParseArgumentString(string input, IEnumerable<KeyValuePair<string, string>> ReplacedStrings)
		{
			string ret = input;

			foreach (var kv in ReplacedStrings)
				ret = ret.Replace(kv.Key,kv.Value);

			return ret;
		}
	}
}
