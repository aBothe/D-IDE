using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using DebugEngineWrapper;

namespace D_IDE.Core
{
	public abstract class IBuildSupport
	{
		protected Thread buildThread;
		public abstract void BuildProject(Project Project);
		public abstract void BuildModule(SourceModule Module, string OutputDirectory, string ExecDirectory, bool LinkToStandAlone);

		public delegate void BuildFinishedEvent(BuildResult Result);
		public event BuildFinishedEvent BuildFinished;
		protected virtual void OnBuildFinished(BuildResult res)
		{
			if (BuildFinished != null)
				BuildFinished(res);
		}

		public virtual void StopBuilding()
		{
			if (buildThread != null && buildThread.IsAlive)
				buildThread.Abort();

			buildThread = null;
		}

		public void BuildProjectAsync(Project Project)
		{
			if (Project == null)
				return;
		}

		public void BuildModuleAsync(SourceModule Module, string OutputDirectory, string ExecDirectory, bool LinkToStandAlone)
		{
			if (Module == null)
				return;

			StopBuilding();

			buildThread = new Thread(delegate()
			{
				BuildModule(Module, OutputDirectory,ExecDirectory, LinkToStandAlone);
				OnBuildFinished(Module.LastBuildResult);
			});

			buildThread.IsBackground = true;
			buildThread.Start();
		}

		public void BuildStandAloneAsync(SourceModule Module)
		{
			if (Module == null)
				return;

			var dir=Path.GetDirectoryName(Module.AbsoluteFileName);
			BuildModuleAsync(Module, dir, dir, true);
		}

		public void BuildStandAlone(SourceModule Module)
		{
			BuildModule(Module, Path.GetDirectoryName(Module.FileName), Path.GetDirectoryName(Module.FileName), true);
		}

		public BuildResult BuildStandAlone(string file)
		{
			var sm = new SourceModule();
			sm.FileName = file;

			BuildStandAlone(sm);

			return sm.LastBuildResult;
		}

		/// <summary>
		/// Helper function that replaces replaceable strings with specified values.
		/// Useful e.g. when create execution argument strings
		/// </summary>
		public static string BuildArgumentString(string input, IEnumerable<KeyValuePair<string, string>> ReplacedStrings)
		{
			string ret = input;

			if (!string.IsNullOrEmpty(ret))
				foreach (var kv in ReplacedStrings)
					ret = ret.Replace(kv.Key, kv.Value);

			return ret;
		}
	}

	/// <summary>
	/// Contains information about the build state of a file.
	/// </summary>
	public class BuildResult
	{
		/// <summary>
		/// True if target file was up to date before the build request was invoked
		/// </summary>
		public bool NoBuildNeeded = false;

		public bool Successful;
		public readonly List<GenericError> BuildErrors=new List<GenericError>();
		public string SourceFile;
		public string TargetFile;

		/// <summary>
		/// Additionally generated files that are worth it to take care of.
		/// Useful e.g. when creating a .pdb file after sources have been compiled.
		/// </summary>
		public string[] AdditionalFiles;
	}
}
