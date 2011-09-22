using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;

namespace D_Parser.Completion
{
	/// <summary>
	/// Generic interface between a high level editor object and the low level completion engine
	/// </summary>
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
