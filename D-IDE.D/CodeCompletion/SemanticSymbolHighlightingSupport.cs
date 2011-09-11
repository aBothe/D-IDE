using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using D_Parser.Parser;
using D_IDE.D.CodeCompletion;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;

namespace D_IDE.D
{
	public class CodeWideSymbolEnlister
	{
		public class CodeSymbol
		{
			public IdentifierDeclaration Symbol;
		}

		public static List<CodeSymbol> ScanForTypeIdentifiers(IAbstractSyntaxTree Module)
		{
			var l = new List<CodeSymbol>();

			var list = new List<INode> { Module };
			var list2 = new List<INode>();

			while (list.Count > 0)
			{
				foreach (var n in list)
				{
					if (!(n is DMethod) && n is IBlockNode)
						list2.AddRange(n as IBlockNode);

					if (n is DNode)
					{
						var dn = n as DNode;

						//TODO: Template params still missing
					}

					if (n is DMethod)
					{
						var dm = n as DMethod;

						foreach (var p in dm.Parameters)
						{
							SearchInTypeDeclaration(p.Type,l);
							if (p is DVariable)
							{
								var pv = p as DVariable;
								//TODO: Ditto; Also check template params
								SearchInExpression(pv.Initializer, l);
							}
						}

						SearchInStatement(dm.In,l);
						SearchInStatement(dm.Out, l);
						SearchInStatement(dm.Body, l);
					}

					if (n is DVariable)
					{
						var dv = n as DVariable;

						SearchInExpression(dv.Initializer,l);
					}

					if (n is DClassLike)
					{
						var dc=n as DClassLike;
						foreach (var bc in dc.BaseClasses)
							SearchInTypeDeclaration(bc, l);

						SearchInExpression(dc.Constraint,l);
					}
				}

				list = list2;
				list2 = new List<INode>();
			}

			return l;
		}

		static void SearchInStatement(IStatement s, List<CodeSymbol> l)
		{
			if (s == null)
				return;

			if(s is StatementContainingStatement)
				foreach(var ss in (s as StatementContainingStatement).SubStatements)
				{

				}
		}

		static void SearchInTypeDeclaration(ITypeDeclaration type, List<CodeSymbol> l)
		{
			if (type == null)
				return;
		}

		static void SearchInExpression(IExpression e, List<CodeSymbol> l)
		{
			if (e == null)
				return;
		}
	}

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
}
