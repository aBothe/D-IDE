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
		Process TempPrc = null;
		bool ShallStop = false;

		public DVersion DMDVersion = DVersion.D2;
		public bool CompileRelease = true;
		public DMDConfig CurrentDMDConfig
		{
			get { return DSettings.Instance.DMDConfig(DMDVersion); }
		}
		#endregion

		public override BuildResult BuildProject(Project prj)
		{
			// Build outputs/target paths
			string objectDirectory = prj.BaseDirectory+"\\obj";
			Util.CreateDirectoryRecursively(objectDirectory);

			// Compile d sources to object files
			

			// Link files
			return null;
		}

		public BuildResult CompileSource(DMDConfig dmd,bool DebugCompile, string srcFile, string objFile, string execDirectory)
		{
			var br = new BuildResult() { SourceFile = srcFile,TargetFile=objFile,Successful=true };

			if (GlobalProperties.Instance.VerboseBuildOutput)
				IDEInterface.Log("Compile " + srcFile);

			var dmd_exe = dmd.SoureCompiler;

			// Always enable it to use environment paths to find dmd.exe
			if (!Path.IsPathRooted(dmd_exe) && Directory.Exists(dmd.BaseDirectory))
				dmd_exe = Path.Combine(dmd.BaseDirectory, dmd.SoureCompiler);

			TempPrc = FileExecution.ExecuteSilentlyAsync(dmd_exe,
					BuildDSourceCompileArgumentString(dmd.BuildArguments(DebugCompile).SoureCompiler, srcFile, objFile), // Compile our program always in debug mode
					execDirectory,
					OnOutput, delegate(string s) {
						var err = ParseErrorMessage(s);
						if (err.Type == GenericError.ErrorType.Error)
							br.Successful = false;
						br.BuildErrors.Add(err);
					}, OnExit);

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.WaitForExit(10000);

			br.Successful = br.Successful && File.Exists(objFile);
			return br;
		}

		/// <summary>
		/// Links several object files to an executable, dynamic or static library
		/// </summary>
		public BuildResult LinkFiles(string linkerExe, string linkerArgs, string startDirectory, string targetFile, bool CreatePDB, params string[] files)
		{
			var errList = new List<GenericError>();
			var br = new BuildResult() { TargetFile=targetFile};

			TempPrc = FileExecution.ExecuteSilentlyAsync(
					linkerExe,	BuildDLinkerArgumentString(linkerArgs,targetFile,files), startDirectory,
					OnOutput, delegate(string s)
			{
				var err = ParseErrorMessage(s);
				if (err.Type == GenericError.ErrorType.Error)
					br.Successful = false;
				br.BuildErrors.Add(err);
			}, OnExit);

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.WaitForExit(10000);

			br.Successful = File.Exists(targetFile);

			// If targetFile is executable or library, create PDB
			if (CreatePDB && (targetFile.EndsWith(".exe") || targetFile.EndsWith(".dll")))
			{
				var br_=CreatePDBFromExe(targetFile);
				br.BuildErrors.AddRange(br_.BuildErrors);

				br.AdditionalFiles=new[]{br_.TargetFile};
			}

			return br;
		}

		public override BuildResult BuildModule(string FileName, string OutputDirectory, bool Link)
		{
			var dmd = CurrentDMDConfig;
			var dmd_exe = dmd.SoureCompiler;
			bool compileDebug = true;

			var outputDir = Path.IsPathRooted(OutputDirectory) ? OutputDirectory : Path.Combine(Path.GetDirectoryName(FileName), OutputDirectory);
			
			// Compile .d source file to obj
			var obj = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(FileName) + ".obj");
			var br = CompileSource(dmd,compileDebug,FileName,obj,Path.GetDirectoryName(FileName));

			if (Link && br.Successful)
			{
				br.Successful = false;
				#region Link To StandAlone executable
				var exe = OutputDirectory + "\\" + Path.GetFileNameWithoutExtension(FileName) + ".exe";
				br.TargetFile = exe;

				var br_ = LinkFiles(dmd.ExeLinker, dmd.BuildArguments(compileDebug).ExeLinker, Path.GetDirectoryName(obj), exe,compileDebug, obj);
				
				br.BuildErrors.AddRange(br_.BuildErrors);
				br.AdditionalFiles = br_.AdditionalFiles;
				br.Successful = br_.Successful;
				#endregion
			}

			return br;			
		}

		/// <summary>
		/// Calls the cv2pdb program
		/// </summary>
		public BuildResult CreatePDBFromExe(string Executable)
		{
			var pdb = Path.ChangeExtension(Executable, ".pdb");
			var br = new BuildResult() { SourceFile=Executable,TargetFile=pdb};

			if (!File.Exists(Executable))
			{
				br.BuildErrors.Add(new GenericError(){Message="Debug information database creation failed - "+Executable+" does not exist"});
				return br;
			}

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
				OnOutput, delegate(string s){
					br.BuildErrors.Add(new GenericError(){Message=s});	
				}, delegate()
				{
					if (File.Exists(pdb))
					{
						IDEInterface.Log("Debug information database created successfully");
						br.Successful = true;
					}
					else
						br.BuildErrors.Add(new GenericError()
						{
							Message = "Debug information database creation failed",
							Type = GenericError.ErrorType.Warning
						});
				});

				if (prc != null && !prc.HasExited)
					prc.WaitForExit(10000); // A time out of 10 seconds should be enough
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }

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

		public static BuildError ParseErrorMessage(string s)
		{
			int to = s.IndexOf(".d(");
			if (to>0)
			{
				to += 2;
				string FileName = s.Substring(0, to);
				to += 1;
				int to2 = s.IndexOf("):", to);
				if (to2 > 0)
				{
					int lineNumber = Convert.ToInt32(s.Substring(to, to2 - to));
					string errmsg = s.Substring(to2 + 2).Trim();

					return new BuildError(errmsg, FileName, new CodeLocation(0, lineNumber));
				}
			}
			return new BuildError(s);
		}
		#endregion

		#region Build string util
		/// <summary>
		/// Builds an argument string for compiling a source to an object file
		/// </summary>
		/// <param name="input"></param>
		/// <param name="srcFile"></param>
		/// <param name="objDir"></param>
		/// <returns></returns>
		public static string BuildDSourceCompileArgumentString(string input, string srcFile, string objFile)
		{
			return ParseArgumentString(input, new Dictionary<string, string>{
				{"$src",srcFile},
				{"$objDir",Path.Combine( Path.GetDirectoryName(srcFile), Path.GetDirectoryName(objFile))},
				{"$obj",objFile},
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

			if(!string.IsNullOrEmpty(ret))
				foreach (var kv in ReplacedStrings)
					ret = ret.Replace(kv.Key,kv.Value);

			return ret;
		}
		#endregion

		bool isBuilding = false;
		public override bool IsBuilding
		{
			get { return isBuilding; }
		}

		public override void StopBuilding()
		{
			ShallStop = true;

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.Kill();
		}
	}
}
