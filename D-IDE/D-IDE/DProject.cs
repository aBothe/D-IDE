﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using D_Parser;
using System.Windows.Forms;
using System.Xml;

namespace D_IDE
{
	public class DProject
	{
		#region Manifest creation
		public static void CreateManifestFile(string file)
		{
			string cont = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\"><assemblyIdentity name=\"DApplication\" processorArchitecture=\"x86\" version=\"1.0.0.0\" type=\"win32\"/><description></description><dependency><dependentAssembly><assemblyIdentity type=\"win32\" name=\"Microsoft.Windows.Common-Controls\" version=\"6.0.0.0\" processorArchitecture=\"x86\" publicKeyToken=\"6595b64144ccf1df\" language=\"*\" /></dependentAssembly></dependency></assembly>";

			File.WriteAllText(file, cont, Encoding.UTF8);
		}

		public static void CreateManifestImportingResourceFile(string rc,string manifestFile)
		{
			string cont = "// Automatically created resource file - changes will become deleted after the next build\r\n"+
				"1 24 \""+manifestFile.Replace("\\","\\\\")+"\"\r\n";

			File.WriteAllText(rc, cont, Encoding.ASCII);
		}

		public bool CreateExternalManifestFile()
		{
			if (!String.IsNullOrEmpty(LastBuiltTarget))
				CreateManifestFile(LastBuiltTarget + ".manifest");
			else return false;

			return true;
		}

		public enum ManifestCreationType
		{
			None = 0,
			IntegratedResource,
			External,
		}
		#endregion

		#region Properties
		public const string prjext = ".dproj";
		public enum PrjType
		{
			WindowsApp = 0,
			ConsoleApp,
			Dll,
			StaticLib,
		}

		public ManifestCreationType ManifestCreation=ManifestCreationType.None;

		public bool isRelease;
		//public CompilerConfiguration Compiler=D_IDE_Properties.Default.dmd2;
		public CompilerConfiguration.DVersion CompilerVersion=CompilerConfiguration.DVersion.D2;
		public CompilerConfiguration Compiler
		{
			get {
				if (CompilerVersion == CompilerConfiguration.DVersion.D1) 
					return D_IDE_Properties.Default.dmd1;
				return D_IDE_Properties.Default.dmd2;
			}
		}

		public bool EnableSubversioning = false; // Shall this stay activated by default?
        /// <summary>
        /// Indicates whether all changed source files are stored into the subversion directories
        /// </summary>
		public bool AlsoStoreSources = true;
		public int LastVersionCount = 10;
		public string LastBuiltTarget = "";
		public DateTime LastBuildDate;
		public void RefreshBuildDate()
		{
			LastBuildDate = DateTime.Now;
		}

		public List<string> libs;
		public string execargs;
		public string linkargs;
		public string compileargs;

		public PrjType type;
		public string name;
		public string targetfilename;
		public string OutputDirectory = "bin\\Release";
		public string OutputDirectory_dbg = "bin\\Debug";
		public string AbsoluteOutputDirectory
		{
			get
			{
				string od = isRelease ? OutputDirectory : OutputDirectory_dbg;
				string add = "";
				if (EnableSubversioning)
				{
					add = "\\" + LastBuildDate.ToString().Replace(':', '-').Replace('/','.');
				}
				if (Path.IsPathRooted(od)) return od + add;
				return basedir + "\\" + od + add;
			}
		}
		public string AbsoluteOutputDirectoryWithoutSubversionName
		{
			get
			{
				string od = isRelease ? OutputDirectory : OutputDirectory_dbg;
				if (Path.IsPathRooted(od)) return od;
				return basedir + "\\" + od;
			}
		}

		[NonSerialized()]
		public string prjfn;
		public string basedir;

		[NonSerialized()]
		public Dictionary<string, long> LastModifyingDates = new Dictionary<string, long>();

		[NonSerialized()]
		public List<CodeModule> Modules = new List<CodeModule>();
		public List<string> Files = new List<string>();
		public List<string> FileDependencies = new List<string>();
		public List<string> ProjectDependencies = new List<string>();
		public List<string> LastOpenedFiles = new List<string>();
		#endregion

