using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	public interface IModule
	{
		string FileName { get; set; }
		IProject Project { get; }
		ILanguage Language { get; }
		ISourceModule CodeNode { get; }

		bool CanUseDebugging { get; }
		bool CanUseCodeCompletion { get; }
		bool CanBuild { get; }
		bool CanBuildToSingleModule { get; }

		void Refresh();
		
		void Build(string OutputFile);
		/// <returns>Did it build or not?</returns>
		bool BuildIncrementally();
	}
}
