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
using System.Diagnostics;

namespace D_IDE
{
	public class D_IDE_Properties
	{
        public static string UserDocStorageFile = Application.StartupPath + "\\SettingsAreAtUserDocs";
        public static string cfgDirName = "D-IDE.config";
        public static string cfgDir;
        public static string prop_file = "D-IDE.settings.xml";
        public static string D1ModuleCacheFile = "D-IDE.D1.cache.db";
        public static string D2ModuleCacheFile = "D-IDE.D2.cache.db";
        public static string LayoutFile = "D-IDE.layout.xml";

        /// <summary>
        /// Globally initializes all settings and essential properties
        /// </summary>
        static D_IDE_Properties()
        {
            // Determine config path
            cfgDir = (File.Exists(UserDocStorageFile)?Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments):Application.StartupPath) + "\\" + cfgDirName;

            // Create config directory
            if (!Directory.Exists(cfgDir))
                DBuilder.CreateDirectoryRecursively(cfgDir);

            bool UpdateD2Cache = true;
            try
            {
                // Load global settings
                if (!Load(cfgDir + "\\" + prop_file))
                {
                    // If no settings were loaded, launch CompilerConfiguration wizard
                    Program.StartScreen.Close();
                    Misc.SetupWizardDialog swd = new D_IDE.Misc.SetupWizardDialog(CompilerConfiguration.DVersion.D2);
                    if (swd.ShowDialog() == DialogResult.OK)
                    {
                        Default.dmd2 = swd.CompilerConfiguration;

                        // Parse include paths if wanted
                        if (!File.Exists(cfgDir + "\\" + D2ModuleCacheFile) && MessageBox.Show("Do you want to parse all of the import directories?", "Parse Imports", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                        {
                            D_IDEForm.UpdateChacheThread(Default.dmd2);
                            UpdateD2Cache = false;
                        }
                    }
                }

                // Initialize D Code Completion Icons
                D_IDEForm.InitCodeCompletionIcons();

                LoadGlobalCache(Default.dmd1, cfgDir + "\\" + D1ModuleCacheFile);
                if(UpdateD2Cache)LoadGlobalCache(Default.dmd2, cfgDir + "\\" + D2ModuleCacheFile);
            }
            catch (Exception ex)
            {
                Program.StartScreen.Close();
                MessageBox.Show(ex.Message + " (" + ex.Source + ")" + "\n\n" + ex.StackTrace, "Error while loading global settings");
            }
        }


		public static bool Load(string fn)
		{
			if (!File.Exists(fn)) return false;

            try
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

                            case "dmd":
                                CompilerConfiguration cc = CompilerConfiguration.ReadFromXML(xr);
                                if (cc.Version == CompilerConfiguration.DVersion.D1) p.dmd1 = cc;
                                else p.dmd2 = cc;
                                break;

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

                            case "shownewconsolewhenexecuting":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.ShowExternalConsoleWhenExecuting = xr.Value == "1";
                                }
                                break;
                        }
                    }
                }

                xr.Close();
                Default = p;

            }
            catch {  }
            return true;
		}

		#region Caching
		public static void LoadGlobalCache(CompilerConfiguration cc, string file)
		{
			if (!File.Exists(file)) return;
			if (cacheTh != null && cacheTh.IsAlive) return;

			Program.Parsing = true;

			//cacheTh = new Thread(delegate(object o)			{
			BinaryDataTypeStorageReader bsr = new BinaryDataTypeStorageReader(file);
			try
			{
				cc.GlobalModules = bsr.ReadModules(ref cc.ImportDirectories);
			}
			catch (Exception ex) {
                Program.StartScreen.Close();
                if (System.Diagnostics.Debugger.IsAttached) throw ex;
                MessageBox.Show(ex.Message); }
			bsr.Close();

			// add all loaded data to the precached completion list
			cc.GlobalCompletionList.Clear();
			List<ICompletionData> ilist = new List<ICompletionData>();
			DCodeCompletionProvider.AddGlobalSpaceContent(cc, ref ilist);
			cc.GlobalCompletionList = ilist;

			Program.Parsing = false;
			//});			cacheTh.Start();
		}

		static Thread cacheTh;
		public static void SaveGlobalCache(CompilerConfiguration cc, string file)
		{
			Program.Parsing = true;

			//cacheTh = new Thread(delegate(object o)			{
            if (cc.GlobalModules != null && cc.GlobalModules.Count > 1)
            {
                BinaryDataTypeStorageWriter bsw = new BinaryDataTypeStorageWriter();
                bsw.WriteModules(cc.ImportDirectories.ToArray(), cc.GlobalModules);
                MemoryStream ms = (MemoryStream)bsw.BinStream.BaseStream;
                File.WriteAllBytes(file,ms.ToArray());
                ms.Close();
                bsw.Close();
            }
			Program.Parsing = false;
			//});			cacheTh.Start();
		}
		#endregion

		public DModule GetModule(CompilerConfiguration cc, string moduleName)
		{
			foreach (DModule dm in cc.GlobalModules)
			{
				if (dm.ModuleName == moduleName) return dm;
			}
			return null;
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

			// DMD paths and args
			CompilerConfiguration.SaveToXML(Default.dmd1, xw);
			CompilerConfiguration.SaveToXML(Default.dmd2, xw);

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

            xw.WriteStartElement("shownewconsolewhenexecuting");
            xw.WriteAttributeString("value", Default.ShowExternalConsoleWhenExecuting ? "1" : "0");
            xw.WriteEndElement();

			xw.WriteEndDocument();
			xw.Close();
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
		public static bool AddFileData(CompilerConfiguration cc, DModule pf)
		{
			return AddFileData(cc.GlobalModules, pf);
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
        public bool UseRibbonMenu = false;

		public bool LogBuildProgress = true;
		public bool ShowBuildCommands = true;
		public bool UseExternalDebugger = false;
		public bool DoAutoSaveOnBuilding = true;
		public bool CreatePDBOnBuild = true;
		public bool ShowDbgPanelsOnDebugging = false;

		#region Debugging
		public bool VerboseDebugOutput = false;
		public bool SkipUnknownCode = true;
        public bool ShowExternalConsoleWhenExecuting = true;
		#endregion

		public bool EnableFXFormsDesigner = false; // For those who want to experiment a little bit ;-)
		public bool RetrieveNews = true;
		public bool SingleInstance = true;
		public bool WatchForUpdates = false;
		public string DefaultProjectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\D Projects";

		public CompilerConfiguration dmd1 = new CompilerConfiguration(CompilerConfiguration.DVersion.D1);
		public CompilerConfiguration dmd2 = new CompilerConfiguration(CompilerConfiguration.DVersion.D2);

		public static List<DModule> D1GlobalModules = new List<DModule>();
		public static List<ICompletionData> D1GlobalCompletionList = new List<ICompletionData>();
		public static List<DModule> D2GlobalModules = new List<DModule>();
		public static List<ICompletionData> D2GlobalCompletionList = new List<ICompletionData>();

		public CompilerConfiguration Compiler
		{
			set
			{
				if (value.Version == CompilerConfiguration.DVersion.D1) dmd1 = value;
				else dmd2 = value;
			}
		}
        public CompilerConfiguration DefaultCompiler
        {
            get { return dmd2; }
        }

		public string exe_dbg = "windbg.exe";
		public string dbg_args = "\"$exe\"";

		public string lastSearchDir = Application.StartupPath;

		public static ICSharpCode.NRefactory.Location fromCodeLocation(CodeLocation cloc)
		{
			return new ICSharpCode.NRefactory.Location(cloc.Column, cloc.Line);
		}
        public static CodeLocation toCodeLocation(D_Parser.Location loc)
        {
            return new CodeLocation(loc.Column, loc.Line);
        }
		public static CodeLocation toCodeLocation(ICSharpCode.NRefactory.Location loc)
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

    /*public class CodeViewToPDBConverter
    { 
        public delegate void MsgHandler(string message);
        static public event MsgHandler Message;

        static public bool DoConvert(bool isD2,string exe,string pdb)
        {
            try
            {
                Process p = DBuilder.Exec("cv2pdb.exe", (!isD2 ? "-D2 " : "-D1 ") +""+ exe + " " + pdb, Path.GetDirectoryName(exe), true);
                if (!p.WaitForExit(10000))
                {
                    p.Kill();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                return false;
            }
            return true;
        }
    }*/

	public class CompilerConfiguration
	{
		public DVersion Version = DVersion.D2;

		public string BinDirectory=".";
		public List<DModule> GlobalModules
		{
			get {
				if (Version == DVersion.D1) return D_IDE_Properties.D1GlobalModules;
				else return D_IDE_Properties.D2GlobalModules;
			}
			set
			{
				if (Version == DVersion.D1) D_IDE_Properties.D1GlobalModules = value;
				else D_IDE_Properties.D2GlobalModules = value;
			}
		}
		public List<ICompletionData> GlobalCompletionList
		{
			get {
				if (Version == DVersion.D1) return D_IDE_Properties.D1GlobalCompletionList;
				else return D_IDE_Properties.D2GlobalCompletionList;
			}
			set
			{
				if (Version == DVersion.D1) D_IDE_Properties.D1GlobalCompletionList = value;
				else D_IDE_Properties.D2GlobalCompletionList = value;
			}
		}
		public List<string> ImportDirectories = new List<string>();

		public enum DVersion
		{
			D1 = 1,
			D2 = 2,
		}

		public CompilerConfiguration(DVersion version)
		{
			Version = version;
			if (Version == DVersion.D1) BinDirectory = "C:\\dmd\\windows\\bin";
			else BinDirectory = "C:\\dmd2\\windows\\bin";
		}

		public string SoureCompiler = "dmd.exe";
		public string ExeLinker = "dmd.exe";
		public string Win32ExeLinker = "dmd.exe";
		public string DllLinker = "dmd.exe";
		public string LibLinker = "lib.exe";
		public string ResourceCompiler = "rc.exe";

		public string SoureCompilerDebugArgs = "-c \"$src\" -of\"$obj\" -gc";
        public string Win32ExeLinkerDebugArgs = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -gc";
		public string ExeLinkerDebugArgs = "$objs $libs -of\"$exe\" -gc";
		public string DllLinkerDebugArgs = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc";
		public string LibLinkerDebugArgs = "-c -n \"$lib\" $objs";

		public string SoureCompilerArgs = "-c \"$src\" -of\"$obj\" -release";
        public string Win32ExeLinkerArgs = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -release";
		public string ExeLinkerArgs = "$objs $libs -of\"$exe\" -release";
		public string DllLinkerArgs = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -release";
        public string LibLinkerArgs = "-c -n \"$lib\" $objs";

		public string ResourceCompilerArgs = "/fo\"$res\" \"$rc\"";

		public static void SaveToXML(CompilerConfiguration cc, XmlWriter xml)
		{
			xml.WriteStartElement("dmd"); // <dmd>
			xml.WriteAttributeString("version", ((int)cc.Version).ToString());

			xml.WriteStartElement("binpath");
			xml.WriteCData(cc.BinDirectory);
			xml.WriteEndElement();

			xml.WriteStartElement("imports");
			foreach (string dir in cc.ImportDirectories)
			{
				xml.WriteStartElement("dir"); 
				xml.WriteCData(dir); 
				xml.WriteEndElement();
			}
			xml.WriteEndElement();

			xml.WriteStartElement("dtoobj");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.SoureCompiler);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.SoureCompilerArgs);
			xml.WriteEndElement();
			xml.WriteStartElement("dbgargs");
			xml.WriteCData(cc.SoureCompilerDebugArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteStartElement("objtowinexe");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.Win32ExeLinker);
			xml.WriteEndElement();
			xml.WriteStartElement("dbgargs");
			xml.WriteCData(cc.Win32ExeLinkerDebugArgs);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.Win32ExeLinkerArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteStartElement("objtoexe");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.ExeLinker);
			xml.WriteEndElement();
			xml.WriteStartElement("dbgargs");
			xml.WriteCData(cc.ExeLinkerDebugArgs);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.ExeLinkerArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteStartElement("objtodll");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.DllLinker);
			xml.WriteEndElement();
			xml.WriteStartElement("dbgargs");
			xml.WriteCData(cc.DllLinkerDebugArgs);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.DllLinkerArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteStartElement("objtolib");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.LibLinker);
			xml.WriteEndElement();
			xml.WriteStartElement("dbgargs");
			xml.WriteCData(cc.LibLinkerDebugArgs);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.LibLinkerArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteStartElement("rctores");
			xml.WriteStartElement("bin");
			xml.WriteCData(cc.ResourceCompiler);
			xml.WriteEndElement();
			xml.WriteStartElement("args");
			xml.WriteCData(cc.ResourceCompilerArgs);
			xml.WriteEndElement();
			xml.WriteEndElement();

			xml.WriteEndElement(); // </dmd>
		}

		public static CompilerConfiguration ReadFromXML(XmlReader xr)
		{
			CompilerConfiguration cc = new CompilerConfiguration(DVersion.D2);

			if (xr.LocalName != "dmd") return null;
			cc.Version = (DVersion)Convert.ToInt32(xr.GetAttribute("version"));

			while (xr.Read())// now 'settings' should be the current node
			{
				if (xr.LocalName == "dmd" && xr.NodeType == XmlNodeType.EndElement) break;
				if (xr.NodeType == XmlNodeType.Element)
				{
					switch (xr.LocalName)
					{
						default: break;

						case "binpath":
							if (xr.IsEmptyElement) break;
							cc.BinDirectory = xr.ReadString();
							break;

						case "parsedirectories":
						case "imports":
							if (xr.IsEmptyElement) break;
							while (xr.Read())
							{
								if (xr.LocalName == "dir")
									cc.ImportDirectories.Add(xr.ReadString());
								else break;
							}
							break;

						case "dtoobj":
						case "objtowinexe":
						case "objtoexe":
						case "objtodll":
						case "objtolib":
						case "rctores":
							if (xr.IsEmptyElement) break;
							string binname = xr.LocalName;
							string bin = null, arg = null, dbgarg = null;
							while (xr.Read())
							{
								if (xr.LocalName == "bin")
								{
									bin = xr.ReadString();
								}
								else if (xr.LocalName == "dbgargs")
								{
									dbgarg = xr.ReadString();
								}
								else if (xr.LocalName == "args")
								{
									arg = xr.ReadString();
								}
								else break;
							}

							switch (binname)
							{
								default: break;
								case "dtoobj":
									cc.SoureCompiler = bin;
									cc.SoureCompilerArgs = arg;
									cc.SoureCompilerDebugArgs = dbgarg;
									break;
								case "objtowinexe":
									cc.Win32ExeLinker = bin;
									cc.Win32ExeLinkerArgs = arg;
									cc.Win32ExeLinkerDebugArgs = dbgarg;
									break;
								case "objtoexe":
									cc.ExeLinker = bin;
									cc.ExeLinkerArgs = arg;
									cc.ExeLinkerDebugArgs = dbgarg;
									break;
								case "objtodll":
									cc.DllLinker = bin;
									cc.DllLinkerArgs = arg;
									cc.DllLinkerDebugArgs = dbgarg;
									break;
								case "objtolib":
									cc.LibLinker = bin;
									cc.LibLinkerArgs = arg;
									cc.LibLinkerDebugArgs = dbgarg;
									break;
								case "rctores":
									cc.ResourceCompiler = bin;
									cc.ResourceCompilerArgs = arg;
									break;
							}

							break;
					}
				}
			}


			return cc;
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
			if (shinfo.hIcon == null) return D_IDEForm.thisForm.Icon;
			try
			{
				Icon ret = (System.Drawing.Icon.FromHandle(shinfo.hIcon));
				return ret;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, FilePath);
				return D_IDEForm.thisForm.Icon;
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
