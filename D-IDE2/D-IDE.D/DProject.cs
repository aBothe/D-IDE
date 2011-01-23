using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Xml;

namespace D_IDE.D
{
	public class DProject:Project
	{
		public DProject(Solution sln,string file)
		{
			Solution = sln;
			OnReadElementFromFile += delegate(XmlReader xr)
			{
				return false;
			};

			OnWriteToFile += delegate(XmlWriter xw)
			{

			};

			FileName = sln.ToAbsoluteFileName( file);
		}

		public DVersion Version = DVersion.D2;
		public bool IsRelease;

		public DMDConfig CompilerConfiguration
		{
			get
			{
				return DSettings.Instance.DMDConfig(Version);
			}
		}
	}
}
