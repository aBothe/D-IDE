using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Dom.Statements;

namespace D_Parser.Resolver
{
	public partial class DResolver
	{
		public static IBlockNode SearchBlockAt(IBlockNode Parent, CodeLocation Where, out IStatement ScopedStatement)
		{
			ScopedStatement = null;

			if (Parent != null && Parent.Count > 0)
				lock(Parent)
					foreach (var n in Parent)
						if (n is IBlockNode && Where >= n.StartLocation && Where <= n.EndLocation)
							return SearchBlockAt(n as IBlockNode, Where, out ScopedStatement);

			if (Parent is DMethod)
			{
				var dm = Parent as DMethod;

				var body = dm.GetSubBlockAt(Where);

				// First search the deepest statement under the caret
				if (body != null)
					ScopedStatement = body.SearchStatementDeeply(Where);
			}

			return Parent;
		}

		public static IBlockNode SearchClassLikeAt(IBlockNode Parent, CodeLocation Where)
		{
			if (Parent != null && Parent.Count > 0)
				foreach (var n in Parent)
				{
					if (!(n is DClassLike)) continue;

					var b = n as IBlockNode;
					if (Where >= b.BlockStartLocation && Where <= b.EndLocation)
						return SearchClassLikeAt(b, Where);
				}

			return Parent;
		}

		public static ResolveResult[] FilterOutByResultPriority(
			ResolverContextStack ctxt,
			ResolveResult[] results)
		{
			if (results != null && results.Length > 1)
			{
				var newRes = new List<ResolveResult>();
				foreach (var rb in results)
				{
					var n = GetResultMember(rb);
					if (n != null)
					{
						// Put priority on locals
						if (n is DVariable &&
							(n as DVariable).IsLocal)
							return new[] { rb };

						// If member/type etc. is part of the actual module, omit external symbols
						if (n.NodeRoot == ctxt.CurrentContext.ScopedBlock.NodeRoot)
							newRes.Add(rb);
					}
				}

				if (newRes.Count > 0)
					return newRes.ToArray();
			}

			return results;
		}

		public static INode GetResultMember(ResolveResult res)
		{
			if (res is MemberResult)
				return (res as MemberResult).ResolvedMember;
			else if (res is TypeResult)
				return (res as TypeResult).ResolvedTypeDefinition;
			else if (res is ModuleResult)
				return (res as ModuleResult).ResolvedModule;

			return null;
		}

		/// <summary>
		/// If an aliased type result has been passed to this method, it'll return the resolved type.
		/// If aliases were done multiple times, it also tries to skip through these.
		/// 
		/// alias char[] A;
		/// alias A B;
		/// 
		/// var resolvedType=TryRemoveAliasesFromResult(% the member result from B %);
		/// --> resolvedType will be StaticTypeResult from char[]
		/// 
		/// </summary>
		/// <param name="rr"></param>
		/// <returns></returns>
		public static ResolveResult[] TryRemoveAliasesFromResult(IEnumerable<ResolveResult> initialResults)
		{
			var ret = new List<ResolveResult>(initialResults);
			var l2 = new List<ResolveResult>();

			while (ret.Count > 0)
			{
				foreach (var res in ret)
				{
					var mr = res as MemberResult;
					if (mr != null &&

						// Alias check
						mr.ResolvedMember is DVariable &&
						(mr.ResolvedMember as DVariable).IsAlias &&

						// Check if it has resolved base types
						mr.MemberBaseTypes != null &&
						mr.MemberBaseTypes.Length > 0)
						l2.AddRange(mr.MemberBaseTypes);
				}

				if (l2.Count < 1)
					break;

				ret.Clear();
				ret.AddRange(l2);
				l2.Clear();
			}

			return ret.ToArray();
		}

	}
}
