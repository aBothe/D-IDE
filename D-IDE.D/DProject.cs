using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using D_IDE.Core;
using D_Parser.Misc;

namespace D_IDE.D
{
	public class DProject : Project
	{
		#region Properties
		public DVersion version = DVersion.D2;
		public DVersion DMDVersion
		{
			get{return version;}
			set{
				CompilerConfiguration.ParsingFinished -= globalCacheAnalysisFinished;
				version = value;
				CompilerConfiguration.ParsingFinished += globalCacheAnalysisFinished;
			}
		}

		public bool IsParsing { get { return !localCacheAnalysisFinished; } }
		public bool IsRelease = false;
		public List<string> LinkedLibraries = new List<string>();

		public DMDConfig CompilerConfiguration
		{
			get
			{
				return DSettings.Instance.DMDConfig(DMDVersion);
			}
		}

		public override string OutputFile
		{
			get
			{
				var f = base.OutputFile;
				if (OutputType == OutputTypes.StaticLibrary)
					return Path.ChangeExtension(f, ".lib");
				if (OutputType == OutputTypes.DynamicLibary)
					return Path.ChangeExtension(f, ".dll");
				return f;
			}
			set
			{
				base.OutputFile = value;
			}
		}

		public ParseCacheView CacheView
		{
			get {
				var pcl = new ParseCacheView(CompilerConfiguration.ImportDirectories);
				pcl.Add(new[]{BaseDirectory});
				return pcl;
			}
		}
		#endregion

		#region Constructor/Init
		public DProject()
		{ }

		public DProject(Solution sln,string file)
		{
			Solution = sln;
			FileName = sln.ToAbsoluteFileName( file);
		}
		#endregion
		
		/// <summary>
		/// Parse all D sources that belong to the project
		/// </summary>
		public void ParseDSourcesAsync()
		{
			localCacheAnalysisFinished = false;
			GlobalParseCache.BeginAddOrUpdatePaths(new[] { BaseDirectory }, false, (ParsingFinishedEventArgs ea) => {
				localCacheAnalysisFinished = true;
				BuildUfcsCache();
			});
		}

		public void BuildUfcsCache()
		{
			if (localCacheAnalysisFinished && CompilerConfiguration.InitialParsingDone)
				GlobalParseCache.GetRootPackage(BaseDirectory).UfcsCache.BeginUpdate(CacheView);
		}

		bool localCacheAnalysisFinished=false;

		void globalCacheAnalysisFinished()
		{
			BuildUfcsCache();
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
