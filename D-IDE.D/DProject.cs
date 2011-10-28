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

		public readonly ASTCollection ParsedModules = new ASTCollection() { ParseFunctionBodies=true};

		/// <summary>
		/// Parse all D sources that belong to the project
		/// </summary>
		public void ParseDSources()
		{
			ParsedModules.Clear();

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

			var files = Directory.EnumerateFiles(BaseDirectory, "*.d", SearchOption.AllDirectories);

			foreach (var file in files)
			{
				try
				{
					var ast = DParser.ParseFile(file);
					ParsedModules.Add(ast);
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
			}
			/*
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
			}*/
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
				yield return new DPrjSettingsPage();
			}
		}
	}
}
