using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using System.Diagnostics;

namespace D_IDE
{
	partial class IDEManager
	{
		public class ExeLaunchManagement
		{
			public static Process CurrentProcess { get; protected set; }

			public void LaunchWithoutDebugger(Solution sln)
			{

			}

			public void LaunchWithoutDebugger(string exe)
			{

			}
		}
	}
}
