using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using D_IDE.CodeCompletion;
using D_Parser;
using D_Parser.CodeCompletion;
using D_IDE.Properties;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Gui.InsightWindow;
using WeifenLuo.WinFormsUI.Docking;
using System.Globalization;

namespace D_IDE
{
	class DocumentInstanceWindow : DockContent
	{
		public TextEditorControl txt;

		private System.Windows.Forms.ContextMenuStrip tcCont;
		private System.Windows.Forms.ToolStripMenuItem goToDefinitionToolStripMenuItem, createImportDirectiveItem;

		public DModule fileData;
		public DProject project;
		public bool Modified;

		public new void Update()
		{
			this.Text = Path.GetFileName(fileData.mod_file) + (Modified ? " *" : "");
			base.Update();
		}

		public void EmulateCopy()
		{
			txt.ActiveTextAreaControl.TextArea.ExecuteDialogKey(Keys.C | Keys.Control);
		}
		public void EmulateCut()
		{
			txt.ActiveTextAreaControl.TextArea.ExecuteDialogKey(Keys.X | Keys.Control);
		}
		public void EmulatePaste()
		{
			txt.ActiveTextAreaControl.TextArea.ExecuteDialogKey(Keys.V | Keys.Control);
		}

		void Init(string fn)
		{
			this.DockAreas = DockAreas.Document;

			fileData = new DModule(fn);

			Update();
			txt = new TextEditorControl();
			txt.Dock = DockStyle.Fill;
			this.Controls.Add(txt);

			txt.TextEditorProperties.AllowCaretBeyondEOL = false;
			txt.TextEditorProperties.AutoInsertCurlyBracket = true;
			txt.TextEditorProperties.BracketMatchingStyle = BracketMatchingStyle.Before;
			txt.TextEditorProperties.ConvertTabsToSpaces = false;
			txt.TextEditorProperties.DocumentSelectionMode = DocumentSelectionMode.Normal;
			txt.TextEditorProperties.EnableFolding = true;
			txt.TextEditorProperties.IsIconBarVisible = false;
			txt.TextEditorProperties.LineViewerStyle = LineViewerStyle.FullRow;

			txt.TextEditorProperties.ShowEOLMarker = false;
			txt.TextEditorProperties.ShowHorizontalRuler = false;
			txt.TextEditorProperties.ShowInvalidLines = false;
			txt.TextEditorProperties.ShowLineNumbers = true;
			txt.TextEditorProperties.ShowMatchingBracket = true;
			txt.TextEditorProperties.ShowTabs = false;
			txt.TextEditorProperties.ShowSpaces = true;
			txt.TextEditorProperties.ShowVerticalRuler = false;

			txt.SetHighlighting(Path.GetExtension(fn).TrimStart(new char[] { '.' }).ToUpper());
			txt.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(Caret_PositionChanged);

			if (DModule.Parsable(fn))
			{
				txt.Document.FormattingStrategy = new DFormattingStrategy();
				txt.Document.TextContentChanged += Document_TextContentChanged;
				txt.ActiveTextAreaControl.TextArea.ToolTipRequest += TextArea_ToolTipRequest;
				txt.ActiveTextAreaControl.TextArea.KeyEventHandler += TextAreaKeyEventHandler;
			}

			txt.TextEditorProperties.AutoInsertCurlyBracket = true;
			txt.TextEditorProperties.IndentStyle = IndentStyle.Smart;


			this.tcCont = new System.Windows.Forms.ContextMenuStrip();
			this.goToDefinitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			createImportDirectiveItem = new ToolStripMenuItem();

			ToolStripMenuItem tmi1 = new ToolStripMenuItem("Copy", global::D_IDE.Properties.Resources.copy, new EventHandler(delegate(object sender, EventArgs ea)
				{
					EmulateCopy();
				}));
			ToolStripMenuItem tmi2 = new ToolStripMenuItem("Cut", global::D_IDE.Properties.Resources.cut, new EventHandler(delegate(object sender, EventArgs ea)
			{
				EmulateCut();
			}));
			ToolStripMenuItem tmi3 = new ToolStripMenuItem("Paste", global::D_IDE.Properties.Resources.paste, new EventHandler(delegate(object sender, EventArgs ea)
			{
				EmulatePaste();
			}));

			this.tcCont.SuspendLayout();
			// 
			// tcCont
			// 
			this.tcCont.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tmi1, tmi2, tmi3, new ToolStripSeparator(), this.goToDefinitionToolStripMenuItem, createImportDirectiveItem });
			this.tcCont.Name = "tcCont";
			this.tcCont.Size = new System.Drawing.Size(158, 120);
			// 
			// goToDefinitionToolStripMenuItem
			// 
			this.goToDefinitionToolStripMenuItem.Name = "goToDefinitionToolStripMenuItem";
			this.goToDefinitionToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.goToDefinitionToolStripMenuItem.Text = "Go to definition";
			this.goToDefinitionToolStripMenuItem.Click += goToDefinitionToolStripMenuItem_Click;
			// 
			// createImportDirectiveItem
			// 
			createImportDirectiveItem.Name = "createImportDirectiveItem";
			createImportDirectiveItem.Size = new System.Drawing.Size(170, 22);
			createImportDirectiveItem.Text = "Create import directive";
			createImportDirectiveItem.Click += new EventHandler(createImportDirectiveItem_Click);

