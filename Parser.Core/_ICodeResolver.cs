using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public interface _ICodeResolver
	{
		ITypeDeclaration BuildIdentifierList(string Text, int CaretOffset, bool BackwardOnly, out object OptionalInitToken);
		IEnumerable<Node> ResolveTypes(IEnumerable<ISourceModule> Cache, IBlockNode CurrentlyScopedBlock, ITypeDeclaration IdentifierList);

		IEnumerable<Node> ResolveImports(IEnumerable<ISourceModule> Cache, ISourceModule CurrentModule);

		void IsInCommentAreaOrString(string Text, int Offset, out bool IsInString, out bool IsInLineComment, out bool IsInBlockComment, out bool IsInNestedBlockComment);
		bool IsInCommentOrString(string Text,int Offset);
	}
}
