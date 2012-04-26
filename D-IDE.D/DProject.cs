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

		/// <summary>
		/// UfcsCaching got disabled because of concurrency problems:
		/// This local cache requires the global cache to be parsed completely in order to e.g. resolve symbols like "string".
		/// Without or with partly finished analysis the created ufcs parameter cache will exclude some methods that use parameter types which are defined in the global cache -- like string or File*
		/// </summary>
		public readonly ParseCache ParsedModules = new ParseCache { EnableUfcsCaching=false };

		/// <summary>
		/// Parse all D sources that belong to the project
		/// </summary>
		public void ParseDSourcesAsync()
		{
			localCacheAnalysisFinished = false;
			ParsedModules.FinishedParsing += finishedProjectModuleAnalysis;
			ParsedModules.BeginParse(new[] { BaseDirectory },BaseDirectory);
		}

		public void BuildUfcsCache()
		{
			ParsedModules.UfcsCache.Update(ParseCacheList.Create(ParsedModules, CompilerConfiguration.ASTCache), ParsedModules);
		}

		/*
		 * Build the local UFCS Cache just AFTER the global cache analysis has been finished!
		 * So check both local & global parse states and ensure that both local&global caches have been built so
		 * it can proceed with resolving methods' first parameters.
		 */
		bool globalCacheAnalysisFinished = false, localCacheAnalysisFinished=false;

		void finishedCmpCacheAnalysis(ParsePerformanceData[] pfd)
		{
			if (localCacheAnalysisFinished)
				BuildUfcsCache();
			else
				globalCacheAnalysisFinished = true;
		}

		void finishedProjectModuleAnalysis(ParsePerformanceData[] pfd)
		{
			localCacheAnalysisFinished = true;

			if (globalCacheAnalysisFinished || !CompilerConfiguration.ASTCache.IsParsing)
				BuildUfcsCache();

			ParsedModules.FinishedParsing -= finishedProjectModuleAnalysis;

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

			CompilerConfiguration.ASTCache.FinishedParsing += finishedCmpCacheAnalysis;
		}

		protected override void SaveLanguageSpecificSettings(XmlWriter xw)
		{
			xw.WriteElementString("type", ((int)OutputType).ToString());
			xw.WriteElementString("isrelease", IsRelease?"true":"false");
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
