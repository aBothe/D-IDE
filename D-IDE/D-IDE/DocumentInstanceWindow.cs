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
    public class DocumentInstanceWindow : DockContent
    {
        public TextEditorControl txt;

        private System.Windows.Forms.ContextMenuStrip tcCont;

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
            txt.TextEditorProperties.ShowSpaces = false;
            txt.TextEditorProperties.ShowVerticalRuler = false;

            try
            {
                txt.SetHighlighting(Path.GetExtension(fn).TrimStart(new char[] { '.' }).ToUpper());
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + " (File not found or wrong file format!)"); }
            txt.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(Caret_PositionChanged);
            txt.Document.DocumentChanged += new DocumentEventHandler(Document_DocumentChanged);
            txt.Document.LineCountChanged += new EventHandler<LineCountChangeEventArgs>(Document_LineCountChanged);

            if (DModule.Parsable(fn))
            {
                txt.Document.FormattingStrategy = new DFormattingStrategy();
                txt.ActiveTextAreaControl.TextArea.ToolTipRequest += TextArea_ToolTipRequest;
                txt.ActiveTextAreaControl.TextArea.KeyEventHandler += TextAreaKeyEventHandler;
            }

            txt.TextEditorProperties.AutoInsertCurlyBracket = true;
            txt.TextEditorProperties.IndentStyle = IndentStyle.Smart;

            // Additional GUI settings

            // Context menu
            this.tcCont = new System.Windows.Forms.ContextMenuStrip();

            ToolStripMenuItem
                tmi1, tmi2, tmi3,
                GotoDef, CreateImportDirective,
                CommentBlock, UnCommentBlock,
                OutlineMenu,
                ShowDefsOnly, ExpandAll, CollapseAll;

            tmi1 = new ToolStripMenuItem("Copy", global::D_IDE.Properties.Resources.copy, new EventHandler(delegate(object sender, EventArgs ea)
                {
                    EmulateCopy();
                }));
            tmi2 = new ToolStripMenuItem("Cut", global::D_IDE.Properties.Resources.cut, new EventHandler(delegate(object sender, EventArgs ea)
            {
                EmulateCut();
            }));
            tmi3 = new ToolStripMenuItem("Paste", global::D_IDE.Properties.Resources.paste, new EventHandler(delegate(object sender, EventArgs ea)
            {
                EmulatePaste();
            }));

            GotoDef = new ToolStripMenuItem("Go to definition", null, GoToDefinition_Click);
            CreateImportDirective = new ToolStripMenuItem("Create import directive", null, CreateImportDirective_Click);
            CommentBlock = new ToolStripMenuItem("Comment out block", null, CommentOutBlock);
            UnCommentBlock = new ToolStripMenuItem("Uncomment block", null, UncommentBlock);

            // Outline Folding Submenu
            ExpandAll = new ToolStripMenuItem("Expand all", null, this.ExpandAllFolds);
            CollapseAll = new ToolStripMenuItem("Collapse all", null, this.CollapseAllFolds);
            ShowDefsOnly = new ToolStripMenuItem("Show Definitions only", null, this.ShowDefsOnly);
            OutlineMenu = new ToolStripMenuItem("Outlining", null, ExpandAll, CollapseAll, ShowDefsOnly);

            this.tcCont.SuspendLayout();
            // 
            // tcCont
            // 
            this.tcCont.Items.AddRange(new System.Windows.Forms.ToolStripItem[] 
            { 
                tmi1, tmi2, tmi3, 
                new ToolStripSeparator(),
                GotoDef,
                CreateImportDirective,
                new ToolStripSeparator(),
                CommentBlock,UnCommentBlock,
                new ToolStripSeparator(),
                OutlineMenu
            });
            this.tcCont.Name = "tcCont";
            this.tcCont.ResumeLayout(false);
            txt.ContextMenuStrip = tcCont;
        }

        void Document_LineCountChanged(object sender, LineCountChangeEventArgs e)
        {
            foreach (DIDEBreakpoint dbp in Breakpoints)
            {
                if (dbp.line >= e.LineStart)
                {
                    dbp.line += e.LinesMoved;
                }
            }

            D_IDEForm.thisForm.dbgwin.Update();
            //DrawBreakPoints();
        }

        #region Debug Breakpoints
        public List<DIDEBreakpoint> Breakpoints
        {
            get
            {
                if (D_IDEForm.thisForm.Breakpoints.Breakpoints.ContainsKey(fileData.FileName))
                    return D_IDEForm.thisForm.Breakpoints.Breakpoints[fileData.FileName];
                else return new List<DIDEBreakpoint>();
            }
        }

        public void DrawBreakPoints()
        {
            txt.Document.MarkerStrategy.RemoveAll(RemoveMarkerMatch);
            txt.BeginUpdate();
            foreach (DIDEBreakpoint bp in Breakpoints)
            {
                    LineSegment ls = txt.Document.GetLineSegment(bp.line - 1);
                    TextMarker tm = new TextMarker(ls.Offset, ls.Length, TextMarkerType.SolidBlock, Color.Orange);

                    txt.Document.MarkerStrategy.AddMarker(tm);
            }

            txt.EndUpdate();
            txt.Refresh();
        }

        static bool RemoveMarkerMatch(TextMarker tm)
        {
            if (tm.Color == Color.Orange) return true;
            return false;
        }
        #endregion

        void Document_DocumentChanged(object sender, DocumentEventArgs e)
        {
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

        DNode selectedBlock = null;
        public List<ICompletionData> CurrentCompletionData = new List<ICompletionData>();

        /// <summary>
        /// Updates the local completion data cache
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Caret_PositionChanged(object sender, EventArgs e)
        {
            CompilerConfiguration cc = project != null ? project.Compiler : D_IDE_Properties.Default.dmd2;
            D_IDEForm.thisForm.LineLabel.Text =
                "Line " + (txt.ActiveTextAreaControl.Caret.Line + 1).ToString() +
                " Col " + (txt.ActiveTextAreaControl.Caret.Column).ToString();
            DNode tv = DCodeCompletionProvider.GetBlockAt(fileData.dom, Caret);

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
                        CurrentCompletionData.Add(new DCompletionData(mod, "Project Module", D_IDEForm.icons.Images.IndexOfKey("namespace")));
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

        void CreateImportDirective_Click(object sender, EventArgs e)
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
            DNode dt =
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
                DCodeCompletionProvider.Commenting.IsInCommentAreaOrString(txt.Document.TextContent, txt.ActiveTextAreaControl.Caret.Offset))
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

        private void GoToDefinition_Click(object sender, EventArgs e)
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
            DNode dt =
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
			if (mouseOffset < 1 || DCodeCompletionProvider.Commenting.IsInCommentAreaOrString(txt.Document.TextContent, mouseOffset)) return;
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
            if (D_IDEForm.thisForm.IsDebugging)
            {
                DebugScopedSymbol[] syms = D_IDEForm.thisForm.dbg.Symbols.ScopeLocalSymbols;
                if (syms != null)
                {
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
                        e.ShowToolTip(cursym.TypeName + " " + desc.Trim('.') + " = " + D_IDEForm.thisForm.BuildSymbolValueString((uint)ta.Caret.Line - 1, cursym, exprs));
                        return;
                    }
                }
            }
            #endregion

            DNode dt =
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
                foreach (DNode ch in dt)
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
            IW = new InsightWindow(D_IDEForm.thisForm, txt);
            IW.AddInsightDataProvider(new InsightWindowProvider(this, key), fileData.mod_file);
            IW.ShowInsightWindow();
        }

        #region Commenting
        public void CommentOutBlock(object o, EventArgs ea)
        {
            string sel = txt.ActiveTextAreaControl.SelectionManager.SelectedText;
            if (String.IsNullOrEmpty(sel))
            {
                int line = Caret.Line;
                txt.Document.Insert(
                    txt.Document.PositionToOffset(new TextLocation(0, line)),
                    "//"
                    );
            }
            else
            {
                ISelection isel = txt.ActiveTextAreaControl.SelectionManager.SelectionCollection[0];
                txt.Document.UndoStack.StartUndoGroup();

                bool a, b, IsInBlock, IsInNested, HasBeenCommented=false;
                DCodeCompletionProvider.Commenting.IsInCommentAreaOrString(txt.Text, isel.Offset, out a, out b, out IsInBlock, out IsInNested);

                if (!IsInBlock && !IsInNested)
                {
                    txt.Document.Insert(isel.EndOffset, "*/");
                    txt.Document.Insert(isel.Offset, "/*");
                    HasBeenCommented = true;
                }
                else
                {
                    txt.Document.Insert(isel.EndOffset, "+/");
                    txt.Document.Insert(isel.Offset, "/+");
                    HasBeenCommented = true;
                }

                if(HasBeenCommented)txt.ActiveTextAreaControl.SelectionManager.SetSelection(
                    new TextLocation(isel.StartPosition.X + 2, isel.StartPosition.Y),
                    new TextLocation(isel.EndPosition.X + 2, isel.EndPosition.Y)
                    );
                txt.Document.UndoStack.EndUndoGroup();
            }
            Refresh();
        }

        public void UncommentBlock(object o, EventArgs ea)
        {
            #region Remove line comments first
            LineSegment ls = txt.Document.LineSegmentCollection[Caret.Line];
            int commStart = CaretOffset;
            for (; commStart > ls.Offset; commStart--)
            {
                if (txt.Document.TextBufferStrategy.GetCharAt(commStart) == '/' && commStart > 0 &&
                    txt.Document.TextBufferStrategy.GetCharAt(commStart - 1) == '/')
                {
                    txt.Document.Remove(commStart - 1, 2);
                    Refresh();
                    return;
                }
            }
            #endregion
            #region If no single-line comment was removed, delete multi-line comment block tags
            int off = CaretOffset;
            bool IsNested = false;

            // Seek the comment block opener
            commStart = DCodeCompletionProvider.Commenting.LastIndexOf(txt.Text, "/*", off);
            int nestedCommStart = DCodeCompletionProvider.Commenting.LastIndexOf(txt.Text, "/+", off);
            if (nestedCommStart>commStart)
            {
                commStart = nestedCommStart;
                IsNested = true;
            }

            if (commStart < 0) return;

            int off2 = off + (commStart == off ? 2 : 0);

            int commEnd = DCodeCompletionProvider.Commenting.IndexOf (txt.Text,IsNested?"+/":"*/", off2);
            if (commEnd < 0) return;

            txt.Document.UndoStack.StartUndoGroup();
            txt.Document.Remove(commEnd, 2);
            txt.Document.Remove(commStart, 2);

            if(commStart!=off)txt.ActiveTextAreaControl.Caret.Position = txt.ActiveTextAreaControl.Document.OffsetToPosition(off-2);

            if (txt.ActiveTextAreaControl.SelectionManager.SelectionCollection.Count > 0)
            {
                ISelection isel = txt.ActiveTextAreaControl.SelectionManager.SelectionCollection[0];
                txt.ActiveTextAreaControl.SelectionManager.SetSelection(
                        new TextLocation(isel.StartPosition.X - 2, isel.StartPosition.Y),
                        new TextLocation(isel.EndPosition.X - 2, isel.EndPosition.Y)
                        );
            }

            txt.Document.UndoStack.EndUndoGroup();
            #endregion
            Refresh();

        }
        #endregion

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
            D_IDEForm.thisForm.errlog.parserErrors.Clear();
            D_IDEForm.thisForm.errlog.Update();

            txt.Document.MarkerStrategy.RemoveAll(RemoveParserMarkersPred);

            D_IDEForm.thisForm.ProgressStatusLabel.Text = "Parsing " + fileData.ModuleName;
            fileData.dom = DParser.ParseText(fileData.mod_file, fileData.ModuleName, txt.Text, out fileData.import);
            D_IDEForm.thisForm.ProgressStatusLabel.Text = "Done parsing " + fileData.ModuleName;

            if (project != null)
            {
                try { project.files.Remove(project.FileDataByFile(fileData.mod_file)); }
                catch { }
                project.files.Add(fileData);
            }

            ParseFolds();
        }

        private static bool RemoveParserMarkersPred(TextMarker tm)
        {
            if (tm.TextMarkerType == TextMarkerType.WaveLine) return true;
            return false;
        }

        #region Folding
        public List<FoldMarker> ParseFolds()
        {
            char cur = '\0', peekChar='\0';
            int off = 0;
            List<FoldMarker> ret = new List<FoldMarker>();

            Stack<int> FMStack = new Stack<int>();
            bool IsInString = false, IsInLineComment = false, IsInBlockComment=false;
            while (off < txt.Document.TextLength)
            {
                cur = txt.Document.TextBufferStrategy.GetCharAt(off);
                #region Non-parsed file sections
                if (off < txt.Document.TextLength - 1) peekChar = txt.Document.TextBufferStrategy.GetCharAt(off + 1);

                if (!IsInBlockComment && !IsInLineComment &&cur == '\"' && (off < 1 || txt.Document.TextBufferStrategy.GetCharAt(off - 1) != '\\'))
                    IsInString = !IsInString;

                if (!IsInBlockComment && !IsInString && cur == '/' && peekChar == '/')
                    IsInLineComment = true;
                if (!IsInBlockComment && !IsInString && IsInLineComment && cur == '\n')
                    IsInLineComment = false;

                if (!IsInLineComment && !IsInString && cur == '/' && (peekChar == '*' || peekChar=='+'))
                    IsInBlockComment = true;
                if (!IsInLineComment && !IsInString && IsInBlockComment && (cur == '*' || cur=='+') && peekChar == '/')
                    IsInBlockComment = false;
                #endregion
                #region Main part
                if (!IsInString && !IsInLineComment && !IsInBlockComment)
                {
                    if (cur == '{')
                    {
                        FMStack.Push(off);
                    }

                    if (cur == '}' && FMStack.Count>0)
                    {
                        int start = FMStack.Pop();
                        int end = off + 1;

                        TextLocation startLoc = txt.Document.OffsetToPosition(start);
                        TextLocation endLoc = txt.Document.OffsetToPosition(end);

                        if (endLoc.Line != startLoc.Line)
                        {
                            FoldMarker fm = new FoldMarker(
                                        txt.Document,
                                        startLoc.Line, startLoc.Column,
                                        endLoc.Line, endLoc.Column);
                            fm.IsFolded = !txt.Document.FoldingManager.IsLineVisible(startLoc.Line);
                            ret.Add(fm);
                        }
                    }
                }
                #endregion
                off++;
            }

            txt.Document.FoldingManager.UpdateFoldings(ret);
            return ret;
        }

        public void ExpandAllFolds(object o, EventArgs ea)
        {
            foreach (FoldMarker fm in txt.Document.FoldingManager.FoldMarker)
                fm.IsFolded = false;
            Refresh();
        }

        public void CollapseAllFolds(object o, EventArgs ea)
        {
            foreach (FoldMarker fm in txt.Document.FoldingManager.FoldMarker)
                fm.IsFolded = true;
            Refresh();
        }

        public void ShowDefsOnly(object o, EventArgs ea)
        {
            foreach (FoldMarker fm in txt.Document.FoldingManager.FoldMarker)
            {
                if (CaretOffset > fm.Offset && (fm.Offset + fm.Length) > CaretOffset)
                {
                    fm.IsFolded = false;
                    continue;
                }
                fm.IsFolded = true;
            }
            Refresh();
        }
        #endregion
    }
}