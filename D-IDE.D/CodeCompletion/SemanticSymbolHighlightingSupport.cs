using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using D_Parser.Parser;
using D_IDE.D.CodeCompletion;

namespace D_IDE.D
{
	public class SemanticSymbolHighlightingSupport:IVisualLineTransformer,IBackgroundRenderer
	{
		public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
		{
			throw new NotImplementedException();
		}

		public void Draw(TextView textView, System.Windows.Media.DrawingContext drawingContext)
		{
			throw new NotImplementedException();
		}

		public KnownLayer Layer
		{
			get { throw new NotImplementedException(); }
		}
	}

	public class IdentifierTracker : TokenTracker
	{
		public readonly List<string> handledIdentifiers = new List<string>();

		public void OnToken(AbstractLexer lexer,int kind)
		{
			if (kind == DTokens.Identifier)
			{

			}
		}
	}
}
