using System.Collections.Generic;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;

namespace D_Parser.Resolver
{
	/// <summary>
	/// Class for scanning through code
	/// </summary>
	public class CodeScanner
	{
		public class CodeScanResult
		{
			public Dictionary<IdentifierDeclaration, ResolveResult> ResolvedIdentifiers = new Dictionary<IdentifierDeclaration, ResolveResult>();
			public List<IdentifierDeclaration> UnresolvedIdentifiers = new List<IdentifierDeclaration>();
			public List<ITypeDeclaration> RawIdentifiers = new List<ITypeDeclaration>();

			//public Dictionary<IExpression,>;
		}

		/// <summary>
		/// Scans the syntax tree for all kinds of identifier declarations, 
		/// tries to resolve them,
		/// adds them to a dictionary. If not found, 
		/// they will be added to a second, special array.
		/// 
		/// Note: For performance reasons, it's recommended to disable 'ResolveAliases' in the ResolverContext parameter
		/// </summary>
		/// <param name="lastResCtxt"></param>
		/// <param name="SyntaxTree"></param>
		/// <returns></returns>
		public static CodeScanResult ScanSymbols(ResolverContext lastResCtxt,IAbstractSyntaxTree SyntaxTree)
		{
			var csr = new CodeScanResult();

			var compDict = new Dictionary<string, ResolveResult>();

			// Step 1: Enum all existing type id's that shall become resolved'n displayed
			var typeObjects = CodeScanner.ScanForTypeIdentifiers(SyntaxTree);

			
			
			#region Step 2: Loop through all of them, try to resolve them, write the results in a dictionary
			foreach (var o in typeObjects)
			{
				if (o == null)
					continue;

				#region Identifier Declarations
				if (o is IdentifierDeclaration)
				{
					HandleIdDeclaration(o as IdentifierDeclaration,
						lastResCtxt,
						SyntaxTree,
						csr,
						compDict);
				}
				#endregion

				else if (o is NewExpression)
				{
					var ne = o as NewExpression;
				}
			}
			#endregion

			return csr;
		}

		static void HandleIdDeclaration(IdentifierDeclaration typeId,
			ResolverContext lastResCtxt,
			IAbstractSyntaxTree SyntaxTree,
			CodeScanResult csr, 
			Dictionary<string, ResolveResult> compDict)
		{
			var typeString = typeId.ToString();

			bool WasAlreadyResolved = false;
			ResolveResult[] allCurrentResults = null;
			ResolveResult rr = null;
			IStatement _unused = null;

			/*
			 * string,wstring,dstring are highlighted by the editor's syntax definition automatically..
			 * Anyway, allow to resolve e.g. "object.string"
			 */
			if (typeString == "" || typeString == "string" || typeString == "wstring" || typeString == "dstring")
				return;

			lastResCtxt.ScopedBlock = DResolver.SearchBlockAt(SyntaxTree, typeId.Location, out _unused);

			if (!(WasAlreadyResolved = compDict.TryGetValue(typeString, out rr)))
			{
				allCurrentResults = DResolver.ResolveType(typeId, lastResCtxt);

				if (allCurrentResults != null && allCurrentResults.Length > 0)
					rr = allCurrentResults[0];
			}

			if (rr == null)
			{
				if (typeId is IdentifierDeclaration)
					csr.UnresolvedIdentifiers.Add(typeId as IdentifierDeclaration);
			}
			else
			{
				/*
				 * Note: It is of course possible to highlight more than one type in one type declaration!
				 * So, we scan down the result hierarchy for TypeResults and highlight all of them later.
				 */
				var curRes = rr;

				/*
				 * Note: Since we want to use results multiple times,
				 * we at least have to 'update' their type declarations
				 * to ensure that the second, third, fourth etc. occurence of this result
				 * are also highlit (and won't(!) cause an Already-Added-Exception of our finalDict-Array)
				 */
				var curTypeDeclBase = typeId as ITypeDeclaration;

				while (curRes != null)
				{

					if (curRes is MemberResult)
					{
						var mr = curRes as MemberResult;

						// If curRes is an alias or a template parameter, highlight it
						if (mr.ResolvedMember is TemplateParameterNode ||
							(mr.ResolvedMember is DVariable &&
							(mr.ResolvedMember as DVariable).IsAlias))
						{
							try
							{
								csr.ResolvedIdentifiers.Add(curTypeDeclBase as IdentifierDeclaration, curRes);
							}
							catch { }

							// See performance reasons
							//if (curRes != rr && !WasAlreadyResolved && !) compDict.Add(curTypeDeclBase.ToString(), curRes);
						}
					}

					if (curRes is TypeResult)
					{
						// Yeah, in quite all cases we do identify a class via its name ;-)
						if (curTypeDeclBase is IdentifierDeclaration &&
							!(curTypeDeclBase is DTokenDeclaration) &&
							!csr.ResolvedIdentifiers.ContainsKey(curTypeDeclBase as IdentifierDeclaration))
						{
							csr.ResolvedIdentifiers.Add(curTypeDeclBase as IdentifierDeclaration, curRes);

							// See performance reasons
							//if (curRes != rr && !WasAlreadyResolved) compDict.Add(curTypeDeclBase.ToString(), curRes);
						}
					}

					curRes = curRes.ResultBase;
					curTypeDeclBase = curTypeDeclBase.InnerDeclaration;
				}
			}
		}

		public static List<object> ScanForTypeIdentifiers(INode Node)
		{
			var l = new List<object>();

			if (Node != null)
				SearchIn(Node, l);

			return l;
		}

		static void SearchIn(INode node, List<object> l)
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
						if(dn.TemplateParameters!=null)
							foreach (var tp in dn.TemplateParameters)
							{
								if (tp is TemplateValueParameter)
								{
									var tvp = tp as TemplateValueParameter;

									SearchIn(tvp.Type, l);
									SearchIn(tvp.DefaultExpression, l);
									SearchIn(tvp.SpecializationExpression, l);
								}
							}
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

		static void SearchIn(IStatement stmt, List<object> l)
		{
			if (stmt == null)
				return;

			var l1 = new List<IStatement> { stmt };
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

		static void SearchIn(ITypeDeclaration type, List<object> l)
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
				else if (type is DExpressionDecl)
				{
					SearchIn((type as DExpressionDecl).Expression, l);
				}
				else if (type is TemplateInstanceExpression)
				{
					var tie=type as TemplateInstanceExpression;

					//l.Add(tie);
					
					if (tie.TemplateIdentifier != null)
						l.Add(tie.TemplateIdentifier);

					var args=tie.Arguments;

					if(args!=null)
						foreach(var arg in args)
							SearchIn(arg, l);
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

		static void SearchIn(IExpression ex, List<object> l)
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

					if (
						e is NewExpression ||
						
						e is PostfixExpression_Access ||
						(e is IdentifierExpression && (e as IdentifierExpression).IsIdentifier))
					{
						l.Add(e.ExpressionTypeRepresentation);
					}
					else if (/*e is PostfixExpression_MethodCall || */e is TemplateInstanceExpression)
					{
						//l.Add(e);
						var tie = e as TemplateInstanceExpression;
						
						if (tie.TemplateIdentifier != null)
							l.Add(tie.TemplateIdentifier);

						// Do not add e's subexpressions anymore - it'll be done in the ScanSymbols method
						//continue;
					}
					
					if (e is ContainerExpression)
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
