using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using D_IDE.Core;
using D_Parser.Misc;
using D_Parser.Dom;

namespace D_IDE.D
{
	public class DSettings
	{
		public static DSettings Instance=new DSettings();

		public DMDConfig dmd1 = new DMDConfig() { Version=DVersion.D1};
		public DMDConfig dmd2 = new DMDConfig() { Version=DVersion.D2};

		public DVersion DefaultDMDVersion = DVersion.D2;

		public DMDConfig DMDConfig()
		{
			return DMDConfig(DefaultDMDVersion);
		}

		public DMDConfig DMDConfig(DVersion v)
		{
			if (v == DVersion.D1)
				return dmd1;
			return dmd2;
		}

		public string cv2pdb_exe = "cv2pdb.exe";
		public bool UseCodeCompletion = true;
		public bool UseMethodInsight = true;
		public bool EnableMatchingBracketHighlighting = true;

		public bool UseSemanticErrorHighlighting = false;
		public bool UseSemanticHighlighting = true;

		public bool EnableSmartIndentation = true;

		/// <summary>
		/// If non-letter has been typed, the popup will close down and insert the selected item's completion text.
		/// If this value is false, the completion text will _not_ be inserted.
		/// In developing process, this flag will remain 'false' by default - because the completion still not brings 100%-suitable results.
		/// </summary>
		public bool ForceCodeCompetionPopupCommit = false;

