using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugEngineWrapper;

namespace D_IDE.Core
{
	public interface IDebugProvider
	{
		string BuildSymbolValueString(IModule Module,uint ScopedSrcLine, DebugScopedSymbol sym);
	}
}
