using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using D_IDE.Core;

namespace D_IDE.ResourceFiles
{
	public class ResScriptBuildSupport:AbstractBuildSupport
	{
		Process TempPrc = null;

		/// <summary>
		/// Maximum compilation time (10 Seconds)
		/// </summary>
		public const int MaxCompilationTime = 10000;

		public override bool CanBuildProject(Project Project)
		{
			return false;
		}

		public override bool CanBuildFile(string SourceFile)
		{
			return SourceFile.ToLower().EndsWith(".rc");
		}

		public override void BuildProject(Project Project)
		{
			throw new NotImplementedException();
		}

		public override void BuildModule(SourceModule Module, 
			string OutputDirectory, 
			string ExecDirectory, 
			bool LinkToStandAlone)
		{
			var src = Module.AbsoluteFileName;

			var outputDir = Path.IsPathRooted(OutputDirectory) ? OutputDirectory : Path.Combine(Path.GetDirectoryName(src), OutputDirectory);
			
			// Ensure that target directory exists
			Util.CreateDirectoryRecursively(outputDir);

			// Compile .rc source file to res
			var res = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(src) + ".res");

			// Check if creation can be skipped
			if (GlobalProperties.Instance.EnableIncrementalBuild && !Module.Modified && File.Exists(res))
			{
				Module.LastBuildResult = new BuildResult() { SourceFile = src, TargetFile = res, Successful = true, NoBuildNeeded = true };
				return;
			}

			// Init returned BuildResult-object
			var br = new BuildResult() { SourceFile = src, TargetFile = res, Successful = true };

			// Print verbose information message
			ErrorLogger.Log("Compile " + src, ErrorType.Information, ErrorOrigin.Build);

			var resourceCompiler = ResConfig.Instance.ResourceCompilerPath;

			// Prefer the resource compiler located in D-IDE's bin root 
			// Otherwise let the system search along the %PATH% environment variable to find the resource compiler
			if (!Path.IsPathRooted(resourceCompiler) && 
				File.Exists(Path.Combine(Util.ApplicationStartUpPath,resourceCompiler)))
				resourceCompiler = Path.Combine(Util.ApplicationStartUpPath, resourceCompiler);

			TempPrc = FileExecution.ExecuteSilentlyAsync(resourceCompiler,
					BuildResArgumentString(
						ResConfig.Instance.ResourceCompilerArguments, 
						src, res, outputDir),
					ExecDirectory,
					// Output handling
					delegate(string s)
					{
						var err = ParseErrorMessage(s);
						if (Module.Project != null && Module.Project.ContainsFile(err.FileName))
							err.Project = Module.Project;
						br.BuildErrors.Add(err);
						br.Successful = false;

						if (!GlobalProperties.Instance.VerboseBuildOutput)
							ErrorLogger.Log(s, ErrorType.Message, ErrorOrigin.Build);
					}, 
					// Error handling
					delegate(string s)
					{
						br.Successful = false;

						ErrorLogger.Log(s, ErrorType.Error, ErrorOrigin.Build);
					}, 
					OnExit);

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.WaitForExit(MaxCompilationTime);

			br.Successful = br.Successful && TempPrc != null && TempPrc.ExitCode == 0 && File.Exists(res);

			if (br.Successful)
				Module.ResetModifiedTime();

			Module.LastBuildResult = br;
		}

		public static string BuildResArgumentString(string inputArgString,
			string SourceFile, string TargetFile, string OutputDir)
		{
			return BuildArgumentString(inputArgString, new Dictionary<string, string>{
				{"$rc",SourceFile},
				{"$sourceDir", Path.GetDirectoryName(SourceFile)},
				{"$targetDir", Path.GetDirectoryName(OutputDir)},
				{"$res",TargetFile}
			});
		}

		public static BuildError ParseErrorMessage(string s)
		{
			int to = s.IndexOf(".rc(");
			if (to >= 0)
			{
				to += 3;
				string FileName = s.Substring(0, to);
				to += 1;
				int to2 = s.IndexOf(") :", to);
				if (to2 > 0)
				{
					int lineNumber = Convert.ToInt32(s.Substring(to, to2 - to));
					string errmsg = s.Substring(to2 + 2).Trim();

					return new BuildError(errmsg, FileName, 0, lineNumber);
				}
			}
			return new BuildError(s);
		}

		void OnExit()
		{
			if (GlobalProperties.Instance.VerboseBuildOutput ? true : TempPrc.ExitCode != 0)
				ErrorLogger.Log("Process ended with code " + TempPrc.ExitCode.ToString(),
					TempPrc.ExitCode == 0 ? ErrorType.Information : ErrorType.Error, ErrorOrigin.Build);
		}
	}
}
