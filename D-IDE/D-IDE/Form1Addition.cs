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
using D_IDE.Properties;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Gui.InsightWindow;
using WeifenLuo.WinFormsUI.Docking;
using System.Globalization;
using System.Text;
using ICSharpCode.NRefactory.Ast;
using DebugEngineWrapper;

namespace D_IDE
{
    class DocumentInstanceWindow : DockContent
    {
        public TextEditorControl txt;

        private System.Windows.Forms.ContextMenuStrip tcCont;
        private System.Windows.Forms.ToolStripMenuItem goToDefinitionToolStripMenuItem, createImportDirectiveItem;

        public DModule fileData;
        public string ProjectFile;
        public DProject project
        {
            get { return D_IDE_Properties.GetProject(ProjectFile); }
        }
        bool modified = false;
        public bool Modified
        {
            set
            {
                if (value != modified) this.Text = Path.GetFileName(fileData.mod_file) + (value ? " *" : "");
                modified = value;
            }
            get { return modified; }
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

            fileData = new DModule(project, fn);

            txt = new TextEditorControl();

            txt.Dock = DockStyle.Fill;
            this.Controls.Add(txt);

            txt.TextEditorProperties.AllowCaretBeyondEOL = false;
            txt.TextEditorProperties.AutoInsertCurlyBracket = true;
            txt.TextEditorProperties.BracketMatchingStyle = BracketMatchingStyle.After;
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

            try
            {
                txt.SetHighlighting(Path.GetExtension(fn).TrimStart(new char[] { '.' }).ToUpper());
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + " (File not found or wrong file format!)"); }
            txt.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(Caret_PositionChanged);
            txt.Document.DocumentChanged += new DocumentEventHandler(Document_DocumentChanged);

            if (DModule.Parsable(fn))
            {
                txt.Document.FormattingStrategy = new DFormattingStrategy();
                txt.ActiveTextAreaControl.TextArea.ToolTipRequest += TextArea_ToolTipRequest;
                txt.ActiveTextAreaControl.TextArea.KeyEventHandler += TextAreaKeyEventHandler;
            }

            txt.TextEditorProperties.AutoInsertCurlyBracket = true;
            txt.TextEditorProperties.IndentStyle = IndentStyle.Smart;

            this.tcCont = new System.Windows.Forms.ContextMenuStrip();
            this.goToDefinitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            createImportDirectiveItem = new ToolStripMenuItem();
            this.Activated += new EventHandler(DocumentInstanceWindow_Activated);

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

        void DocumentInstanceWindow_Activated(object sender, EventArgs e)
        {
            this.txt.ActiveTextAreaControl.Focus();
        }

        void Document_DocumentChanged(object sender, DocumentEventArgs e)
        {
            //txt.Document.FormattingStrategy.IndentLine(txt.ActiveTextAreaControl.TextArea, txt.ActiveTextAreaControl.Caret.Line);
            Modified = true;
        }

        public int CaretOffset
        {
            get { return txt.ActiveTextAreaControl.Caret.Offset; }
        }
        public TextLocation Caret
        {
            get { return txt.ActiveTextAreaControl.Caret.Position; }
        }

        DataType selectedBlock = null;
        public List<ICompletionData> CurrentCompletionData = new List<ICompletionData>();

        /// <summary>
        /// Updates the local completion data cache
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Caret_PositionChanged(object sender, EventArgs e)
        {
            CompilerConfiguration cc = project != null ? project.Compiler : D_IDE_Properties.Default.dmd2;
            Form1.thisForm.LineLabel.Text =
                "Line " + (txt.ActiveTextAreaControl.Caret.Line + 1).ToString() +
                " Col " + (txt.ActiveTextAreaControl.Caret.Column).ToString();
            DataType tv = DCodeCompletionProvider.GetBlockAt(fileData.dom, Caret);

            if (selectedBlock != tv || selectedBlock == null)
            {
                selectedBlock = tv;
                CurrentCompletionData.Clear();
                if (tv != null)
                {
                    DCodeCompletionProvider.AddAllClassMembers(cc, tv, ref CurrentCompletionData, true);
                }

                if (project != null)
                {
                    List<string> mods = new List<string>();
                    string tmod;
                    foreach (DModule ppf in project.files)
                    {
                        if (!ppf.IsParsable) continue;
                        if (!String.IsNullOrEmpty(ppf.ModuleName))
                        {
                            tmod = ppf.ModuleName.Split('.')[0];
                            if (!mods.Contains(tmod)) mods.Add(tmod);
                        }
                        // Add the content of the module
                        DCodeCompletionProvider.AddAllClassMembers(cc, ppf.dom, ref CurrentCompletionData, false);
                    }
                    // Add all local modules
                    foreach (string mod in mods)
                    {
                        CurrentCompletionData.Add(new DCompletionData(mod, "Project Module", Form1.icons.Images.IndexOfKey("namespace")));
                    }
                }
                else // Add classes etc from current module
                    DCodeCompletionProvider.AddAllClassMembers(cc, fileData.dom, ref CurrentCompletionData, true);
                try
                {
                    CurrentCompletionData.Capacity += cc.GlobalCompletionList.Count;
                    CurrentCompletionData.AddRange(cc.GlobalCompletionList);
                }
                catch { }
            }
        }

