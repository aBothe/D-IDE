using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	public interface IModule: ISourceModule
	{
		IProject Project { get; }

		void Parse();
	}
}
