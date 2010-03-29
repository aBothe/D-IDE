using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using ICSharpCode.TextEditor.Document;
using System.IO;
using System.Xml;
using D_Parser;
using ICSharpCode.NRefactory;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ICSharpCode.TextEditor;
using D_IDE.Properties;
using ICSharpCode.SharpDevelop.Dom;

namespace D_IDE
{
	public class D_IDE_Properties
	{
		public static void Load(string fn)
		{
			if (File.Exists(fn))
			{
				BinaryFormatter formatter = new BinaryFormatter();

				Stream stream = File.Open(fn, FileMode.Open);

				XmlTextReader xr = new XmlTextReader(stream);
				D_IDE_Properties p = new D_IDE_Properties();

				while (xr.Read())// now 'settings' should be the current node
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						switch (xr.LocalName)
						{
							default: break;

							case "recentprojects":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "f")
									{
										try
										{
											p.lastProjects.Add(xr.ReadString());
										}
										catch { }
									}
									else break;
								}
								break;

							case "recentfiles":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "f")
									{
										try
										{
											p.lastFiles.Add(xr.ReadString());
										}
										catch { }
									}
									else break;
								}
								break;

							case "lastopenedfiles":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "f")
									{
										try
										{
											p.lastOpenFiles.Add(xr.ReadString());
										}
										catch { }
									}
									else break;
								}
								break;


							case "openlastprj":
								if (xr.MoveToAttribute("value"))
								{
									p.OpenLastPrj = xr.Value == "1";
								}
								break;

							case "openlastfiles":
								if (xr.MoveToAttribute("value"))
								{
									p.OpenLastFiles = xr.Value == "1";
								}
								break;

							case "windowstate":
								if (xr.MoveToAttribute("value"))
								{
									try { p.lastFormState = (FormWindowState)Convert.ToInt32(xr.Value); }
									catch { }
								}
								break;

							case "windowsize":
								if (xr.MoveToAttribute("x"))
								{
									try { p.lastFormSize.Width = Convert.ToInt32(xr.Value); }
									catch { }
								}
								if (xr.MoveToAttribute("y"))
								{
									try { p.lastFormSize.Height = Convert.ToInt32(xr.Value); }
									catch { }
								}
								break;

							case "retrievenews":
								if (xr.MoveToAttribute("value"))
								{
									p.RetrieveNews = xr.Value == "1";
								}
								break;

							case "logbuildstatus":
								if (xr.MoveToAttribute("value"))
								{
									p.LogBuildProgress = xr.Value == "1";
								}
								break;

							case "showbuildcommands":
								if (xr.MoveToAttribute("value"))
								{
									p.ShowBuildCommands = xr.Value == "1";
								}
								break;

							case "externaldbg":
								if (xr.MoveToAttribute("value"))
								{
									p.UseExternalDebugger = xr.Value == "1";
								}
								break;

							case "singleinstance":
								if (xr.MoveToAttribute("value"))
								{
									p.SingleInstance = xr.Value == "1";
								}
								break;

							case "watchforupdates":
								if (xr.MoveToAttribute("value"))
								{
									p.WatchForUpdates = xr.Value == "1";
								}
								break;

							case "defprjdir":
								p.DefaultProjectDirectory = xr.ReadString();
								break;

							case "parsedirectories":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "dir")
										p.parsedDirectories.Add(xr.ReadString());
									else break;
								}
								break;

							case "dtoobj":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_cmp = xr.ReadString();
									}
									else if (xr.LocalName == "dbgargs")
									{
										p.cmp_obj_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.cmp_obj = xr.ReadString();
									}
									else break;
								}
								break;

							case "objtowinexe":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_win = xr.ReadString();
									}
									else if (xr.LocalName == "dbgargs")
									{
										p.link_win_exe_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.link_win_exe = xr.ReadString();
									}
									else break;
								}
								break;

							case "objtoexe":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_console = xr.ReadString();
									}
									else if (xr.LocalName == "dbgargs")
									{
										p.link_to_exe_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.link_to_exe = xr.ReadString();
									}
									else break;
								}
								break;

							case "objtodll":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_dll = xr.ReadString();
									}
									else if (xr.LocalName == "dbgargs")
									{
										p.link_to_dll_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.link_to_dll = xr.ReadString();
									}
									else break;
								}
								break;

							case "objtolib":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_lib = xr.ReadString();
									}
									else if (xr.LocalName == "dbgargs")
									{
										p.link_to_lib_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.link_to_lib = xr.ReadString();
									}
									else break;
								}
								break;

							case "rctores":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_res = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.cmp_res = xr.ReadString();
									}
									else break;
								}
								break;

							case "debugger":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "bin")
									{
										p.exe_dbg = xr.ReadString();
									}
									else if (xr.LocalName == "args")
									{
										p.dbg_args = xr.ReadString();
									}
									else break;
								}
								break;

							case "lastsearchdir":
								p.lastSearchDir = xr.ReadString();
								break;

							case "verbosedbgoutput":
								if (xr.MoveToAttribute("value"))
								{
									p.VerboseDebugOutput = xr.Value == "1";
								}
								break;

							case "skipunknowncode":
								if (xr.MoveToAttribute("value"))
								{
									p.SkipUnknownCode = xr.Value == "1";
								}
								break;

							case "showdbgpanelswhendebugging":
								if (xr.MoveToAttribute("value"))
								{
									p.ShowDbgPanelsOnDebugging = xr.Value == "1";
								}
								break;

							case "autosave":
								if (xr.MoveToAttribute("value"))
								{
									p.DoAutoSaveOnBuilding = xr.Value == "1";
								}
								break;

							case "createpdb":
								if (xr.MoveToAttribute("value"))
								{
									p.CreatePDBOnBuild = xr.Value == "1";
								}
								break;

							case "highlightings":
								if (xr.IsEmptyElement) break;
								while (xr.Read())
								{
									if (xr.LocalName == "f")
									{
										try
										{
											string ext = xr.GetAttribute("ext");
											p.SyntaxHighlightingEntries.Add(ext, xr.ReadString());
										}
										catch { }
									}
									else break;
								}
								break;
						}
					}
				}

				xr.Close();
				Default = p;
			}
		}

		#region Caching
		public static void LoadGlobalCache(string file)
		{
			if (!File.Exists(file)) return;
			if (cacheTh != null && cacheTh.IsAlive) return;

			Program.Parsing = true;

			cacheTh = new Thread(delegate(object o)
			{
				if (cscr == null || cscr.IsDisposed) cscr = new CachingScreen();
				cscr.Show();

				BinaryDataTypeStorageReader bsr = new BinaryDataTypeStorageReader(file);
				try
				{
					GlobalModules = bsr.ReadModules();
				}
				catch (Exception ex) { MessageBox.Show(ex.Message); }
				bsr.Close();

				cscr.Close();

				// add all loaded data to the precached completion list
				D_IDE_Properties.GlobalCompletionList.Clear();
				if (Form1.thisForm != null && Form1.thisForm.icons != null)
					DCodeCompletionProvider.AddGlobalSpaceContent(ref D_IDE_Properties.GlobalCompletionList, Form1.thisForm.icons);

				Program.Parsing = false;
			});
			cacheTh.Start();

		}

		static Thread cacheTh;
		static CachingScreen cscr;
		public static void SaveGlobalCache(string file)
		{
			if (cacheTh != null && cacheTh.IsAlive) return;
			int i = 0;
			while (Program.Parsing && i < 500) { Thread.Sleep(50); i++; }

			Program.Parsing = true;

			cacheTh = new Thread(delegate(object o)
			{
				if (cscr == null || cscr.IsDisposed) cscr = new CachingScreen();
				cscr.Show();

				BinaryDataTypeStorageWriter bsw = new BinaryDataTypeStorageWriter(file);
				bsw.WriteModules(GlobalModules);
				bsw.Close();

				Program.Parsing = false;
			}); cacheTh.Start();
		}
		#endregion

		public DModule this[string moduleName]
		{
			get
			{
				foreach (DModule dm in GlobalModules)
				{
					if (dm.ModuleName == moduleName) return dm;
				}
				return null;
			}
			set
			{
				int i = 0;
				foreach (DModule dm in GlobalModules)
				{
					if (dm.ModuleName == moduleName)
					{
						GlobalModules[i] = value;
						return;
					}
					i++;
				}
				AddFileData(value);
			}
		}

		public static void Save(string fn)
		{
			if (fn == null) return;
			if (fn == "") return;

			XmlTextWriter xw = new XmlTextWriter(fn, Encoding.UTF8);
			xw.WriteStartDocument();
			xw.WriteStartElement("settings");

			xw.WriteStartElement("recentprojects");
			foreach (string f in Default.lastProjects)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("recentfiles");
			foreach (string f in Default.lastFiles)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("lastopenedfiles");
			foreach (string f in Default.lastOpenFiles)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("openlastprj");
			xw.WriteAttributeString("value", Default.OpenLastPrj ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("openlastfiles");
			xw.WriteAttributeString("value", Default.OpenLastFiles ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("windowstate");
			xw.WriteAttributeString("value", ((int)Default.lastFormState).ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("windowsize");
			xw.WriteAttributeString("x", Default.lastFormSize.Width.ToString());
			xw.WriteAttributeString("y", Default.lastFormSize.Height.ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("retrievenews");
			xw.WriteAttributeString("value", Default.RetrieveNews ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("logbuildstatus");
			xw.WriteAttributeString("value", Default.LogBuildProgress ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("showbuildcommands");
			xw.WriteAttributeString("value", Default.ShowBuildCommands ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("externaldbg");
			xw.WriteAttributeString("value", Default.UseExternalDebugger ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("singleinstance");
			xw.WriteAttributeString("value", Default.SingleInstance ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("watchforupdates");
			xw.WriteAttributeString("value", Default.WatchForUpdates ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("defprjdir");
			xw.WriteCData(Default.DefaultProjectDirectory);
			xw.WriteEndElement();

			xw.WriteStartElement("parsedirectories");
			foreach (string dir in Default.parsedDirectories)
			{
				xw.WriteStartElement("dir"); xw.WriteCData(dir); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			// d source to obj
			xw.WriteStartElement("dtoobj");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_cmp);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.cmp_obj);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("objtowinexe");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_win);
			xw.WriteEndElement();
			xw.WriteStartElement("dbgargs");
			xw.WriteCData(Default.link_win_exe_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.link_win_exe);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("objtoexe");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_console);
			xw.WriteEndElement();
			xw.WriteStartElement("dbgargs");
			xw.WriteCData(Default.link_to_exe_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.link_to_exe);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("objtodll");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_dll);
			xw.WriteEndElement();
			xw.WriteStartElement("dbgargs");
			xw.WriteCData(Default.link_to_dll_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.link_to_dll);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("objtolib");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_lib);
			xw.WriteEndElement();
			xw.WriteStartElement("dbgargs");
			xw.WriteCData(Default.link_to_lib_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.link_to_lib);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("rctores");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_res);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.cmp_res);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("debugger");
			xw.WriteStartElement("bin");
			xw.WriteCData(Default.exe_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Default.dbg_args);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("lastsearchdir");
			xw.WriteCData(Default.lastSearchDir);
			xw.WriteEndElement();

			xw.WriteStartElement("verbosedbgoutput");
			xw.WriteAttributeString("value", Default.VerboseDebugOutput ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("skipunknowncode");
			xw.WriteAttributeString("value", Default.SkipUnknownCode ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("showdbgpanelswhendebugging");
			xw.WriteAttributeString("value", Default.ShowDbgPanelsOnDebugging ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("autosave");
			xw.WriteAttributeString("value", Default.DoAutoSaveOnBuilding ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("createpdb");
			xw.WriteAttributeString("value", Default.CreatePDBOnBuild ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("highlightings");
			foreach (string ext in Default.SyntaxHighlightingEntries.Keys)
			{
				if (String.IsNullOrEmpty(Default.SyntaxHighlightingEntries[ext])) continue;
				xw.WriteStartElement("f");
				xw.WriteAttributeString("ext", ext);
				xw.WriteCData(Default.SyntaxHighlightingEntries[ext]);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteEndDocument();
			xw.Close();
		}

		public static bool HasModule(string file)
		{
			foreach (DModule dpf in GlobalModules)
			{
				if (dpf.mod_file == file)
				{
					return true;
				}
			}
			return false;
		}
		public static bool HasModule(List<DModule> modules, string file)
		{
			foreach (DModule dpf in modules)
			{
				if (dpf.mod_file == file)
				{
					return true;
				}
			}
			return false;
		}
		public static bool AddFileData(DModule pf)
		{
			if (!pf.IsParsable) return false;



			foreach (DModule dpf in GlobalModules)
			{
				if (dpf.FileName == pf.FileName)
				{
					dpf.dom = pf.dom;
					dpf.folds = pf.folds;
					dpf.ModuleName = pf.ModuleName;
					dpf.import = pf.import;
					return true;
				}
			}

			GlobalModules.Add(pf);

			return true;
		}
		public static bool AddFileData(List<DModule> modules, DModule pf)
		{
			if (!pf.IsParsable) return false;

			foreach (DModule dpf in modules)
			{
				if (dpf.mod_file == pf.mod_file)
				{
					dpf.dom = pf.dom;
					dpf.folds = pf.folds;
					dpf.ModuleName = pf.ModuleName;
					dpf.import = pf.import;
					return true;
				}
			}

			modules.Add(pf);
			return true;
		}

		public D_IDE_Properties()
		{
			exe_cmp = exe_console = exe_dll = exe_lib = exe_win = "dmd.exe";
		}

		public static D_IDE_Properties Default = new D_IDE_Properties();

		public List<string>
			lastProjects = new List<string>(),
			lastFiles = new List<string>(),
			lastOpenFiles = new List<string>();

		/// <summary>
		/// Stores currently opened projects
		/// </summary>
		static public Dictionary<string, DProject> Projects = new Dictionary<string, DProject>();
		static public DProject GetProject(string ProjectFile)
		{
			if (!Projects.ContainsKey(ProjectFile))
				return null;
			return Projects[ProjectFile];
		}

		public bool OpenLastPrj = true;
		public bool OpenLastFiles = true;
		public FormWindowState lastFormState = FormWindowState.Maximized;
		public Point lastFormLocation;
		public Size lastFormSize;
		public Dictionary<string, string> SyntaxHighlightingEntries = new Dictionary<string, string>();

		public bool LogBuildProgress = true;
		public bool ShowBuildCommands = true;
		public bool UseExternalDebugger = false;
		public bool DoAutoSaveOnBuilding = true;
		public bool CreatePDBOnBuild = true;
		public bool ShowDbgPanelsOnDebugging = false;

		#region Debugging
		public bool VerboseDebugOutput = false;
		public bool SkipUnknownCode = true;
		#endregion

		public bool EnableFXFormsDesigner = false; // For those who want to experiment a little bit ;-)
		public bool RetrieveNews = true;
		public bool SingleInstance = true;
		public bool WatchForUpdates = true;
		public string DefaultProjectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\D Projects";

		public static List<DModule> GlobalModules = new List<DModule>();
		public static List<ICompletionData> GlobalCompletionList = new List<ICompletionData>();
		public List<string> parsedDirectories = new List<string>();

		public string exe_cmp;
		public string exe_win;
		public string exe_console;
		public string exe_dll;
		public string exe_lib;
		public string exe_res = "rc.exe";
		public string exe_dbg = "windbg.exe";

		public string cmp_obj_dbg = "-c \"$src\" -of\"$obj\" -gc";
		public string link_win_exe_dbg = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -gc";
		public string link_to_exe_dbg = "$objs $libs -of\"$exe\" -gc";
		public string link_to_dll_dbg = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc";
		public string link_to_lib_dbg = "$objs $libs -of\"$lib\"";

		public string cmp_obj = "-c \"$src\" -of\"$obj\" -release";
		public string link_win_exe = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -release";
		public string link_to_exe = "$objs $libs -of\"$exe\" -release";
		public string link_to_dll = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -release";
		public string link_to_lib = "$objs $libs -of\"$lib\"";

		public string cmp_res = "/fo\"$res\" \"$rc\"";
		public string dbg_args = "\"$exe\"";

		public string lastSearchDir = Application.StartupPath;

		public static Location fromCodeLocation(CodeLocation cloc)
		{
			return new Location(cloc.Column, cloc.Line);
		}
		public static CodeLocation toCodeLocation(Location loc)
		{
			return new CodeLocation(loc.Column, loc.Line);
		}
		public static DateTime DateFromUnixTime(long t)
		{
			DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return ret.AddSeconds(t);
		}
		public static long UnixTimeFromDate(DateTime t)
		{
			DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return (long)(t - ret).TotalSeconds;
		}
		public static CodeLocation toCodeLocation(TextLocation Caret)
		{
			return new CodeLocation(Caret.Column + 1, Caret.Line + 1);
		}
	}

	class SyntaxFileProvider : ISyntaxModeFileProvider
	{
		public List<SyntaxMode> modes;
		public SyntaxFileProvider()
		{
			modes = new List<SyntaxMode>();
			if (!D_IDE_Properties.Default.SyntaxHighlightingEntries.ContainsKey(".d")) modes.Add(new SyntaxMode(Application.StartupPath + "\\D.xshd", "D", ".d"));
			if (!D_IDE_Properties.Default.SyntaxHighlightingEntries.ContainsKey(".rc")) modes.Add(new SyntaxMode(Application.StartupPath + "\\RC.xshd", "RC", ".rc"));

			foreach (string ext in D_IDE_Properties.Default.SyntaxHighlightingEntries.Keys)
			{
				modes.Add(new SyntaxMode(D_IDE_Properties.Default.SyntaxHighlightingEntries[ext], ext.Trim('.').ToUpperInvariant(), ext));
			}
		}

		#region ISyntaxModeFileProvider Member

		public System.Xml.XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
		{
			if (File.Exists(syntaxMode.FileName))
				return new XmlTextReader(new FileStream(syntaxMode.FileName, FileMode.Open, FileAccess.Read));
			else
				return new XmlTextReader(new StringReader(Resources.ResourceManager.GetString(syntaxMode.Name)));
		}

		public ICollection<SyntaxMode> SyntaxModes
		{
			get { return (ICollection<SyntaxMode>)modes; }
		}

		public void UpdateSyntaxModeList() { }

		#endregion
	}
	#region Low Level

	/// <summary>
	/// ICSharpCode.SharpDevelop.Dom was created by extracting code from ICSharpCode.SharpDevelop.dll.
	/// There are a few static method calls that refer to GUI code or the code for keeping the parse
	/// information. These calls have to be implemented by the application hosting
	/// ICSharpCode.SharpDevelop.Dom by settings static fields with a delegate to their method
	/// implementation.
	/// </summary>
	static class HostCallbackImplementation
	{
		public static void Register()
		{
			// Must be implemented. Gets the project content of the active project.
			HostCallback.GetCurrentProjectContent = delegate
			{
				return null;// mainForm.myProjectContent;
			};

			// The default implementation just logs to Log4Net. We want to display a MessageBox.
			// Note that we use += here - in this case, we want to keep the default Log4Net implementation.
			HostCallback.ShowError += delegate(string message, Exception ex)
			{
				MessageBox.Show(message + Environment.NewLine + ex.ToString());
			};
			HostCallback.ShowMessage += delegate(string message)
			{
				MessageBox.Show(message);
			};
			HostCallback.ShowAssemblyLoadError += delegate(string fileName, string include, string message)
			{
				MessageBox.Show("Error loading code-completion information for "
						+ include + " from " + fileName
						+ ":\r\n" + message + "\r\n");
			};
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct SHFILEINFO
	{
		public IntPtr hIcon;
		public IntPtr iIcon;
		public uint dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}

	public class ExtractIcon
	{
		/// <summary>
		/// Methode zum extrahieren von einem Icon aus einer Datei.
		/// </summary>
		/// <param name="FilePath">Hier übergeben Sie den Pfad der Datei von dem das Icon extrahiert werden soll.</param>
		/// <param name="Small">Bei übergabe von true wird ein kleines und bei false ein großes Icon zurück gegeben.</param>
		public static Icon GetIcon(string FilePath, bool Small)
		{
			IntPtr hImgSmall;
			IntPtr hImgLarge;
			SHFILEINFO shinfo = new SHFILEINFO();
			if (Small)
			{
				hImgSmall = Win32.SHGetFileInfo(Path.GetFileName(FilePath), 0,
					ref shinfo, (uint)Marshal.SizeOf(shinfo),
					Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_USEFILEATTRIBUTES);
			}
			else
			{
				hImgLarge = Win32.SHGetFileInfo(Path.GetFileName(FilePath), 0,
					ref shinfo, (uint)Marshal.SizeOf(shinfo),
					Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON | Win32.SHGFI_USEFILEATTRIBUTES);
			}
			if (shinfo.hIcon == null) return Form1.thisForm.Icon;
			try
			{
				Icon ret = (System.Drawing.Icon.FromHandle(shinfo.hIcon));
				return ret;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, FilePath);
				return Form1.thisForm.Icon;
			}
		}
	}

	/// <summary>
	/// DLL Definition für IconExtract.
	/// </summary>
	class Win32
	{
		public const uint SHGFI_ICON = 0x100;
		public const uint SHGFI_LARGEICON = 0x0;
		public const uint SHGFI_SMALLICON = 0x1;
		public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

		[DllImport("shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath,
			uint dwFileAttributes,
			ref SHFILEINFO psfi,
			uint cbSizeFileInfo,
			uint uFlags);
		[DllImport("user32.dll")]
		public static extern int DestroyIcon(IntPtr hIcon);
	}
	#endregion
}
