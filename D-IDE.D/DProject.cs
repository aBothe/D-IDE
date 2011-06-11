using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Xml;
using System.IO;
using D_IDE.D.CodeCompletion;
using D_Parser;
using D_Parser.Parser;

namespace D_IDE.D
{
	public class DProject:Project
	{
		public DProject()
		{ }

		public DProject(Solution sln,string file)
		{
			Solution = sln;
			FileName = sln.ToAbsoluteFileName( file);
		}

		public override string OutputFile
		{
			get
			{
				var f = base.OutputFile;
				if (OutputType == OutputTypes.StaticLibrary)
					return Path.ChangeExtension(f,".lib");
				return f;
			}
			set
			{
				base.OutputFile = value;
			}
		}

		public override string FileName
		{
			get
			{
				return base.FileName;
			}
			set
			{
				base.FileName = value;
				ParsedModules.BaseDirectory = value;
			}
		}

		public readonly ASTCollection ParsedModules=new ASTCollection();

		/// <summary>
		/// Parse all D sources that belong to the project
		/// </summary>
		public void ParseDSources()
		{
			ParsedModules.Clear();
			foreach (var mod in _Files)
			{
				if (DLanguageBinding.IsDSource(mod.FileName))
				{
					try
					{
						var ast = DParser.ParseFile(mod.AbsoluteFileName);
						ParsedModules.Add(ast);
					}
					catch (Exception ex)
					{
						ErrorLogger.Log(ex);
					}
				}
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

		protected override void LoadLanguageSpecificSettings(XmlReader xr)
		{
			while(xr.Read())
				switch (xr.LocalName)
				{
					case "isrelease":
						IsRelease = xr.ReadString()=="true";
						break;
					case "dversion":
						DMDVersion = (DVersion)Convert.ToInt32(xr.ReadString());
						break;
					case "libs":
						var xr2 = xr.ReadSubtree();
						while (xr2.Read())
						{
							if (xr2.LocalName == "lib")
								LinkedLibraries.Add(xr2.ReadString());
						}
						break;
					default: break;
				}
		}

		protected override void SaveLanguageSpecificSettings(XmlWriter xw)
		{
			xw.WriteElementString("isrelease", IsRelease.ToString());
			xw.WriteElementString("dversion",((int)DMDVersion).ToString());
			xw.WriteStartElement("libs");
			foreach (var lib in LinkedLibraries)
				xw.WriteElementString("lib",lib);
			xw.WriteEndElement();
		}

		public override IEnumerable<AbstractProjectSettingsPage> LanguageSpecificProjectSettings
		{
			get
			{
				yield return new DPrjSettingsPage();
			}
		}
	}
}
