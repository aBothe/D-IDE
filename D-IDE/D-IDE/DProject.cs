using System;
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
	[Serializable()]
	public class DProject
	{
		[NonSerialized()]
		public const string prjext = ".dproj";
		public enum PrjType
		{
			WindowsApp = 0,
			ConsoleApp,
			Dll,
			StaticLib,
		}

		public void ParseAll()
		{
			if (files == null) files = new List<DModule>();
			files.Clear();
			foreach (string fn in resourceFiles)
			{
				if (!DModule.Parsable(fn)) continue;
				files.Add(new DModule(GetPhysFilePath( fn)));
			}
		}

		public bool isRelease;

		public List<string> libs;
		public string execargs;
		public string linkargs;
		public string compileargs;

		public PrjType type;
		public string name;
		public string targetfilename;

		[NonSerialized()]
		public string prjfn;
		public string basedir;

		[NonSerialized()]
		public Dictionary<string, long> LastModifyingDates = new Dictionary<string, long>();

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

		public bool FileExists(string file)
		{
			if (!File.Exists(file))
			{
				return File.Exists(basedir + "\\" + file);
			}
			return true;
		}

		[NonSerialized()]
		public List<DModule> files = new List<DModule>();
		public List<string> resourceFiles = new List<string>();
		public List<string> lastopen = new List<string>();

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
			files = new List<DModule>();
			resourceFiles = new List<string>();
			lastopen = new List<string>();
		}

		public bool Contains(string file)
		{
			return resourceFiles.Contains(file);
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
		public bool AddSrc(string file,long lastModified)
		{
			string fn = file;
			string phys_fn=GetPhysFilePath(file);
			if (fn.StartsWith(basedir + "\\"))
			{
				fn = fn.Remove(0, basedir.Length + 1);
			}

			if (resourceFiles.Contains(fn))
			{
				MessageBox.Show("File \"" + fn + "\" already exists in project!");
				return false;
			}
			resourceFiles.Add(fn);
			if (LastModifyingDates.ContainsKey(phys_fn))
				LastModifyingDates[phys_fn] = lastModified;
			else
				LastModifyingDates.Add(phys_fn, lastModified);
			return true;
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

		public bool RenameFile(string from, string to)
		{
			if (!resourceFiles.Contains(from)) return false;

			DModule dm = FileDataByFile(from);
			if (dm != null) dm.mod_file = to;

			DocumentInstanceWindow diw = Form1.thisForm.FileDataByFile(from);
			if (diw != null)
			{
				diw.fileData.mod_file = to;
				diw.Update();
			}

			resourceFiles.Remove(from);
			resourceFiles.Add(to);
			return true;
		}

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
						ret.files = new List<DModule>();
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
										xr.Read();
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

							case "files":
								if (ret.resourceFiles == null)
									ret.resourceFiles = new List<string>();

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
								if (ret.lastopen == null)
									ret.lastopen = new List<string>();

								xsr = xr.ReadSubtree();
								while (xsr.Read())
								{
									if (xsr.NodeType == XmlNodeType.CDATA)
									{
										ret.lastopen.Add(xr.ReadString());
									}
								}
								break;
						}
					}
				}
				xr.Close();
			}
			if (ret != null)
			{
				ret.files = new List<DModule>();
				foreach (string f in ret.resourceFiles)
				{
					if (!DModule.Parsable(f)) continue;
					try
					{
						ret.files.Add(new DModule(ret.GetPhysFilePath( f)));
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

			xw.WriteStartElement("name");
			xw.WriteCData(name);
			xw.WriteEndElement();

			if (libs.Count > 0)
			{
				xw.WriteStartElement("libs");
				foreach (string lib in libs)
				{
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

			if (resourceFiles != null && resourceFiles.Count > 0)
			{
				xw.WriteStartElement("files");
				foreach (string fn in resourceFiles)
				{
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

			if (lastopen != null && lastopen.Count > 0)
			{
				xw.WriteStartElement("lastopen");
				foreach (string fn in lastopen)
				{
					xw.WriteStartElement("file");
					xw.WriteCData(fn);
					xw.WriteEndElement();
				}
				xw.WriteEndElement();
			}

			xw.WriteEndElement();
			xw.Close();
			/*BinaryFormatter formatter = new BinaryFormatter();

			Stream stream = File.Open(prjfn, FileMode.Create);
			formatter.Serialize(stream, this);
			stream.Close();*/
		}

		public DModule FileDataByFile(string fn)
		{
			foreach (DModule pf in files)
			{
				if (pf.mod_file == fn) return pf;
			}
			return null;
		}
	}
}
