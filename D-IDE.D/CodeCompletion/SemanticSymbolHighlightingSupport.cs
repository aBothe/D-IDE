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
		public static List<IdentifierDeclaration> ScanForTypeIdentifiers(IAbstractSyntaxTree Module)
		{
			var l = new List<IdentifierDeclaration>();

			if (Module != null)
			{
				var list = new List<INode> { Module };
				var list2 = new List<INode>();

				while (list.Count > 0)
				{
					foreach (var n in list)
					{
						if (!(n is DMethod) && n is IBlockNode)
							list2.AddRange(n as IBlockNode);

						SearchInDeclaration(n, l);
					}

					list = list2;
					list2 = new List<INode>();
				}
			}

			return l;
		}

		static void SearchInDeclaration(INode n, List<IdentifierDeclaration> l)
		{
			if (n == null)
				return;

			if(n.Type!=null)
				SearchInTypeDeclaration(n.Type,l);

			if (n is DNode)
			{
				var dn = n as DNode;

				//TODO: Template params still missing
			}

			if (n is DMethod)
			{
				var dm = n as DMethod;

				foreach (var p in dm.Parameters)
					SearchInDeclaration(p, l);

				SearchInStatement(dm.In, l);
				SearchInStatement(dm.Out, l);
				SearchInStatement(dm.Body, l);
			}

			if (n is DVariable)
			{
				var dv = n as DVariable;

				SearchInExpression(dv.Initializer, l);
			}

			if (n is DClassLike)
			{
				var dc = n as DClassLike;
				foreach (var bc in dc.BaseClasses)
					SearchInTypeDeclaration(bc, l);

				SearchInExpression(dc.Constraint, l);
			}

			if (n is IBlockNode && !(n is DMethod))
				foreach (var sn in n as IBlockNode)
					SearchInDeclaration(sn, l);
		}

		static void SearchInStatement(IStatement s, List<IdentifierDeclaration> l)
		{
			if (s == null)
				return;

			if (s is StatementContainingStatement)
			{
				var sstmts=(s as StatementContainingStatement).SubStatements;

				if(sstmts!=null && sstmts.Length>0)
					foreach (var ss in sstmts)
						SearchInStatement(ss, l);
			}

			if (s is IDeclarationContainingStatement)
			{
				var decls=(s as IDeclarationContainingStatement).Declarations;

				if(decls!=null && decls.Length>0)
					foreach (var d in decls)
						SearchInDeclaration(d, l);
			}

			if (s is IExpressionContainingStatement)
			{
				var exprs=(s as IExpressionContainingStatement).SubExpressions;

				if(exprs!=null && exprs.Length>0)
					foreach (var e in exprs)
						SearchInExpression(e, l);
			}
		}

		static void SearchInTypeDeclaration(ITypeDeclaration type, List<IdentifierDeclaration> l)
		{
			if (type == null)
				return;

			if (type is DelegateDeclaration)
				foreach (var p in (type as DelegateDeclaration).Parameters)
					SearchInDeclaration(p, l);

			if (type is IdentifierDeclaration && !(type is DTokenDeclaration))
				l.Add(type as IdentifierDeclaration);
			else
				SearchInTypeDeclaration(type.InnerDeclaration, l);
		}

		static void SearchInExpression(IExpression e, List<IdentifierDeclaration> l)
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
