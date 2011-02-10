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
		public abstract void BuildModule(SourceModule Module, string OutputDirectory, bool LinkToStandAlone);

		#region TODO: Async build support
		public virtual void BuildModuleAsync(SourceModule Module, string OutputDirectory, bool LinkToStandAlone)
		{
			if (buildThread != null && buildThread.IsAlive)
				buildThread.Abort();

			buildThread = new Thread(delegate()
			{
				BuildModule(Module,OutputDirectory,LinkToStandAlone);
			});

			buildThread.IsBackground = true;
			buildThread.Start();
		}

		public void BuildStandAlone(SourceModule Module)
		{
			BuildModule(Module,Path.GetDirectoryName(Module.FileName),true);
		}

		public delegate void BuildFinishedEvent(BuildResult Result);
		public event BuildFinishedEvent BuildFinished;
		protected void OnBuildFinished(BuildResult res)
		{
			if (BuildFinished != null)
				BuildFinished(res);
		}

		public abstract void StopBuilding();
		#endregion

		public BuildResult BuildStandAlone(string file)
		{
			var sm = new SourceModule();
			sm.FileName = file;

			BuildStandAlone(sm);

			return sm.LastBuildResult;
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
