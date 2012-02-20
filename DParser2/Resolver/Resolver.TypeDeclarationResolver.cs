using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;

namespace D_Parser.Resolver
{
	public partial class TypeDeclarationResolver
	{
		public static ResolveResult Resolve(DTokenDeclaration token)
		{
			var tk = (token as DTokenDeclaration).Token;

			if (DTokens.BasicTypes[tk])
				return new StaticTypeResult
						{
							BaseTypeToken = tk,
							DeclarationOrExpressionBase = token
						};

			return null;
		}

		public static ResolveResult[] ResolveIdentifier(string id, ResolverContextStack ctxt, object idObject)
		{
			var loc = CodeLocation.Empty;

			if (idObject is ITypeDeclaration)
				loc = (idObject as ITypeDeclaration).Location;
			else if (idObject is IExpression)
				loc = (idObject as IExpression).Location;

			var matches = NameScan.SearchMatchesAlongNodeHierarchy(ctxt, loc, id);

			return DResolver.HandleNodeMatches(matches, ctxt, null, loc);
		}

		public static ResolveResult[] Resolve(IdentifierDeclaration declaration, ResolverContextStack ctxt, ResolveResult[] resultBases=null)
		{
			var id = declaration as IdentifierDeclaration;

			if (declaration.InnerDeclaration == null && resultBases==null)
			{
				return ResolveIdentifier(id.Id, ctxt, id);
			}

			var rbases = resultBases ?? Resolve(declaration.InnerDeclaration, ctxt);

			if (rbases == null || rbases.Length == 0)
				return null;

			var r = new List<ResolveResult>();

			var scanResults = new List<ResolveResult>();
			var nextResults = new List<ResolveResult>();

			foreach (var b in rbases)
			{
				scanResults.Clear();
				nextResults.Clear();

				scanResults.Add(b);
				
				while (scanResults.Count > 0)
				{
					foreach (var scanResult in scanResults)
					{
						// First filter out all alias and member results..so that there will be only (Static-)Type or Module results left..
						if (scanResult is MemberResult)
						{
							var _m = (scanResult as MemberResult).MemberBaseTypes;
							if (_m != null)
								nextResults.AddRange(DResolver.FilterOutByResultPriority(ctxt, _m));
						}

						else if (scanResult is TypeResult)
						{
							var tr = scanResult as TypeResult;
							var nodeMatches = NameScan.ScanNodeForIdentifier(tr.ResolvedTypeDefinition, id.Id, ctxt);

							ctxt.PushNewScope(tr.ResolvedTypeDefinition);

							var results = DResolver.HandleNodeMatches(nodeMatches, ctxt, b, declaration);

							if (results != null)
								r.AddRange(DResolver.FilterOutByResultPriority(ctxt, results));

							ctxt.Pop();
						}
						else if (scanResult is ModuleResult)
						{
							var modRes = scanResult as ModuleResult;

							if (modRes.IsOnlyModuleNamePartTyped())
							{
								var modNameParts = modRes.ResolvedModule.ModuleName.Split('.');

								if (modNameParts[modRes.AlreadyTypedModuleNameParts] == id.Id)
									r.Add(new ModuleResult()
									{
										ResolvedModule = modRes.ResolvedModule,
										AlreadyTypedModuleNameParts = modRes.AlreadyTypedModuleNameParts + 1,
										ResultBase = modRes,
										DeclarationOrExpressionBase = declaration
									});
							}
							else
							{
								var matches = NameScan.ScanNodeForIdentifier((scanResult as ModuleResult).ResolvedModule, id.Id, ctxt);

								var results = DResolver.HandleNodeMatches(matches, ctxt, b, declaration);

								if (results != null)
									r.AddRange(results);
							}
						}
					}

					scanResults = nextResults;
					nextResults = new List<ResolveResult>();
				}
			}

			return r.ToArray();
		}

