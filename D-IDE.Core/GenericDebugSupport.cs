using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugEngineWrapper;

namespace D_IDE.Core
{
	public class GenericDebugSupport
	{
		/// <summary>
		/// Retrieves child symbols of a debug symbol.
		/// Primarily used when showing local symbols when debugging.
		/// </summary>
		/// <param name="LocalSymbolCache"></param>
		/// <param name="Parent">Item whose children are requested. Will be null when root symbols are </param>
		/// <returns></returns>
		public virtual IEnumerable<DebugSymbolWrapper> GetChildSymbols(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Parent)
		{
			var ret = new List<DebugSymbolWrapper>();
			if (Parent == null)
			{
				foreach (var s in LocalSymbolCache.Symbols)
					ret.Add(new DebugSymbolWrapper(s));
				return ret;
			}

			foreach (var s in Parent.Symbol.Children)
				ret.Add(new DebugSymbolWrapper(s));
			return ret;
		}

		public virtual bool HasChildren(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Symbol)
		{
			return Symbol.Symbol.ChildrenCount > 0;
		}

		/// <summary>
		/// Retrieves the value of a debug symbol.
		/// Debugging requires CodeCompletion to enable better node search.
		/// </summary>
		/// <param name="ModuleTree"></param>
		/// <param name="ScopedSrcLine"></param>
		/// <param name="sym"></param>
		/// <returns></returns>
		/*public virtual string BuildSymbolValueString(
			Parser.Core.AbstractSyntaxTree ModuleTree,
			uint ScopedSrcLine,
			DebugScopedSymbol sym) 
		{ 
			return sym.TextValue;
		}*/
	}

	/// <summary>
	/// A wrapper class for DebugScopedSymbols
	/// </summary>
	public class DebugSymbolWrapper
	{
		public DebugScopedSymbol Symbol { get; protected set; }
		public DebugSymbolWrapper(DebugScopedSymbol Symbol)
		{
			this.Symbol = Symbol;
		}

		public virtual string Name { get { return Symbol.Name; } }
		public virtual string ValueString { get { return Symbol.TextValue; } }
		public virtual string TypeString { get { return Symbol.TypeName; } }
	}
}