		public void ParseAll()
		{
			if (Modules == null) Modules = new List<CodeModule>();
			Modules.Clear();
			foreach (string fn in Files)
			{
				if (!CodeModule.Parsable(fn)) continue;
				Modules.Add(new CodeModule(this,GetPhysFilePath(fn)));
			}
		}
		public string GetPhysFilePath(string file)
		{
			if (Path.IsPathRooted(file)) return file;

			return basedir + "\\" + file;
		}
		public string GetRelFilePath(string file)
		{
			if (!Path.IsPathRooted(file)) return file;

			if (file.StartsWith(basedir + "\\"))
			{
				return file.Remove(0, basedir.Length + 1);
			}
			return file;
		}

        /// <summary>
        /// Gets module and package (no leading, but trailing dots) string from file
        /// </summary>
        public void GetModuleAndPackage(string file, out string module, out string package)
        {
            file = GetRelFilePath(file);
            if (file.EndsWith(".d")) file = file.Substring(0, file.Length - 2);
            file = file.Replace('\\', '.');
            module = file.Substring(Math.Max(0, file.LastIndexOf('.') + 1));
            package = file.Substring(0, Math.Max(0, file.LastIndexOf('.')));
            if (package.Length > 0) package += ".";
        }

		public bool FileExists(string file)
		{
			if (!File.Exists(file))
			{
				return File.Exists(basedir + "\\" + file);
			}
			return true;
		}

		public DProject()
		{
			isRelease = false;
			libs = new List<string>();

			execargs = "";
			linkargs = "";
			compileargs = "";

			name = "";
			targetfilename = "";
			prjfn = "";
			basedir = ".";
			Modules = new List<CodeModule>();
			Files = new List<string>();
			LastOpenedFiles = new List<string>();
		}

		public bool Contains(string file)
		{
			return Files.Contains(GetRelFilePath( file));
		}

		[System.Diagnostics.DebuggerStepThrough()]
		public bool AddSrc(string file)
		{
			long mod = 0;
			try
			{
				mod = File.GetLastWriteTimeUtc(GetPhysFilePath(file)).ToFileTimeUtc();
			}
			catch { }
			return AddSrc(file, mod);
		}
		public bool AddSrc(string file, long lastModified)
		{
			string fn = file;
			string phys_fn = GetPhysFilePath(file);
			if (fn.StartsWith(basedir + "\\"))
			{
				fn = fn.Remove(0, basedir.Length + 1);
			}

			if (Files.Contains(fn))
			{
				MessageBox.Show("File \"" + fn + "\" already exists in project!");
				return false;
			}
			Files.Add(fn);
			if (LastModifyingDates.ContainsKey(phys_fn))
				LastModifyingDates[phys_fn] = lastModified;
			else
				LastModifyingDates.Add(phys_fn, lastModified);
			return true;
		}

