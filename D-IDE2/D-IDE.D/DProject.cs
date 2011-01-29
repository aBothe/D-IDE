using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Xml;
using System.IO;

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

		public new string OutputFile
		{
			get
			{
				var f = base.OutputFile;
				if (OutputType == OutputTypes.Other)
					return Path.ChangeExtension(f,".lib");
				return f;
			}
			set
			{
				base.OutputFile = value;
			}
		}

		public DVersion DMDVersion = DVersion.D2;
		public bool IsRelease=false;
		public List<string> LinkedLibraries = new List<string>();

		public DMDConfig CompilerConfiguration
		{
			get
			{
				return DSettings.Instance.DMDConfig(DMDVersion);
			}
		}
	}
}
