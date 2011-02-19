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

		public override void BuildProject(Project prj)
		{
			var dprj = prj as DProject;
			if (dprj == null)
				throw new Exception("Cannot compile non-D project");

			DMDVersion=dprj.DMDVersion;

			dprj.LastBuildResult = new BuildResult() { SourceFile=dprj.FileName, TargetFile=dprj.OutputFile};

			// Build outputs/target paths
			string objectDirectory = dprj.BaseDirectory+"\\obj";
			Util.CreateDirectoryRecursively(objectDirectory);

			#region Compile d sources to object files
			var objs = new List<string>();
			bool AllObjectsUnchanged = true;

			AbstractLanguageBinding lang = null;
			foreach (var f in dprj.CompilableFiles)
			{
				if (ShallStop)return;
				bool _u = false ;
				if(lang==null || !lang.CanHandleFile(f.FileName))
					lang=AbstractLanguageBinding.SearchBinding(f.FileName, out _u);

				if (lang == null || _u || !lang.CanBuild)
					continue;

				lang.BuildSupport.BuildModule(f,objectDirectory,false);

				if (!f.LastBuildResult.Successful)
					return;

				if (!f.LastBuildResult.NoBuildNeeded)
					AllObjectsUnchanged = false;

				// To save command line space, make the targetfile relative to our objectDirectory
				objs.Add(f.LastBuildResult.TargetFile.StartsWith(dprj.BaseDirectory) 
					? f.LastBuildResult.TargetFile.Substring(dprj.BaseDirectory.Length + 1) 
					: f.LastBuildResult.TargetFile);
			}

			if (objs.Count < 1)
			{
				prj.LastBuildResult.Successful = true;
				prj.LastBuildResult.NoBuildNeeded = true;
				ErrorLogger.Log("Project "+dprj.Name+" empty",ErrorType.Error,ErrorOrigin.Build);
				return;
			}
			#endregion

			// If no source has been changed, skip linkage 
			if (AllObjectsUnchanged && File.Exists(dprj.OutputFile))
			{
				dprj.LastBuildResult.NoBuildNeeded = true;
				dprj.LastBuildResult.Successful = true;
				return;
			}

			// Add import libraries
			objs.AddRange(dprj.LinkedLibraries);

			// Link files
			var linkerExe = CurrentDMDConfig.ExeLinker;
			var args=CurrentDMDConfig.BuildArguments(!dprj.IsRelease);
			var linkerArgs = args.ExeLinker;

			switch (dprj.OutputType)
			{
				case OutputTypes.CommandWindowLessExecutable:
					linkerExe = CurrentDMDConfig.Win32ExeLinker;
					linkerArgs = args.Win32ExeLinker;
					break;
				case OutputTypes.DynamicLibary:
					linkerExe = CurrentDMDConfig.DllLinker;
					linkerArgs = args.DllLinker;
					break;
				case OutputTypes.StaticLibrary:
				case OutputTypes.Other:
					if (dprj.OutputFile.EndsWith(".lib"))
					{
						linkerExe = CurrentDMDConfig.LibLinker;
						linkerArgs = args.LibLinker;
					}
					break;
			}

			if (ShallStop) return;
			dprj.LastBuildResult = LinkFiles(linkerExe, linkerArgs, dprj.BaseDirectory, dprj.OutputFile, !dprj.IsRelease, objs.ToArray());
		}

		public BuildResult CompileSource(DMDConfig dmd,bool DebugCompile, string srcFile, string objFile, string execDirectory)
		{
			var obj = Path.ChangeExtension(objFile, ".obj");
			var br = new BuildResult() { SourceFile = srcFile,TargetFile=obj,Successful=true };

			ErrorLogger.Log("Compile " + srcFile,ErrorType.Information,ErrorOrigin.Build);

			var dmd_exe = dmd.SoureCompiler;

			// Always enable it to use environment paths to find dmd.exe
			if (!Path.IsPathRooted(dmd_exe) && Directory.Exists(dmd.BaseDirectory))
				dmd_exe = Path.Combine(dmd.BaseDirectory, dmd.SoureCompiler);

			TempPrc = FileExecution.ExecuteSilentlyAsync(dmd_exe,
					BuildDSourceCompileArgumentString(dmd.BuildArguments(DebugCompile).SoureCompiler, srcFile, obj), // Compile our program always in debug mode
					execDirectory,
					OnOutput, delegate(string s) {
						var err = ParseErrorMessage(s);
						if (err.Type == GenericError.ErrorType.Error)
							br.Successful = false;
						br.BuildErrors.Add(err);
					}, OnExit);

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.WaitForExit(10000);

			br.Successful = br.Successful && TempPrc.ExitCode==0 && File.Exists(obj);
			return br;
		}

		/// <summary>
		/// Links several object files to an executable, dynamic or static library
		/// </summary>
		public BuildResult LinkFiles(string linkerExe, string linkerArgs, string startDirectory, string targetFile, bool CreatePDB, params string[] files)
		{
			var errList = new List<GenericError>();
			var br = new BuildResult() { TargetFile=targetFile, Successful=true};

			ErrorLogger.Log("Link files to "+targetFile,ErrorType.Information,ErrorOrigin.Build);

			if (File.Exists(targetFile))
			{
				try
				{
					File.Delete(targetFile);
				}
				catch (Exception ex) // Perhaps our targetFile is still in use
				{
					br.BuildErrors.Add(new GenericError() { Message=ex.Message});
					return br;
				}
			}

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

			br.Successful = br.Successful && TempPrc.ExitCode==0 && File.Exists(targetFile);

			// If targetFile is executable or library, create PDB
			if (br.Successful && CreatePDB && (targetFile.EndsWith(".exe") || targetFile.EndsWith(".dll")))
			{
				var br_=CreatePDBFromExe(targetFile);
				br.BuildErrors.AddRange(br_.BuildErrors);

				br.AdditionalFiles=new[]{br_.TargetFile};
			}

			return br;
		}

		public override void BuildModule(SourceModule Module, string OutputDirectory, bool Link)
		{
			var dmd = CurrentDMDConfig;
			var dmd_exe = dmd.SoureCompiler;
			bool compileDebug = true;
			var src = Module.AbsoluteFileName;

			var outputDir = Path.IsPathRooted(OutputDirectory) ? OutputDirectory : Path.Combine(Path.GetDirectoryName(src), OutputDirectory);
			Util.CreateDirectoryRecursively(outputDir);
			
			// Compile .d source file to obj
			var obj = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(src) + ".obj");

			// Check if creation can be skipped
			if (!Link && !Module.Modified && File.Exists(obj))
			{
				Module.LastBuildResult = new BuildResult() { SourceFile=src, TargetFile=obj,Successful = true, NoBuildNeeded=true};
				return;
			}

			var br =Module.LastBuildResult= CompileSource(dmd,compileDebug,src,obj,Path.GetDirectoryName(src));

			if (Link && br.Successful)
			{
				br.Successful = false;
				#region Link To StandAlone executable
				var exe = OutputDirectory + "\\" + Path.GetFileNameWithoutExtension(src) + ".exe";
				br.TargetFile = exe;

				var br_ = LinkFiles(dmd.ExeLinker, dmd.BuildArguments(compileDebug).ExeLinker, Path.GetDirectoryName(obj), exe,compileDebug, obj);
				
				br.BuildErrors.AddRange(br_.BuildErrors);
				br.AdditionalFiles = br_.AdditionalFiles;
				br.Successful = br_.Successful;
				#endregion
			}		
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

			ErrorLogger.Log("Create debug information database " + pdb,ErrorType.Information,ErrorOrigin.Build);
			try
			{
				var prc = FileExecution.ExecuteSilentlyAsync(cv2pdb, "\"" + Executable + "\"", Path.GetDirectoryName(Executable),
				OnOutput, delegate(string s){
					br.BuildErrors.Add(new GenericError(){Message=s});	
				}, delegate()
				{
					if (File.Exists(pdb))
					{
						ErrorLogger.Log("Debug information database created successfully",ErrorType.Information,ErrorOrigin.Build);
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
			if(GlobalProperties.Instance.VerboseBuildOutput?true:TempPrc.ExitCode!=0)
			ErrorLogger.Log("Process ended with code " + TempPrc.ExitCode.ToString(),
				TempPrc.ExitCode==0?ErrorType.Information:ErrorType.Error,ErrorOrigin.Build);
		}

		static void OnOutput(string s)
		{
			ErrorLogger.Log(s,ErrorType.Message,ErrorOrigin.Build);
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
			return BuildArgumentString(input, new Dictionary<string, string>{
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

			return BuildArgumentString(input, new Dictionary<string, string>{
				{"$objs",objs},
				{"$targetDir",Path.GetDirectoryName(targetFile)},
				{"$target",targetFile},
				{"$exe",Path.ChangeExtension(targetFile,".exe")},
				{"$dll",Path.ChangeExtension(targetFile,".dll")},
				{"$lib",Path.ChangeExtension(targetFile,".lib")},
			});
		}
		#endregion

		public override void StopBuilding()
		{
			ShallStop = true;

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.Kill();
		}
	}
}
