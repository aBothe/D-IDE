using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public interface IParser
	{
		ITypeDeclaration ParseType(string Code, out object OptionalToken);

		ISourceModule ParseString(string Code, bool OuterStructureOnly);
		ISourceModule ParseFile(string FileName, bool OuterStructureOnly);

		void UpdateModule(ISourceModule Module);
		void UpdateModuleFromText(string Code,ISourceModule Module);
	}
}
