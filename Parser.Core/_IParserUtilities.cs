using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public interface _IParserUtilities
	{
		
		SourceModule ParseString(string Code, bool OuterStructureOnly);
		SourceModule ParseFile(string FileName, bool OuterStructureOnly);

		void UpdateModule(SourceModule Module);
		void UpdateModuleFromText(string Code,SourceModule Module);
	}
}
