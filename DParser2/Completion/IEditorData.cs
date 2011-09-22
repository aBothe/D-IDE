using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;

namespace D_Parser.Completion
{
	public interface IEditorData
	{
		string ModuleCode { get; }
		CodeLocation CaretLocation { get; }
		int CaretOffset { get; }
		DModule SyntaxTree { get; }

		IEnumerable<IAbstractSyntaxTree> ParseCache { get; }
		IEnumerable<IAbstractSyntaxTree> ImportCache { get; }
	}
}