        public void AddDirectory(string Path, bool AlsoScanSubdirectories)
        {
            string[] Files = Directory.GetFiles(Path, "*", AlsoScanSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (Files == null || Files.Length < 1)
                return;

            foreach (string file in Files)
                AddSrc(file);

            Save();
        }

		/*public void RemoveNonExisting()
		{
			List<string> newRC = new List<string>(resourceFiles);
			foreach (string rc in resourceFiles)
			{
				if (!Path.IsPathRooted(rc))
				{
					if (!File.Exists(basedir + "\\" + rc))
					{
						newRC.Remove(rc);
					}
					continue;
				}
				if (!File.Exists(rc))
				{
					newRC.Remove(rc);
				}
			}
			resourceFiles = newRC;
		}*/
		/*
		public bool RenameFile(string from, string to)
		{
			if (!resourceFiles.Contains(from)) return false;

			DModule dm = FileDataByFile(from);
			if (dm != null) dm.FileName = to;

			DocumentInstanceWindow diw = Form1.thisForm.FileDataByFile(from);
			if (diw != null)
			{
				diw.fileData.FileName = to;
				diw.Update();
			}

			resourceFiles.Remove(from);
			resourceFiles.Add(to);
			return true;
		}*/

		#region Load & Save
		public static DProject LoadFrom(string fn)
		{
			DProject ret = null;

			if (File.Exists(fn))
			{
				Stream stream = File.Open(fn, FileMode.Open);
				if (stream.ReadByte() == 0)
				{
					stream.Seek(0, SeekOrigin.Begin);
					BinaryFormatter formatter = new BinaryFormatter();
					try
					{
						ret = (DProject)formatter.Deserialize(stream);
					}
					catch (Exception ex) { MessageBox.Show(ex.Message); }
					stream.Close();
					if (ret != null)
					{
						ret.Modules = new List<CodeModule>();
						ret.prjfn = fn;
					}
					return ret;
				}
				stream.Seek(0, SeekOrigin.Begin);
				XmlTextReader xr = new XmlTextReader(stream);

				// now 'xml' should be the current node

				XmlReader xsr = null;

				while (xr.Read())// now 'dproject' should be the current node
				{
					if (xr.LocalName != "dproject") continue;

					ret = new DProject();
					try
					{
						ret.type = (PrjType)Convert.ToInt32(xr.GetAttribute("type"));
						ret.isRelease = xr.GetAttribute("release") == "1";
					}
					catch { }
					break;
				}

				while (xr.Read())
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						switch (xr.LocalName)
						{
							default: break;
							case "manifestcreation":
								ret.ManifestCreation = (ManifestCreationType)Convert.ToInt32(xr.GetAttribute("type"));
								break;
							case "dmd":
								ret.CompilerVersion = (CompilerConfiguration.DVersion)Convert.ToInt32(xr.GetAttribute("version"));
								break;
							case "name":
								xr.Read();
								ret.name = xr.ReadString();
								break;

							case "libs":
								if (ret.libs == null)
									ret.libs = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.NodeType == XmlNodeType.CDATA)
									{
										ret.libs.Add(xr.ReadString());
									}
								}
								break;

							case "execargs":
								xr.Read();
								ret.execargs = xr.ReadString();
								break;

							case "linkargs":
								xr.Read();
								ret.linkargs = xr.ReadString();
								break;

							case "compileargs":
								xr.Read();
								ret.compileargs = xr.ReadString();
								break;

							case "targetfilename":
								xr.Read();
								ret.targetfilename = xr.ReadString();
								break;

							case "basedir":
								xr.Read();
								ret.basedir = xr.ReadString();
								break;

							case "outputdirectory":
								xr.Read();
								ret.OutputDirectory = xr.ReadString();
								break;
							case "dbgoutputdirectory":
								xr.Read();
								ret.OutputDirectory_dbg = xr.ReadString();
								break;

							case "files":
								if (ret.Files == null)
									ret.Files = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.LocalName == "file")
									{
										long mod = 0;
										if (xsr.MoveToAttribute("lastModified"))
										{
											mod = Convert.ToInt64(xsr.Value);
											xsr.MoveToElement();
										}
										try
										{
											string _fn = xsr.ReadString();

											if (mod != 0)
												ret.AddSrc(_fn, mod);
											else
												ret.AddSrc(_fn);
										}
										catch { }
									}
								}
								break;

