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
	public class CodeSymbolResolver
	{
		public static List<ITypeDeclaration> ScanForTypeIdentifiers(INode Node)
		{
			var l = new List<ITypeDeclaration>();

			if (Node!= null)
				SearchIn(Node, l);

			return l;
		}

		static void SearchIn(INode node, List<ITypeDeclaration> l)
		{
			if (node == null)
				return;

			var l1 = new List<INode> { node };
			var l2 = new List<INode>();

			while (l1.Count > 0)
			{
				foreach (var n in l1)
				{
					if (n.Type != null)
						SearchIn(n.Type, l);

					if (n is DNode)
					{
						var dn = n as DNode;

						//TODO: Template params still missing
					}

					if (n is DMethod)
					{
						var dm = n as DMethod;

						l2.AddRange(dm.Parameters);

						if (dm.AdditionalChildren.Count > 0)
							l2.AddRange(dm.AdditionalChildren);

						SearchIn(dm.In, l);
						SearchIn(dm.Out, l);
						SearchIn(dm.Body, l);
					}

					if (n is DVariable)
					{
						var dv = n as DVariable;

						SearchIn(dv.Initializer, l);
					}

					if (n is DClassLike)
					{
						var dc = n as DClassLike;
						foreach (var bc in dc.BaseClasses)
							SearchIn(bc, l);

						SearchIn(dc.Constraint, l);
					}

					if (n is IBlockNode && !(n is DMethod))
						l2.AddRange((n as IBlockNode).Children);
				}

				l1.Clear();
				l1.AddRange(l2);
				l2.Clear();
			}
		}

		static void SearchIn(IStatement stmt, List<ITypeDeclaration> l)
		{
			if (stmt == null)
				return;

			var l1 = new List<IStatement> {stmt };
			var l2 = new List<IStatement>();

			while (l1.Count > 0)
			{
				foreach (var s in l1)
				{
					if (s is StatementContainingStatement)
					{
						var sstmts = (s as StatementContainingStatement).SubStatements;

						if (sstmts != null && sstmts.Length > 0)
							l2.AddRange(sstmts);
					}

					if (s is IDeclarationContainingStatement)
					{
						var decls = (s as IDeclarationContainingStatement).Declarations;

						if (decls != null && decls.Length > 0)
							foreach (var d in decls)
								SearchIn(d, l);
					}

					if (s is IExpressionContainingStatement)
					{
						var exprs = (s as IExpressionContainingStatement).SubExpressions;

						if (exprs != null && exprs.Length > 0)
							foreach (var e in exprs)
								SearchIn(e, l);
					}
				}

				l1.Clear();
				l1.AddRange(l2);
				l2.Clear();
			}
		}

		static void SearchIn(ITypeDeclaration type, List<ITypeDeclaration> l)
		{
			while (type != null)
			{
				if (type is DelegateDeclaration)
					foreach (var p in (type as DelegateDeclaration).Parameters)
						SearchIn(p, l);
				else if (type is ArrayDecl)
				{
					var ad = type as ArrayDecl;

					if (ad.KeyExpression != null)
						SearchIn(ad.KeyExpression, l);
					if (ad.KeyType != null)
						SearchIn(ad.KeyType, l);
				}

				if (type is IdentifierDeclaration && !(type is DTokenDeclaration))
					l.Add(type as IdentifierDeclaration);
				else
				{
					type = type.InnerDeclaration;
					continue;
				}

				break;
			}
		}

		static void SearchIn(IExpression ex, List<ITypeDeclaration> l)
		{
			if (ex == null)
				return;

			var l1 = new List<IExpression> { ex };
			var l2 = new List<IExpression>();

			while (l1.Count > 0)
			{
				foreach (var e in l1)
				{
					if (e is UnaryExpression_Type)
						SearchIn((e as UnaryExpression_Type).Type, l);

					if (e is NewExpression ||
						e is PostfixExpression_Access ||
						(e is IdentifierExpression && (e as IdentifierExpression).IsIdentifier))
					{
						l.Add(e.ExpressionTypeRepresentation);
					}
					else if (e is ContainerExpression)
					{
						var ec = e as ContainerExpression;
						var subex = ec.SubExpressions;

						if (subex != null)
							l2.AddRange(subex);
					}
				}

				l1.Clear();
				l1.AddRange(l2);
				l2.Clear();
			}
		}
	}
}