        void createImportDirectiveItem_Click(object sender, EventArgs e)
        {
            int off = txt.ActiveTextAreaControl.Caret.Offset;
            bool ctor, super, isInst, isNameSpace;

            string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(txt.Document.TextContent, ref off, out ctor, false);
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
                    true,
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
        public bool TextAreaKeyEventHandler(char key)
        {
            if (Program.Parsing ||
                DCodeCompletionProvider.isInCommentAreaOrString(txt.Document.TextContent, txt.ActiveTextAreaControl.Caret.Offset))
                return false;

            //if (key == '(')txt.Document.Insert(CaretOffset, ")");
            if (key == '(' || key == ',')
            {
                ShowFunctionParameterToolTip(key);
                return false;
            }

            ICompletionDataProvider dataProvider = null;

            if (Char.IsLetterOrDigit(key) || key == '_' || key == '.' || key == ' ' || key == '\0')
                dataProvider = new DCodeCompletionProvider();
            else return false;

            ICompletionData[] data = dataProvider.GenerateCompletionData(fileData.FileName, txt.ActiveTextAreaControl.TextArea, key);
            if (data.Length < 1) return false;
            /*
            D_IDE.CodeCompletion.CodeCompletionWindow ccw = new D_IDE.CodeCompletion.CodeCompletionWindow(data,Form1.icons);
            ccw.Show();*/

            DCodeCompletionWindow.ShowCompletionWindow(
                this,					// The parent window for the completion window
                txt, 					// The text editor to show the window for
                fileData.FileName,		// Filename - will be passed back to the provider
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

            string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(txt.Document.TextContent, ref off, out ctor, false);
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
                    true,
                    out super,
                    out isInst,
                    out isNameSpace,
                    out gpf
                    );

            if (dt == null || gpf == null) return;

            ErrorLog.OpenError(gpf.mod_file, dt.StartLocation.Line, dt.StartLocation.Column);
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
            string[] exprs = DCodeCompletionProvider.GetExpressionStringsAtOffset(ta.Document.TextContent, ref off, out ctor, false);
            if (exprs == null || exprs.Length < 1) return;

            if (exprs[0] == "__FILE__")
            {
                e.ShowToolTip(fileData.FileName);
                return;
            }
            if (exprs[0] == "__LINE__")
            {
                e.ShowToolTip((e.LogicalPosition.Line + 1).ToString());
                return;
            }

            int key = DKeywords.GetToken(exprs[0]);
            if (key != -1 && key != DTokens.This && key != DTokens.Super)
            {
                e.ShowToolTip(DTokens.GetDescription(key));
                return;
            }

            #region If debugging, check if a local fits to one of the scoped symbols and show its value if possible
            if (Form1.thisForm.IsDebugging)
            {
                DebugScopedSymbol[] syms = Form1.thisForm.dbg.Symbols.ScopeLocalSymbols;

                DebugScopedSymbol cursym = null;
                string desc = "";
                foreach (string exp in exprs)
                {
                    foreach (DebugScopedSymbol sym in syms)
                    {
                        if (cursym != null && sym.ParentId != cursym.Id) continue;

                        if (sym.Name == exp)
                        {
                            desc += "." + sym.Name;
                            cursym = sym;
                        }
                    }
                }
                if (desc != "" && cursym != null)
                {
                    e.ShowToolTip(cursym.TypeName + " " + desc.Trim('.') + " = " + Form1.thisForm.BuildSymbolValueString((uint)ta.Caret.Line - 1, cursym, exprs));
                    return;
                }
            }
            #endregion

            DataType dt =
                DCodeCompletionProvider.FindActualExpression(project,
                    fileData,
                    D_IDE_Properties.toCodeLocation(e.LogicalPosition),
                    exprs,
                    false,
                    false,
                    out super,
                    out isInst,
                    out isNameSpace,
                    out gpf
                    );

            if (dt == null) return;

            if (!ctor)
                e.ShowToolTip(DCompletionData.BuildDescriptionString(dt, gpf));
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

        public DocumentInstanceWindow(string filename, string prj)
        {
            this.ProjectFile = prj;
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
            catch (Exception ex) { txt.Document.TextContent = File.ReadAllText(filename); throw ex; }
            Modified = false;
        }

        public DocumentInstanceWindow(string filename, string content, string prj)
        {
            this.ProjectFile = prj;
            Init(filename);
            txt.Document.TextContent = content;
            Modified = false;
        }

        public void Reload()
        {
            txt.LoadFile(fileData.FileName);
            ParseFromText();
        }

        public void Save()
        {
            if (fileData.mod_file == "" || fileData.mod_file == null || !Modified) return;
            File.WriteAllText(fileData.mod_file, txt.Document.TextContent);

            Modified = false;
        }

        public void ParseFromText()
        {
            Form1.thisForm.errlog.parserErrors.Clear();
            Form1.thisForm.errlog.Update();

            txt.Document.MarkerStrategy.RemoveAll(new Predicate<TextMarker>(delegate(TextMarker tm)
            {
                return true;
            }));
            Form1.thisForm.ProgressStatusLabel.Text = "Parsing " + fileData.ModuleName;
            fileData.dom = DParser.ParseText(fileData.mod_file, fileData.ModuleName, txt.Text, out fileData.import);
            Form1.thisForm.ProgressStatusLabel.Text = "Done parsing " + fileData.ModuleName;

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
}