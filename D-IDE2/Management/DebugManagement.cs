using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugEngineWrapper;

namespace D_IDE
{
	partial class IDEManager
	{
		public class DebugManagement
		{
			public static DBGEngine Engine=new DBGEngine();

			public static bool IsDebugging
			{
				get;
				protected set;
			}
		}
	}
}
