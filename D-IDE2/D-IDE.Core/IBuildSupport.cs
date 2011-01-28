using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace D_IDE.Core
{
	public abstract class IBuildSupport
	{
		protected Thread buildThread;
		public abstract BuildResult BuildProject(Project Project);
		public abstract BuildResult BuildModule(string FileName, string OutputDirectory, bool LinkToStandAlone);

		public virtual void BuildModuleAsync(string FileName, string OutputDirectory, bool LinkToStandAlone)
		{
			if (buildThread != null && buildThread.IsAlive)
				buildThread.Abort();

			buildThread = new Thread(delegate()
			{
				BuildModule(FileName,OutputDirectory,LinkToStandAlone);
			});

			buildThread.IsBackground = true;
			buildThread.Start();
		}


		public BuildResult BuildStandAlone(string FileName)
		{
			return BuildModule(FileName,Path.GetDirectoryName(FileName),true);
		}

		public delegate void BuildFinishedEvent(BuildResult Result);
		public event BuildFinishedEvent BuildFinished;
		protected void OnBuildFinished(BuildResult res)
		{
			if (BuildFinished != null)
				BuildFinished(res);
		}

		public abstract bool IsBuilding { get; }
		public abstract void StopBuilding();
	}

	public class BuildResult
	{
		public bool Successful;
		public readonly List<GenericError> BuildErrors=new List<GenericError>();
		public string SourceFile;
		public string TargetFile;

		public string[] AdditionalFiles;
	}
}
