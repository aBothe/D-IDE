using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core
{
	public interface IBuildSupport
	{
		BuildResult BuildProject(Project Project);
		BuildResult BuildModule(string FileName,string OutputDirectory,bool Link);
	}

	public class BuildResult
	{
		public bool Successful;
		public GenericError[] BuildErrors;
		public string SourceFile;
		public string TargetFile;

		public string[] AdditionalFiles;
	}
}