							case "lastopen":
								if (ret.LastOpenedFiles == null)
									ret.LastOpenedFiles = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.NodeType == XmlNodeType.CDATA)
									{
										ret.LastOpenedFiles.Add(xr.ReadString());
									}
								}
								break;

							case "FileDeps":
								if (ret.FileDependencies == null)
									ret.FileDependencies = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.NodeType == XmlNodeType.CDATA)
									{
										ret.FileDependencies.Add(xr.ReadString());
									}
								}
								break;

							case "ProjectDeps":
								if (ret.ProjectDependencies == null)
									ret.ProjectDependencies = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.NodeType == XmlNodeType.CDATA)
									{
										ret.ProjectDependencies.Add(xr.ReadString());
									}
								}
								break;

							case "enablesubversioning":
								if (xr.MoveToAttribute("value"))
								{
									ret.EnableSubversioning = xr.Value == "1";
								}
								break;

							case "alsostoresources":
								if (xr.MoveToAttribute("value"))
								{
									ret.AlsoStoreSources = xr.Value == "1";
								}
								break;

							case "lastversioncount":
								if (xr.MoveToAttribute("value"))
								{
									try
									{
										ret.LastVersionCount = Convert.ToInt32(xr.Value);
									}
									catch { }
								}
								break;
						}
					}
				}
				xr.Close();
			}
			if (ret != null)
			{
				ret.Modules = new List<CodeModule>();
				foreach (string f in ret.Files)
				{
					if (!CodeModule.Parsable(f)) continue;
					try
					{
						ret.Modules.Add(new CodeModule(ret,ret.GetPhysFilePath(f)));
					}
					catch { }
				}
				ret.prjfn = fn;
			}
			return ret;
		}

		public void Save()
		{
			if (String.IsNullOrEmpty(prjfn))
			{
				return;
			}
			XmlTextWriter xw = new XmlTextWriter(prjfn, Encoding.UTF8);
			xw.WriteStartDocument();
			xw.WriteStartElement("dproject");
			xw.WriteAttributeString("type", ((int)type).ToString());
			xw.WriteAttributeString("release", isRelease ? "1" : "0");

			xw.WriteStartElement("manifestcreation");
			xw.WriteAttributeString("type", ((int)ManifestCreation).ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("dmd");
			xw.WriteAttributeString("version",((int)CompilerVersion).ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("name");
			xw.WriteCData(name);
			xw.WriteEndElement();

			if (libs.Count > 0)
			{
				xw.WriteStartElement("libs");
				foreach (string lib in libs)
				{
					if (String.IsNullOrEmpty(lib)) continue;
					xw.WriteStartElement("lib");
					xw.WriteCData(lib);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(execargs))
			{
				xw.WriteStartElement("execargs");
				xw.WriteCData(execargs);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(linkargs))
			{
				xw.WriteStartElement("linkargs");
				xw.WriteCData(linkargs);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(compileargs))
			{
				xw.WriteStartElement("compileargs");
				xw.WriteCData(compileargs);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(targetfilename))
			{
				xw.WriteStartElement("targetfilename");
				xw.WriteCData(targetfilename);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(basedir))
			{
				xw.WriteStartElement("basedir");
				xw.WriteCData(basedir);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(OutputDirectory))
			{
				xw.WriteStartElement("outputdirectory");
				xw.WriteCData(OutputDirectory);
				xw.WriteEndElement();
			}

			if (!String.IsNullOrEmpty(OutputDirectory_dbg))
			{
				xw.WriteStartElement("dbgoutputdirectory");
				xw.WriteCData(OutputDirectory_dbg);
				xw.WriteEndElement();
			}

			if (Files != null && Files.Count > 0)
			{
				xw.WriteStartElement("files");
				foreach (string fn in Files)
				{
					if (String.IsNullOrEmpty(fn)) continue;
					xw.WriteStartElement("file");
					try
					{
						xw.WriteAttributeString("lastModified", File.GetLastWriteTimeUtc(GetPhysFilePath(fn)).ToFileTimeUtc().ToString());
					}
					catch { }
					xw.WriteCData(fn);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			if (LastOpenedFiles != null && LastOpenedFiles.Count > 0)
			{
				xw.WriteStartElement("lastopen");
				foreach (string fn in LastOpenedFiles)
				{
					xw.WriteStartElement("file");
					xw.WriteCData(fn);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			if (FileDependencies.Count > 0)
			{
				xw.WriteStartElement("FileDeps");
				foreach (string fn in FileDependencies)
				{
					if (String.IsNullOrEmpty(fn)) continue;
					xw.WriteStartElement("file");
					xw.WriteCData(fn);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			if (ProjectDependencies.Count > 0)
			{
				xw.WriteStartElement("ProjectDeps");
				foreach (string fn in ProjectDependencies)
				{
					if (String.IsNullOrEmpty(fn)) continue;
					xw.WriteStartElement("file");
					xw.WriteCData(fn);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			xw.WriteStartElement("enablesubversioning");
			xw.WriteAttributeString("value", EnableSubversioning?"1":"0");
			xw.WriteEndElement();

			xw.WriteStartElement("alsostoresources");
			xw.WriteAttributeString("value", AlsoStoreSources ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("lastversioncount");
			xw.WriteAttributeString("value", LastVersionCount.ToString());
			xw.WriteEndElement();

			xw.WriteEndElement();
			xw.Close();
		}
		#endregion
		public CodeModule FileDataByFile(string fn)
		{
			foreach (var pf in Modules)
			{
				if (pf.FileName == fn) return pf;
			}
			return null;
		}
	}
}
