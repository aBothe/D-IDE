using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core
{
	public interface ISolution
	{
		string Name { get; set; }
		string FileName { get; set; }
		IProject StartProject { get; set; }
		IProject[] Projects { get; set; }
	}
}
