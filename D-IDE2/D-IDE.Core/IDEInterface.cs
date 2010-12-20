using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace D_IDE.Core
{
	public class IDEInterface
	{
		public static IDEInterface Current;

		public IProject LoadProject(string FileName)
		{
			string ls=FileName.ToLower();

			foreach (var lang in from l in LanguageLoader.Bindings where l.ProjectsSupported select l)
				foreach (var pt in lang.ProjectTypes)
					foreach (var ext in pt.Extensions)
						if (ls.EndsWith(ext))
							return lang.OpenProject(ls);
			return null;
		}
	}
}
