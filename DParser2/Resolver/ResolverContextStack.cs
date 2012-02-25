using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Dom.Statements;

namespace D_Parser.Resolver
{
	public class ResolverContextStack
	{
		#region Properties
		protected Stack<ResolverContext> stack = new Stack<ResolverContext>();

		public IEnumerable<IAbstractSyntaxTree> ParseCache;
		public Dictionary<string,IEnumerable<IAbstractSyntaxTree>> ImportsDictionary= new Dictionary<string,IEnumerable<IAbstractSyntaxTree>>();

		public IEnumerable<IAbstractSyntaxTree> ImportCache
		{
			get
			{
				if (CurrentContext == null || CurrentContext.ScopedBlock == null)
					return null;

				return GetImportCache(CurrentContext.ScopedBlock.NodeRoot as DModule);
			}
		}

		public IBlockNode ScopedBlock
		{
			get {
				if (stack.Count<1)
					return null;

				return CurrentContext.ScopedBlock;
			}
			set
			{
				if (stack.Count > 0)
					CurrentContext.ScopedBlock = value;
			}
		}

		public IStatement ScopedStatement
		{
			get
			{
				if (stack.Count < 1)
					return null;

				return CurrentContext.ScopedStatement;
			}
			set
			{
				if (stack.Count > 0)
					CurrentContext.ScopedStatement = value;
			}
		}

		Dictionary<object, Dictionary<string, ResolveResult[]>> resolvedTypes = new Dictionary<object, Dictionary<string, ResolveResult[]>>();

		/// <summary>
		/// Stores scoped-block dependent type dictionaries, which store all types that were already resolved once
		/// </summary>
		public Dictionary<object, Dictionary<string, ResolveResult[]>> ResolvedTypes
		{
			get { return resolvedTypes; }
		}

		public ResolverContext CurrentContext
		{
			get {
				return stack.Peek();
			}
		}
		#endregion

		public ResolverContextStack(
			IEnumerable<IAbstractSyntaxTree> ParseCache,
			ResolverContext initialContext,
			IEnumerable<IAbstractSyntaxTree> Imports=null)
		{
			this.ParseCache = ParseCache;
			
			stack.Push(initialContext);

			CreateImportCache(Imports);
		}

		public ResolverContext Pop()
		{
			if(stack.Count>0)
				return stack.Pop();

			return null;
		}

		public ResolverContext PushNewScope(IBlockNode scope)
		{
			var ctxtOverride = new ResolverContext();
			ctxtOverride.ApplyFrom(CurrentContext);
			ctxtOverride.ScopedBlock = scope;
			ctxtOverride.ScopedStatement = null;

			stack.Push(ctxtOverride);

			return ctxtOverride;
		}

		public void CreateImportCache(IEnumerable<IAbstractSyntaxTree> Imports=null)
		{
			if (CurrentContext == null || CurrentContext.ScopedBlock == null || ParseCache==null)
				return;

			var m = CurrentContext.ScopedBlock.NodeRoot as DModule;

			if (m != null)
				ImportsDictionary[m.ModuleName] = Imports ?? ImportResolver.ResolveImports(m, ParseCache);
		}

		public IEnumerable<IAbstractSyntaxTree> GetImportCache(DModule m)
		{
			if (m == null)
				return null;

			IEnumerable<IAbstractSyntaxTree> ret = null;
			if (ImportsDictionary.TryGetValue(m.ModuleName, out ret))
				return ret;

			// If imports weren't resolved already, do so
			try
			{
				ret = ImportResolver.ResolveImports(m, ParseCache);
				ImportsDictionary[m.ModuleName] = ret;
				return ret;
			}
			catch { }

			return null;
		}

		object GetMostFittingBlock()
		{
			if (CurrentContext == null)
				return null;

			if (CurrentContext.ScopedStatement != null)
			{
				var r = CurrentContext.ScopedStatement;

				while (r != null)
				{
					if (r is BlockStatement)
						return r;
					else
						r = r.Parent;
				}
			}
			
			return CurrentContext.ScopedBlock;
		}

		public void TryAddResults(string TypeDeclarationString, ResolveResult[] NodeMatches)
		{
			var ScopedType = GetMostFittingBlock();

			Dictionary<string, ResolveResult[]> subDict = null;

			if (!resolvedTypes.TryGetValue(ScopedType, out subDict))
				resolvedTypes.Add(ScopedType, subDict = new Dictionary<string, ResolveResult[]>());

			if (!subDict.ContainsKey(TypeDeclarationString))
				subDict.Add(TypeDeclarationString, NodeMatches);
		}

		public bool TryGetAlreadyResolvedType(string TypeDeclarationString, out ResolveResult[] NodeMatches)
		{
			var ScopedType = GetMostFittingBlock();

			Dictionary<string, ResolveResult[]> subDict = null;

			if (ScopedType != null && !resolvedTypes.TryGetValue(ScopedType, out subDict))
			{
				NodeMatches = null;
				return false;
			}

			if (subDict != null)
				return subDict.TryGetValue(TypeDeclarationString, out NodeMatches);

			NodeMatches = null;
			return false;
		}
	}
}
