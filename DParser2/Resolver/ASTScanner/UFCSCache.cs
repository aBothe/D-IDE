using System.Collections.Generic;
using D_Parser.Dom;
using D_Parser.Misc;
using D_Parser.Resolver.TypeResolution;

namespace D_Parser.Resolver.ASTScanner
{
	/// <summary>
	/// Contains resolution results of methods.
	/// </summary>
	public class UFCSCache
	{
		#region Properties
		public readonly Dictionary<DMethod, ResolveResult> CachedMethods = new Dictionary<DMethod, ResolveResult>();
		#endregion

		public void Clear()
		{
			CachedMethods.Clear();
		}

		/// <summary>
		/// Note: Does not truncate the cache!
		/// </summary>
		public void Update(ParseCache pc, ResolverContextStack ctxt)
		{
			// Enum through all modules of the parse cache
			foreach (var module in pc)
				// Enum through all child nodes
				foreach (var n in module)
				{
					var dm = n as DMethod;
					// UFCS only allowes free function that contain at least one parameter
					if (dm == null || dm.Parameters.Count == 0 || dm.Parameters[0].Type == null)
						continue;

					var firstParam = TypeDeclarationResolver.Resolve(dm.Parameters[0].Type,ctxt);

					if (firstParam != null && firstParam.Length != 0)
						CachedMethods[dm] = firstParam[0];
				}
		}

		public IEnumerable<DMethod> FindFitting(ResolverContextStack ctxt, CodeLocation currentLocation,ResolveResult firstArgument)
		{
			var preMatchList = new List<DMethod>();

			foreach (var kv in CachedMethods)
			{
				// First test if arg is matching the parameter
				if (kv.Value == firstArgument)
					preMatchList.Add(kv.Key);
			}

			var mv = new MatchVisitor<DMethod> {
				Context=ctxt,
				rawList=preMatchList
			};

			mv.IterateThroughScopeLayers(currentLocation);

			return mv.filteredList;
		}

		public class MatchVisitor<T> : AbstractVisitor where T : INode
		{
			/// <summary>
			/// Contains items that shall be tested for existence in the current scope tree.
			/// </summary>
			public IList<T> rawList;
			/// <summary>
			/// Contains items that passed the filter successfully.
			/// </summary>
			public List<T> filteredList=new List<T>();

			protected override bool HandleItem(INode n)
			{
				if (n is T && rawList.Contains((T)n))
					filteredList.Add((T)n);

				return false;
			}
		}
	}
}
