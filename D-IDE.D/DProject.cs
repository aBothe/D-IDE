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
using D_Parser.Completion;
using D_Parser.Misc;

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
				if (OutputType == OutputTypes.DynamicLibary)
					return Path.ChangeExtension(f, ".dll");
				return f;
			}
			set
			{
				base.OutputFile = value;
			}
		}

		public readonly ParseCache ParsedModules = new ParseCache();

		/// <summary>
		/// Parse all D sources that belong to the project
		/// </summary>
		public void ParseDSourcesAsync()
		{
			/*
			 * Instead of parsing added files only, add all D sources that are situated in the project's base directory.
			 * DMD allows importing local modules that are not referenced in the objects parameter.
			 * 
			 * ---
			 * module A;
			 * 
			 * void foo();
			 * 
			 * ---
			 * module B;
			 * 
			 * import A;
			 * 
			 * ... foo(); ... // Allowed!
			 * 
			 * --- whereas we compiled the program only via dmd.exe A.d
			 */
			ParsedModules.FinishedParsing += finishedProjectModuleAnalysis;
			ParsedModules.BeginParse(new[] { BaseDirectory },BaseDirectory);
		}

		void finishedProjectModuleAnalysis(ParsePerformanceData[] pfd)
		{
			DEditorDocument.UpdateSemanticHighlightings(true);
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
					case "type":
						try
						{
							OutputType = (OutputTypes)Convert.ToInt32(xr.ReadString());
						}
						catch { }
						break;
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
			xw.WriteElementString("type", ((int)OutputType).ToString());
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
				return new[]{ new DPrjSettingsPage() };
			}
		}
	}
}
