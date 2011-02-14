using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using DebugEngineWrapper;

namespace D_IDE.D
{
	public class DDebugSupport:GenericDebugSupport
	{
		Dictionary<DebugScopedSymbol, DebugSymbolWrapper[]> _childArray=new Dictionary<DebugScopedSymbol,DebugSymbolWrapper[]>();

		public DebugSymbolWrapper[] GetChildren(DebugSymbolGroup locals, DebugSymbolWrapper parent)
		{
			var ret = new List<DDebugSymbolWrapper>();

			var scache=locals.Symbols;

			// If requesting root-leveled items, return all whose depth equals 0
			if (parent == null)
			{
				foreach (var sym in scache)
					if (sym.Depth < 1)
						ret.Add(new DDebugSymbolWrapper(sym));

				return ret.ToArray();
			}

			// Find out index of parent item in locals
			int i = 0;
			for (; i < scache.Length; i++)
				if (scache[i].Offset == parent.Symbol.Offset)
					break;

			// If any items aren't there for searching, return empty list
			if (i >= scache.Length)
				return _childArray[parent.Symbol] = null;

			// Scan if following items are deeper-leveled
			for (int j = i + 1; j < scache.Length; j++)
			{
				var d=scache[j].Depth;
				if (d == parent.Symbol.Depth + 1) // Only add direct child items
				{
					//TODO: Scan for base classes
					ret.Add(new DDebugSymbolWrapper(scache[j]));
				}
				else if(d<=parent.Symbol.Depth) 
					break; // If on the same level again or moved up a level, break
			}

			return  ret.ToArray();
		}

		public override IEnumerable<DebugSymbolWrapper> GetChildSymbols(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Parent)
		{
			//return GetChildren(LocalSymbolCache,Parent);
			if (Parent == null)
				return GetChildren(LocalSymbolCache, Parent);

			if (_childArray.ContainsKey(Parent.Symbol))
				return _childArray[Parent.Symbol];

			return null;
		}

		public override bool HasChildren(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Symbol)
		{
			/* Note: HasChildren gets called first. To save searching time,
			 * write our pre-results into the dictionary to return them when GetChildSymbols() is called.
			 */
			var ret=_childArray[Symbol.Symbol] = GetChildren(LocalSymbolCache,Symbol);
			return ret != null && ret.Length > 0;
		}

		public class DDebugSymbolWrapper : DebugSymbolWrapper
		{
			public DDebugSymbolWrapper(DebugScopedSymbol sym):base(sym)
			{}

			//TODO: Read out (array!) values, class defintion locations and rename base class references to 'base'
		}
	}
}
