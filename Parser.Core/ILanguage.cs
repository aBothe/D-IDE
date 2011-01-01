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

		void SaveModuleCache(IAbstractSyntaxTree[] Modules, string[] ImportDirectories, string FileName);
		void SaveModuleCache(IAbstractSyntaxTree[] Modules, string FileName);
		IAbstractSyntaxTree[] LoadModuleCache(string FileName, out string[] ImportDirectories);
		IAbstractSyntaxTree[] LoadModuleCache(string FileName);
	}
}
