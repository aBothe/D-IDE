using System.IO;
using System.Xml;
using D_IDE.Core;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;

namespace D_IDE.ResourceFiles
{
	public class ResScriptFileBinding : AbstractLanguageBinding
	{
		public ResScriptFileBinding()
		{
			modTemplates = new[] {
				new FileTemplate{
					DefaultFilePrefix="Resources",
					Description="Contains information about objects (Dialog/Menu definitions, Icons, Images, String tables, Binary data etc.) that will be linked into the executable/dll later.",
					Extensions=new[]{".rc"},
					LargeImage=ResResources.res32,
					SmallImage=ResResources.res16,
					Name="Resource script"
				}		
			};

			// Associate highlighting definitions
			var ms = new MemoryStream(ResResources.RC_xshd);
			var hi = HighlightingLoader.Load(new XmlTextReader(ms), HighlightingManager.Instance);
			HighlightingManager.Instance.RegisterHighlighting(
				"RC", new[] { ".rc" }, hi);
			ms.Close();
		}

		public override string LanguageName
		{
			get { return "Resources"; }
		}

		public override object LanguageIcon
		{
			get { return ResResources.resx32; }
		}

		FileTemplate[] modTemplates;
		public override FileTemplate[] ModuleTemplates
		{
			get { return modTemplates; }
		}

		public override bool CanBuild{get	{	return true;	}}
		public override bool CanUseSettings		{			get			{				return true;			}		}

		#region Editing
		public override bool SupportsEditor(string SourceFile)
		{
			return SourceFile.ToLower().EndsWith(".rc");
		}
		#endregion

		#region Settings
		public override void SaveSettings(string SuggestedFileName)
		{
			var x = XmlTextWriter.Create(SuggestedFileName);
			ResConfig.Instance.Save(x);
			x.Close();
		}

		public override void LoadSettings(string SuggestedFileName)
		{
			if (File.Exists(SuggestedFileName))
			{
				var x = XmlTextReader.Create(SuggestedFileName);
				ResConfig.Instance.Load(x);
				x.Close();
			}
		}

		public override AbstractSettingsPage SettingsPage
		{
			get
			{
				return new ResSettingsPage();
			}
		}
		#endregion

		#region Building
		ResScriptBuildSupport bs = new ResScriptBuildSupport();
		public override AbstractBuildSupport BuildSupport
		{
			get
			{
				return bs;
			}
		}
		#endregion
	}
}