			this.tcCont.ResumeLayout(false);
			txt.ContextMenuStrip = tcCont;
		}

		public int CaretOffset
		{
			get { return txt.ActiveTextAreaControl.Caret.Offset; }
		}
		public TextLocation Caret
		{
			get { return txt.ActiveTextAreaControl.Caret.Position; }
		}

		void Caret_PositionChanged(object sender, EventArgs e)
		{
			Form1.thisForm.LineLabel.Text =
				"Line " + (txt.ActiveTextAreaControl.Caret.Line + 1).ToString() +
				" Col " + (txt.ActiveTextAreaControl.Caret.Column).ToString();
			DataType tv = DCodeCompletionProvider.GetBlockAt(fileData.dom, Caret);

			if (Form1.thisForm.CurBlockEnts.Tag != tv || Form1.thisForm.CurBlockEnts.Tag == null)
			{
				DCodeCompletionProvider.GenerateCurrentGlobalData(txt.ActiveTextAreaControl.TextArea, project, fileData);
				Form1.thisForm.CurBlockEnts.DropDownItems.Clear();
				Form1.thisForm.CurBlockEnts.Tag = tv;
				Form1.thisForm.CurBlockEnts.Image = Form1.thisForm.icons.Images[DCompletionData.GetImageIndex(Form1.thisForm.icons, (DataType)tv.Parent, (DataType)tv)];
				Form1.thisForm.CurBlockEnts.Text = tv.name;
				Form1.thisForm.CurBlockEnts.ToolTipText = tv.desc;

				foreach (DataType ch in tv)
				{
					if (Form1.thisForm.CurBlockEnts.DropDownItems.Count > 60)
					{
						Form1.thisForm.CurBlockEnts.DropDownItems.Add("[Too much elements]");
						break;
					}
					ToolStripMenuItem tsmi = new ToolStripMenuItem(
						DCompletionData.BuildDescriptionString(ch, false),
						Form1.thisForm.icons.Images[DCompletionData.GetImageIndex(Form1.thisForm.icons, (DataType)tv, (DataType)ch)]
						, delegate(Object s, EventArgs ea)
						{
							ToolStripMenuItem mi = (ToolStripMenuItem)s;
							DocumentInstanceWindow diw = Form1.SelectedTabPage;
							if (diw != null)
							{
								diw.txt.ActiveTextAreaControl.Caret.Position = new TextLocation((mi.Tag as DataType).startLoc.Column - 1, (mi.Tag as DataType).startLoc.Line - 1);
								diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
							}
						});
					tsmi.Tag = ch;
					Form1.thisForm.CurBlockEnts.DropDownItems.Add(tsmi);
				}
			}
		}

		void createImportDirectiveItem_Click(object sender, EventArgs e)
		{
			int off = txt.ActiveTextAreaControl.Caret.Offset;
			bool ctor, super, isInst, isNameSpace;

			string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(ref off, out ctor, false);
			if (exprs == null || exprs.Length < 1)
			{
				MessageBox.Show("Nothing selected!");
				return;
			}

			int key = DKeywords.GetToken(exprs[0]);
			if (key != -1 && key != DTokens.This && key != DTokens.Super) return;
			DModule gpf = null;
			DataType dt =
				DCodeCompletionProvider.FindActualExpression(project,
					fileData,
					D_IDE_Properties.toCodeLocation(Caret),
					exprs,
					false,
					out super,
					out isInst,
					out isNameSpace,
					out gpf
					);

			if (gpf == null || dt == null) return;

			if (fileData.import.Contains(dt.module))
			{
				MessageBox.Show("Import directive is already existing!");
				return;
			}

			TextLocation tl = txt.ActiveTextAreaControl.Caret.Position;
			string inss = "import " + dt.module + ";\r\n";
			txt.Document.TextContent = txt.Document.TextContent.Insert(0, inss);
			tl.Line++;
			txt.ActiveTextAreaControl.Caret.Position = tl;
		}

		/// <summary>
		/// Return true to handle the keypress, return false to let the text area handle the keypress
		/// </summary>
		bool TextAreaKeyEventHandler(char key)
		{
			if (Program.Parsing) return false;
			Modified = true;
			Update();
			if (key == '(')
			{
				txt.Document.Insert(CaretOffset, ")");
			}
			if (key == '(' || key == ',')
			{
				ShowFunctionParameterToolTip(key);
				return false;
			}

			ICompletionDataProvider dataProvider = null;

			DProject prj = Form1.thisForm.prj;
			if (prj == null) prj = new DProject();

			if (Char.IsLetterOrDigit(key) || key == '_' || key == '.' || key == ' ')
				dataProvider = new DCodeCompletionProvider(ref prj, Form1.thisForm.icons);
			else return false;

			DCodeCompletionWindow.ShowCompletionWindow(
				this,					// The parent window for the completion window
				txt, 					// The text editor to show the window for
				fileData.mod_file,		// Filename - will be passed back to the provider
				dataProvider,		// Provider to get the list of possible completions
				key							// Key pressed - will be passed to the provider
			);
			return false;
		}

		private void goToDefinitionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Program.Parsing) return;
			int off = txt.ActiveTextAreaControl.Caret.Offset;
			bool ctor, super, isInst, isNameSpace;

			string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(ref off, out ctor, false);
			if (exprs == null || exprs.Length < 1)
			{
				MessageBox.Show("Nothing selected!");
				return;
			}

			int key = DKeywords.GetToken(exprs[0]);
			if (key != -1 && key != DTokens.This && key != DTokens.Super) return;
			DModule gpf = null;
			DataType dt =
				DCodeCompletionProvider.FindActualExpression(project,
					fileData,
					D_IDE_Properties.toCodeLocation(Caret),
					exprs,
					false,
					out super,
					out isInst,
					out isNameSpace,
					out gpf
					);

			if (dt == null) return;

			DocumentInstanceWindow diw = Form1.thisForm.Open(gpf.mod_file);

			if (diw != null)
			{
				//MessageBox.Show(dt.name);
				diw.txt.ActiveTextAreaControl.Caret.Position = new TextLocation(dt.startLoc.Column - 1, dt.startLoc.Line - 1);
				diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
			}
		}

		void TextArea_ToolTipRequest(object sender, ToolTipRequestEventArgs e)
		{
			if (!e.InDocument || Program.Parsing) return;
			TextArea ta = (TextArea)sender;
			if (ta == null || !fileData.IsParsable) return;

			int mouseOffset = 0;
			try
			{
				mouseOffset = ta.TextView.Document.PositionToOffset(e.LogicalPosition);
			}
			catch
			{
				return;
			}
			if (mouseOffset < 1) return;
			bool ctor, super, isInst, isNameSpace;
			DModule gpf = null;
			int off = mouseOffset;
			string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(ref off, out ctor, false);
			if (exprs == null || exprs.Length < 1) return;

			int key = DKeywords.GetToken(exprs[0]);
			if (key != -1 && key != DTokens.This && key != DTokens.Super)
			{
				e.ShowToolTip(DTokens.GetDescription(key));
				return;
			}

			DataType dt =
				DCodeCompletionProvider.FindActualExpression(project,
					fileData,
					D_IDE_Properties.toCodeLocation(e.LogicalPosition),
					exprs,
					false,
					out super,
					out isInst,
					out isNameSpace,
					out gpf
					);

			if (dt == null) return;

			if (!ctor)
				e.ShowToolTip(DCompletionData.BuildDescriptionString(dt));
			else
			{
				string tt = "";
				if (dt.Count < 1) return;
				foreach (DataType ch in dt)
				{
					if (ch.fieldtype == FieldType.Constructor)
						tt += DCompletionData.BuildDescriptionString(ch) + "\n\n";
				}
				if (tt != "") e.ShowToolTip(tt);
			}
		}

		internal static InsightWindow IW;
		public void ShowFunctionParameterToolTip(char key)
		{
			IW = null;
			IW = new InsightWindow(Form1.thisForm, txt);
			IW.AddInsightDataProvider(new InsightWindowProvider(this, key), fileData.mod_file);
			IW.ShowInsightWindow();
		}

		void Document_TextContentChanged(object sender, EventArgs e)
		{
			txt.Document.FormattingStrategy.IndentLine(txt.ActiveTextAreaControl.TextArea, txt.ActiveTextAreaControl.Caret.Line);
		}

		public DocumentInstanceWindow(string filename, DProject prj)
		{
			this.project = prj;
			Init(filename);
			try
			{
				if (File.Exists(filename))
				{
					FileStream tfs = File.OpenRead(filename);
					if (tfs.Length > (1024 * 1024 * 2))
					{
						tfs.Close();
						txt.Document.TextContent = File.ReadAllText(filename);
					}
					else
						txt.LoadFile(filename, tfs, true, true);
				}
			}
			catch { txt.Document.TextContent = File.ReadAllText(filename); }
		}

		public DocumentInstanceWindow(string filename, string content, DProject prj)
		{
			this.project = prj;
			Init(filename);
			txt.Document.TextContent = content;
		}

		public void Save()
		{
			if (fileData.mod_file == "" || fileData.mod_file == null) return;
			File.WriteAllText(fileData.mod_file, txt.Document.TextContent);
			Modified = false;
			Update();
		}

		public void ParseFromText()
		{
			Form1.thisForm.errlog.parserErrors.Clear();
			Form1.thisForm.errlog.Update();

			txt.Document.MarkerStrategy.RemoveAll(new Predicate<TextMarker>(delegate(TextMarker tm)
			{
				return true;
			}));
			Form1.thisForm.ProgressStatusLabel.Text = "Parsing " + fileData.mod;
			fileData.dom = DParser.ParseText(fileData.mod_file, fileData.mod, txt.Text, out fileData.import);
			Form1.thisForm.ProgressStatusLabel.Text = "Done parsing " + fileData.mod;

			if (project != null)
			{
				try { project.files.Remove(project.FileDataByFile(fileData.mod_file)); }
				catch { }
				project.files.Add(fileData);
			}

			ParseFolds(fileData.dom);
		}

		public List<FoldMarker> ParseFolds(DataType env)
		{
			List<FoldMarker> ret = new List<FoldMarker>();

			if (env.Count > 1)
				foreach (DataType ch in env)
				{
					if (DTokens.ClassLike[(int)ch.TypeToken] || ch.fieldtype == FieldType.Function || ch.fieldtype == FieldType.Constructor)
					{
						ret.Add(new FoldMarker(
							txt.Document,
							ch.startLoc.Line - 1, ch.startLoc.Column - 1,
							ch.endLoc.Line - 1, ch.endLoc.Column)
							);
						ret.AddRange(ParseFolds(ch));
					}
				}
			txt.Document.FoldingManager.UpdateFoldings(ret);
			return ret;
		}
	}

	[Serializable()]
	public class D_IDE_Properties
	{
		public static void Load(string fn)
		{
			if (File.Exists(fn))
			{
				BinaryFormatter formatter = new BinaryFormatter();

				Stream stream = File.Open(fn, FileMode.Open);

				try
				{
					Default = (D_IDE_Properties)formatter.Deserialize(stream);
				}
				catch (Exception ex) { MessageBox.Show(ex.Message); }
				stream.Close();
			}
		}

		#region Caching
		public static void LoadGlobalCache(string file)
		{
			if (!File.Exists(file)) return;
			if (cacheTh != null && cacheTh.IsAlive) return;

			Program.Parsing = true;

			List<DModule> tmods = new List<DModule>();


			cacheTh = new Thread(delegate(object o)
			{
				if (cscr == null || cscr.IsDisposed) cscr = new CachingScreen();
				cscr.Show();
				BinaryFormatter formatter = new BinaryFormatter();

				Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
				try
				{
					tmods.AddRange((DModule[])formatter.Deserialize(stream));
				}
				catch (Exception ex) { MessageBox.Show("Error loading " + file + ": " + ex.Message); }
				stream.Close();
				cscr.Close();
				GlobalModules = tmods;
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
			while (Program.Parsing && i < 500) { Thread.Sleep(100); i++; }

			Program.Parsing = true;

			cacheTh = new Thread(delegate(object o)
			{
				if (cscr == null || cscr.IsDisposed) cscr = new CachingScreen();
				cscr.Show();
				BinaryFormatter formatter = new BinaryFormatter();

				Stream stream = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.Read);
				formatter.Serialize(stream, GlobalModules.ToArray()); // ((List<DModule>)arr)
				stream.Close();
				cscr.Close();
				Program.Parsing = false;
			});
			cacheTh.Start();
		}
		#endregion

		public DModule this[string moduleName]
		{
			get
			{
				foreach (DModule dm in GlobalModules)
				{
					if (dm.mod == moduleName) return dm;
				}
				return null;
			}
			set
			{
				int i = 0;
				foreach (DModule dm in GlobalModules)
				{
					if (dm.mod == moduleName)
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
			BinaryFormatter formatter = new BinaryFormatter();

			if (!Directory.Exists(Path.GetDirectoryName(fn)))
				Directory.CreateDirectory(Path.GetDirectoryName(fn));
			Stream stream = File.Open(fn, FileMode.Create);
			formatter.Serialize(stream, Default);
			stream.Close();
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
				if (dpf.mod_file == pf.mod_file)
				{
					dpf.dom = pf.dom;
					dpf.folds = pf.folds;
					dpf.mod = pf.mod;
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
					dpf.mod = pf.mod;
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

		[NonSerialized()]
		public static D_IDE_Properties Default = new D_IDE_Properties();

		public List<string> lastProjects = new List<string>(), lastFiles = new List<string>();
		public bool OpenLastPrj = true;
		public FormWindowState lastFormState = FormWindowState.Maximized;
		public Point lastFormLocation;
		public Size lastFormSize;
		public bool RetrieveNews = true;
		public bool LogBuildProgress = true;
		public bool ShowBuildCommands = false;
		public bool UseExternalDebugger = false;

		public bool SingleInstance = true;
		public bool WatchForUpdates = true;
		public string DefaultProjectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\D Projects";

		[NonSerialized()]
		public static List<DModule> GlobalModules = new List<DModule>();
		public List<string> parsedDirectories = new List<string>();

		public string exe_cmp;
		public string exe_win;
		public string exe_console;
		public string exe_dll;
		public string exe_lib;
		public string exe_res = "rc.exe";
		public string exe_dbg = "windbg.exe";

		public string cmp_obj = "-c -O \"$src\" -of\"$obj\" -gc";
		public string link_win_exe = "$objs $libs -L/su:windows -of\"$exe\" -gc";
		public string link_to_exe = "$objs $libs -of\"$exe\" -gc";
		public string link_to_dll = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc";
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
			return new CodeLocation(Caret.Column, Caret.Line);
		}
	}

	class SyntaxFileProvider : ISyntaxModeFileProvider
	{
		public List<SyntaxMode> modes;
		public SyntaxFileProvider()
		{
			modes = new List<SyntaxMode>();
			modes.Add(new SyntaxMode("D.xshd", "D", ".d"));
			modes.Add(new SyntaxMode("RC.xshd", "RC", ".rc"));
		}

		#region ISyntaxModeFileProvider Member

		public System.Xml.XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
		{
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