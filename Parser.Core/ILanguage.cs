using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public interface ILanguage
	{
		string DefaultFileExtension { get; }

		IParser CreateParser();
		ICodeResolver CreateCodeResolver();

		void SaveModuleCache(ISourceModule[] Modules, string[] ImportDirectories, string FileName);
		void SaveModuleCache(ISourceModule[] Modules, string FileName);
		ISourceModule[] LoadModuleCache(string FileName, out string[] ImportDirectories);
		ISourceModule[] LoadModuleCache(string FileName);
	}
}
