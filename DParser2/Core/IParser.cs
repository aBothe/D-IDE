using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser.Core
{
	public interface IParser
	{
		ITypeDeclaration ParseType(string Code, out object OptionalToken);

		IAbstractSyntaxTree ParseString(string Code, bool OuterStructureOnly);
		IAbstractSyntaxTree ParseFile(string FileName, bool OuterStructureOnly);

		void UpdateModule(IAbstractSyntaxTree Module);
		void UpdateModuleFromText(string Code,IAbstractSyntaxTree Module);
	}
}
