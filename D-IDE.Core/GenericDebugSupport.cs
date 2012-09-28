using System;
using System.Collections.Generic;
using System.Diagnostics;
using DebugEngineWrapper;

namespace D_IDE.Core
{
	public class GenericDebugSupport
	{
		public IntPtr hProcess;

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

		public virtual void PostlaunchInit(DBGEngine Engine) { 
			//hProcess = (IntPtr)Engine.ProcessHandle; 
		}
	}

	/// <summary>
	/// A generic wrapper class for DebugScopedSymbols
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