		#region Saving&Loading
		public void Save(XmlWriter x)
		{
			x.WriteStartDocument();

			x.WriteStartElement("dsettings");

			x.WriteStartElement("cv2pdb");
			x.WriteCData(cv2pdb_exe);
			x.WriteEndElement();

			x.WriteStartElement("BracketHightlighting");
			x.WriteAttributeString("value", EnableMatchingBracketHighlighting.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseCodeCompletion");
			x.WriteAttributeString("value",UseCodeCompletion.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseMethodInsight");
			x.WriteAttributeString("value", UseMethodInsight.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("ForceCodeCompetionPopupCommit");
			x.WriteAttributeString("value", ForceCodeCompetionPopupCommit. ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseSemanticErrorHighlighting");
			x.WriteAttributeString("value", UseSemanticErrorHighlighting.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseSemanticHighlighting");
			x.WriteAttributeString("value", UseSemanticHighlighting.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("SmartIndentation");
			x.WriteAttributeString("value", EnableSmartIndentation. ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("CompletionOptions");
			CompletionOptions.Instance.Save(x);
			x.WriteEndElement();

			dmd1.Save(x);
			dmd2.Save(x);

			x.WriteEndElement();
		}

		public void Load(XmlReader x)
		{
			while (x.Read())
			{
				switch (x.LocalName)
				{
					case "BracketHightlighting":
						if (x.MoveToAttribute("value"))
							EnableMatchingBracketHighlighting = x.ReadContentAsBoolean();
						break;

					case "UseMethodInsight":
						if (x.MoveToAttribute("value"))
							UseMethodInsight = x.ReadContentAsBoolean();
						break;

					case "UseCodeCompletion":
						if (x.MoveToAttribute("value"))
							UseCodeCompletion = x.ReadContentAsBoolean();
						break;

					case "ForceCodeCompetionPopupCommit":
						if (x.MoveToAttribute("value"))
							ForceCodeCompetionPopupCommit = x.ReadContentAsBoolean();
						break;

					case "UseSemanticErrorHighlighting":
						if (x.MoveToAttribute("value"))
							UseSemanticErrorHighlighting = x.ReadContentAsBoolean();
						break;

					case "UseSemanticHighlighting":
						if (x.MoveToAttribute("value"))
							UseSemanticHighlighting = x.ReadContentAsBoolean();
						break;

					case "SmartIndentation":
						if (x.MoveToAttribute("value"))
							EnableSmartIndentation = x.ReadContentAsBoolean();
						break;

					case "CompletionOptions":
						CompletionOptions.Instance.Load(x.ReadSubtree());
						break;

					case "cv2pdb":
						cv2pdb_exe = x.ReadString();
						break;

					case "dmd":
						var config = new DMDConfig();
						config.Load(x);

						if (config.Version == DVersion.D1)
							dmd1 = config;
						else
							dmd2 = config;
						break;
				}
			}
		}
		#endregion
	}

	public enum DVersion:int
	{
		D1=1,
		D2=2
	}

	public class DMDConfig
	{
		public DMDConfig()
		{
			DebugArgs.Reset();
			ReleaseArgs.Reset();
		}

		public class DBuildArguments
		{
			public bool IsDebug = false;

			public string SoureCompiler;
			public string Win32ExeLinker;
			public string ExeLinker;
			public string DllLinker;
			public string LibLinker;

			public void Load(XmlReader x)
			{
				if (x.LocalName != "buildarguments")
					return;

				if (x.MoveToAttribute("IsDebug"))
					bool.TryParse(x.GetAttribute("IsDebug"),out IsDebug);
				x.MoveToElement();

				var x2 = x.ReadSubtree();
				while (x2.Read())
				{
					switch (x2.LocalName)
					{
						case "sourcecompiler":
							SoureCompiler = x2.ReadString();
							break;
						case "win32linker":
							Win32ExeLinker = x2.ReadString();
							break;
						case "exelinker":
							ExeLinker = x2.ReadString();
							break;
						case "dlllinker":
							DllLinker = x2.ReadString();
							break;
						case "liblinker":
							LibLinker = x2.ReadString();
							break;
					}
				}
			}

			public void Save(XmlWriter x)
			{
				x.WriteStartElement("buildarguments");
				x.WriteAttributeString("IsDebug",IsDebug.ToString());

				x.WriteStartElement("sourcecompiler");
				x.WriteCData(SoureCompiler);
				x.WriteEndElement();

				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();

				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();

				x.WriteEndElement();
			}

			public void ApplyFrom(DBuildArguments other)
			{
				IsDebug = other.IsDebug;
				SoureCompiler = other.SoureCompiler;
				Win32ExeLinker = other.Win32ExeLinker;
				ExeLinker = other.ExeLinker;
				DllLinker = other.DllLinker;
				LibLinker = other.LibLinker;
			}

			/// <summary>
			/// Reset arguments to defaults
			/// </summary>
			public void Reset()
			{
				ProvideDefaultBuildArguments(this, IsDebug);
			}

			/// <summary>
			/// (Re-)Sets all arguments back to what they were originally
			/// </summary>
			public static void ProvideDefaultBuildArguments(DBuildArguments args, bool DebugArguments = false)
			{
				string commonLinkerArgs = "";
				if (DebugArguments)
				{
					commonLinkerArgs = "$objs -gc -debug ";

					args.IsDebug = true;
					args.SoureCompiler = "-c \"$src\" -of\"$obj\" $importPaths -gc -debug";
				}
				else
				{
					commonLinkerArgs = "$objs -release -O -inline ";

					args.SoureCompiler = "-c \"$src\" -of\"$obj\" $importPaths -release -O -inline";
				}

				args.Win32ExeLinker = commonLinkerArgs + "-L/su:windows -L/exet:nt -of\"$exe\"";
				args.ExeLinker = commonLinkerArgs + "-of\"$exe\"";
				args.DllLinker = commonLinkerArgs + "-L/IMPLIB:$relativeTargetDir -of\"$dll\"";

				args.LibLinker = "-lib -of\"$lib\" $objs";
			}
		}

		public DVersion Version = DVersion.D2;

		/// <summary>
		/// If the dmd bin directory contains a 'dmd' or 'dmd2', 
		/// check if phobos and/or core paths are existing, 
		/// and add them to the ASTCache
		/// OR empirically update the directory paths
		/// </summary>
		public void TryAddImportPaths()
		{
			var defaultDmdDirname=Version==DVersion.D2? "dmd2":"dmd";

			int k = BaseDirectory.IndexOf(defaultDmdDirname+'\\');
			if (k>0)
			{
				var dmdPath=BaseDirectory.Substring(0,k+defaultDmdDirname.Length);

				var dirs=new[]{@"src\phobos",@"src\druntime\import"};

				// Check for phobos on both D1 and D2
				var newImports = new List<string>();

				foreach (var subPath in dirs)
				{
					var dir = Path.Combine(dmdPath, subPath);

					if (ImportDirectories.Count != 0)
						foreach (var pdir in ImportDirectories)
							if (!pdir.Contains(Path.Combine(defaultDmdDirname, subPath)))
								newImports.Add(dir);
				}
				ImportDirectories.AddRange(newImports);

				if (newImports.Count != 0)
					ReparseImportDirectories();
			}
		}

		public bool BaseDirectoryChanged
		{
			get;
			set;
		}

		string baseDir = @"C:\dmd2\windows\bin";

		/// <summary>
		/// The "bin" directory of the dmd installation
		/// </summary>
		public string BaseDirectory
		{
			get { return baseDir; }
			set {
				if (baseDir != value)
				{
					BaseDirectoryChanged = true;
					baseDir = value;
				}
			}
		}

		public string SoureCompiler = "dmd.exe";
		public string ExeLinker = "dmd.exe";
		public string Win32ExeLinker = "dmd.exe";
		public string DllLinker = "dmd.exe";
		public string LibLinker = "dmd.exe";

		public List<string> DefaultLinkedLibraries = new List<string>();
		public List<string> ImportDirectories = new List<string>();

		public DBuildArguments BuildArguments(bool IsDebug)
		{
			if (IsDebug)
				return DebugArgs;
			return ReleaseArgs;
		}

		public DBuildArguments DebugArgs = new DBuildArguments() { IsDebug=true };

		public DBuildArguments ReleaseArgs=new DBuildArguments();

		public void Load(XmlReader x)
		{
			if (x.LocalName != "dmd")
				return;

			if(x.MoveToAttribute("version"))
				Version = (DVersion)Convert.ToInt32( x.GetAttribute("version"));
			x.MoveToElement();

			var x2 = x.ReadSubtree();
			while (x2.Read())
			{
				switch (x2.LocalName)
				{
					case "basedirectory":
						baseDir = x2.ReadString();
						break;
					case "sourcecompiler":
						SoureCompiler = x2.ReadString();
						break;
					case "exelinker":
						ExeLinker = x2.ReadString();
						break;
					case "win32linker":
						Win32ExeLinker = x2.ReadString();
						break;
					case "dlllinker":
						DllLinker = x2.ReadString();
						break;
					case "liblinker":
						LibLinker = x2.ReadString();
						break;

					case "buildarguments":
						var args = new DBuildArguments();
						args.Load(x2);
						if (args.IsDebug)
							DebugArgs = args;
						else 
							ReleaseArgs = args;
						break;

					case "parsedDirectories":
						if (x2.IsEmptyElement)
							break;

						var st = x2.ReadSubtree();
						if(st!=null)
							while (st.Read())
							{
								if (st.LocalName == "dir")
								{
									var dir = st.ReadString();
									if(!string.IsNullOrWhiteSpace(dir) && !ImportDirectories.Contains(dir))
										ImportDirectories.Add(dir);
								}
							}
						break;

					case "DefaultLibs":
						var xr2 = x2.ReadSubtree();
						while (xr2.Read())
						{
							if (xr2.LocalName == "lib")
							{
								var libF = xr2.ReadString();
								if(!DefaultLinkedLibraries.Contains(libF))
									DefaultLinkedLibraries.Add(libF);
							}
						}
						break;
				}
			}

			// After having loaded the directory paths, parse them asynchronously
			ReparseImportDirectories();
		}

		public void ReparseImportDirectories()
		{
			InitialParsingDone = false;
			GlobalParseCache.BeginAddOrUpdatePaths(tempImports = ImportDirectories.ToArray(), true, parsedSources);
		}

		public Action ParsingFinished;
		public bool InitialParsingDone { private set; get; }
		string[] tempImports;

		void parsedSources(ParsingFinishedEventArgs pfd)
		{
			InitialParsingDone = true;
			if(ParsingFinished != null)
				ParsingFinished();

			var pcw = new ParseCacheView(tempImports);
			
			// Output parse time stats
			if (pfd != null)
				ErrorLogger.Log("Parsed " + pfd.FileAmount + " files in [" +
						string.Join(",",tempImports) + "] in " +
						Math.Round(pfd.ParseDuration/1000.0, 2).ToString() + "s (~" +
						Math.Round(pfd.FileParseDuration, 3).ToString() + "ms per file)",
						ErrorType.Information, ErrorOrigin.Parser);

			// For debugging purposes dump all parse results (errors etc.) to a log file.
			/*try
			{
				ParseLog.Write(ASTCache, IDEInterface.ConfigDirectory + "\\" + Version.ToString() + ".GlobalParseLog.log");
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.System);
			}*/
		}

		public void Save(XmlWriter x)
		{
			x.WriteStartElement("dmd");
			x.WriteAttributeString("version",((int)Version).ToString());

			if (!string.IsNullOrEmpty(BaseDirectory)){
				x.WriteStartElement("basedirectory");
				x.WriteCData(BaseDirectory);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(SoureCompiler)){
			x.WriteStartElement("sourcecompiler");
			x.WriteCData(SoureCompiler);
			x.WriteEndElement();
			} 
			if (!string.IsNullOrEmpty(ExeLinker)){
				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(Win32ExeLinker))
			{
				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(DllLinker))
			{
				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(LibLinker))
			{
				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();
			}

			if (ImportDirectories.Count != 0)
			{
				x.WriteStartElement("parsedDirectories");
				foreach (var pdir in ImportDirectories)
				{
					x.WriteStartElement("dir");
					x.WriteCData(pdir);
					x.WriteEndElement();
				}
				x.WriteEndElement();
			}

			x.WriteStartElement("DefaultLibs");
			foreach (var lib in DefaultLinkedLibraries)
			{
				x.WriteStartElement("lib");
				x.WriteCData(lib);
				x.WriteEndElement();
			}
			x.WriteEndElement();

			DebugArgs.Save(x);
			ReleaseArgs.Save(x);

			x.WriteEndElement();
		}
	}
}