		public static ResolveResult[] Resolve(TypeOfDeclaration typeOf, ResolverContextStack ctxt)
		{
			// typeof(return)
			if (typeOf.InstanceId is TokenExpression && (typeOf.InstanceId as TokenExpression).Token == DTokens.Return)
			{
				var m = DResolver.HandleNodeMatch(ctxt.ScopedBlock, ctxt, null, typeOf);
				if (m != null)
					return new[] { m };
			}
			// typeOf(myInt)  =>  int
			else if (typeOf.InstanceId != null)
			{
				var wantedTypes = ExpressionTypeResolver.ResolveExpression(typeOf.InstanceId, ctxt);

				if (wantedTypes == null)
					return null;

				// Scan down for variable's base types
				var c1 = new List<ResolveResult>(wantedTypes);
				var c2 = new List<ResolveResult>();

				var ret = new List<ResolveResult>();

				while (c1.Count > 0)
				{
					foreach (var t in c1)
					{
						if (t is MemberResult)
						{
							if ((t as MemberResult).MemberBaseTypes != null)
								c2.AddRange((t as MemberResult).MemberBaseTypes);
						}
						else
						{
							t.DeclarationOrExpressionBase = typeOf;
							ret.Add(t);
						}
					}

					c1.Clear();
					c1.AddRange(c2);
					c2.Clear();
				}

				return ret.ToArray();
			}

			return null;
		}

		public static ResolveResult[] Resolve(MemberFunctionAttributeDecl attrDecl, ResolverContextStack ctxt)
		{
			var ret = Resolve(attrDecl.InnerType, ctxt);

			if (ret != null)
				foreach (var r in ret)
					if(r!=null)
						r.DeclarationOrExpressionBase = attrDecl;

			return ret;
		}

		public static ResolveResult[] Resolve(ArrayDecl ad, ResolverContextStack ctxt)
		{
			var valueTypes = Resolve(ad.ValueType, ctxt);

			ResolveResult[] keyTypes = null;

			if (ad.KeyExpression != null)
				keyTypes = ExpressionTypeResolver.ResolveExpression(ad.KeyExpression, ctxt);
			else
				keyTypes = Resolve(ad.KeyType, ctxt);

			if (valueTypes == null)
				return new[] { new ArrayResult { 
					ArrayDeclaration = ad,
					KeyType=keyTypes
				}};

			var r = new List<ResolveResult>(valueTypes.Length);

			foreach (var valType in valueTypes)
				r.Add(new ArrayResult { 
					ArrayDeclaration = ad,
					ResultBase=valType,
					KeyType=keyTypes
				});

			return r.ToArray();
		}

		public static ResolveResult[] Resolve(PointerDecl pd, ResolverContextStack ctxt)
		{
			var ptrBaseTypes = Resolve(pd.InnerDeclaration, ctxt);

			if (ptrBaseTypes == null)
				return new[] { 
					new StaticTypeResult{ DeclarationOrExpressionBase=pd}
				};

			var r = new List<ResolveResult>();

			foreach (var t in ptrBaseTypes)
				r.Add(new StaticTypeResult { 
					DeclarationOrExpressionBase=pd,
					ResultBase=t
				});

			return r.ToArray();
		}

		public static ResolveResult[] Resolve(DelegateDeclaration dg, ResolverContextStack ctxt)
		{
			var r = new DelegateResult { DeclarationOrExpressionBase=dg };

			r.ReturnType = Resolve(dg.ReturnType, ctxt);

			return new[] { r };
		}

		public static ResolveResult[] Resolve(ITypeDeclaration declaration, ResolverContextStack ctxt)
		{
			if (declaration is DTokenDeclaration)
			{
				var r = Resolve(declaration as DTokenDeclaration);

				if (r != null)
					return new[] { r };
			}
			else if (declaration is IdentifierDeclaration)
				return Resolve(declaration as IdentifierDeclaration, ctxt);
			else if (declaration is TemplateInstanceExpression)
				return ExpressionTypeResolver.ResolveTemplateInstance(declaration as TemplateInstanceExpression, ctxt);
			else if (declaration is TypeOfDeclaration)
				return Resolve(declaration as TypeOfDeclaration, ctxt);
			else if (declaration is MemberFunctionAttributeDecl)
				return Resolve(declaration as MemberFunctionAttributeDecl, ctxt);
			else if (declaration is ArrayDecl)
				return Resolve(declaration as ArrayDecl, ctxt);
			else if (declaration is PointerDecl)
				return Resolve(declaration as PointerDecl, ctxt);
			else if (declaration is DelegateDeclaration)
				return Resolve(declaration as DelegateDeclaration, ctxt);
			
			//TODO: VarArgDeclaration

			return null;
		}
	}
}
