using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using D_IDE.Core;
using D_IDE.Core.Controls;
using D_IDE.Core.Controls.Editor;
using D_IDE.D.DEditor;
using D_Parser.Dom;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using D_Parser.Resolver;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using D_Parser.Completion;

namespace D_IDE.D
{
	public class DEditorDocument : EditorDocument, IEditorData
	{
		#region Properties
		ComboBox lookup_Types;
		ComboBox lookup_Members;
		ToolTip editorToolTip = new ToolTip();
		DIndentationStrategy indentationStrategy;
		FoldingManager foldingManager;

		/// <summary>
		/// Parse duration (in seconds) of the last code analysis
		/// </summary>
		public double ParseTime
		{
			get;
			protected set;
		}

		DModule _unboundTree;
		public DModule SyntaxTree
		{
			get
			{
				if (HasProject)
				{
					var prj = Project as DProject;
					if (prj != null)
						return prj.ParsedModules[AbsoluteFilePath] as DModule;
				}

				return _unboundTree;
			}
			set
			{
				if (value != null)
					value.FileName = AbsoluteFilePath;
				if (HasProject)
				{
					var prj = Project as DProject;
					if (prj != null)
						prj.ParsedModules[AbsoluteFilePath] = value;
				}

				_unboundTree = value;

				UpdateImportCache();
			}
		}
		public IEnumerable<IAbstractSyntaxTree> ParseCache
		{
			get;
			set;
		}
		public IEnumerable<IAbstractSyntaxTree> ImportCache
		{
			get;
			protected set;
		}

		public void UpdateImportCache()
		{
			Dispatcher.Invoke(new Action(() => ParseCache = DCodeCompletionSupport.EnumAvailableModules(this)));
			ImportCache = DResolver.ResolveImports(SyntaxTree, ParseCache);
		}

		/// <summary>
		/// Variable that indicates if document is parsed currently.
		/// </summary>
		public bool IsParsing { get; protected set; }

		/// <summary>
		/// Variable that is used for the parser loop to recognize user interaction.
		/// So, if the user typed a character, this will be set to true, whereas it later, after the text has become parsed, will be reset
		/// </summary>
		bool KeysTyped = false;
		Thread parseThread = null;
		readonly HighPrecisionTimer.HighPrecTimer hp = new HighPrecisionTimer.HighPrecTimer();
		//bool CanRefreshSemanticHighlightings = false;

		bool isUpdatingLookupDropdowns = false;

		List<string> foldedNodeNames = new List<string>();

		public string ProposedModuleName
		{
			get
			{
				if (HasProject)
					return Path.ChangeExtension(RelativeFilePath, null).Replace('\\', '.');
				else
					return Path.GetFileNameWithoutExtension(FileName);
			}
		}

		public IBlockNode lastSelectedBlock { get; protected set; }

		DispatcherOperation blockCompletionDataOperation = null;
		//DispatcherOperation showCompletionWindowOperation = null;
		DispatcherOperation parseOperation = null;

		public DMDConfig CompilerConfiguration
		{
			get
			{
				if (HasProject && Project is DProject)
					return (Project as DProject).CompilerConfiguration;
				return DSettings.Instance.DMDConfig();
			}
		}

		internal CompletionWindow completionWindow;
		OverloadInsightWindow insightWindow;
		#endregion

		public DEditorDocument()
		{
			Init();
		}

		public DEditorDocument(string file)
			: base(file)
		{
			Init();
		}

		void Init()
		{
			#region Setup type lookup dropdowns
			// Create a grid which is located at the very top of the editor document
			var stk = new Grid()
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height = 24,
				VerticalAlignment = VerticalAlignment.Top
			};

			// Give it two columns that have an equal width
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });

			// Move the editor away from the upper boundary
			Editor.Margin = new Thickness() { Top = stk.Height };

			MainEditorContainer.Children.Add(stk);

			lookup_Types = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch };
			lookup_Members = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch };

			lookup_Types.SelectionChanged += lookup_Types_SelectionChanged;
			lookup_Members.SelectionChanged += lookup_Types_SelectionChanged;

			stk.Children.Add(lookup_Types);
			stk.Children.Add(lookup_Members);

			#region Setup dropdown item template
			var lookupItemTemplate = lookup_Members.ItemTemplate = lookup_Types.ItemTemplate = new DataTemplate { DataType = typeof(DCompletionData) };

			var sp = new FrameworkElementFactory(typeof(StackPanel));
			sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			sp.SetBinding(StackPanel.ToolTipProperty, new Binding("Description"));

			var iTemplate_Img = new FrameworkElementFactory(typeof(Image));
			iTemplate_Img.SetBinding(Image.SourceProperty, new Binding("Image"));
			iTemplate_Img.SetValue(Image.MarginProperty, new Thickness(1, 1, 4, 1));
			sp.AppendChild(iTemplate_Img);

			var iTemplate_Name = new FrameworkElementFactory(typeof(TextBlock));
			iTemplate_Name.SetBinding(TextBlock.TextProperty, new Binding("PureNodeString"));
			sp.AppendChild(iTemplate_Name);

			lookupItemTemplate.VisualTree = sp;
			#endregion

			// Important: Move the members-lookup to column 1
			lookup_Members.SetValue(Grid.ColumnProperty, 1);
			#endregion

			// Register CodeCompletion events
			Editor.TextArea.TextEntering += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntering);
			Editor.TextArea.TextEntered += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntered);
			Editor.Document.Changed += new EventHandler<ICSharpCode.AvalonEdit.Document.DocumentChangeEventArgs>(Document_Changed);
			Editor.TextArea.Caret.PositionChanged += new EventHandler(TextArea_SelectionChanged);
			Editor.MouseHover += new System.Windows.Input.MouseEventHandler(Editor_MouseHover);
			Editor.MouseHoverStopped += new System.Windows.Input.MouseEventHandler(Editor_MouseHoverStopped);

			Editor.TextArea.IndentationStrategy = indentationStrategy = new DIndentationStrategy(this);
			foldingManager = ICSharpCode.AvalonEdit.Folding.FoldingManager.Install(Editor.TextArea);

			#region Init context menu
			var cm = new ContextMenu();
			Editor.ContextMenu = cm;

			var cmi = new MenuItem() { Header = "Add import directive", ToolTip = "Add an import directive to the document if type cannot be resolved currently" };
			cmi.Click += ContextMenu_AddImportStatement_Click;
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Go to definition", ToolTip = "Go to the definition that defined the currently hovered item" };
			cmi.Click += new System.Windows.RoutedEventHandler(ContextMenu_GotoDefinition_Click);
			cm.Items.Add(cmi);

			cmi = new MenuItem()
			{
				Header = "Toggle Breakpoint",
				ToolTip = "Toggle breakpoint on the currently selected line",
				Command = D_IDE.Core.Controls.IDEUICommands.ToggleBreakpoint
			};
			cm.Items.Add(cmi);

			cm.Items.Add(new Separator());

			cmi = new MenuItem()
			{
				Header = "Comment selection",
				ToolTip = "Comment out current selection. If nothing is selected, the current line will be commented only",
				Command = D_IDE.Core.Controls.IDEUICommands.CommentBlock
			};
			cm.Items.Add(cmi);

			cmi = new MenuItem()
			{
				Header = "Uncomment selection",
				ToolTip = "Uncomment current block. The nearest comment tags will be removed.",
				Command = D_IDE.Core.Controls.IDEUICommands.UncommentBlock
			};
			cm.Items.Add(cmi);

			cm.Items.Add(new Separator());

			cmi = new MenuItem() { Header = "Cut", Command = System.Windows.Input.ApplicationCommands.Cut };
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Copy", Command = System.Windows.Input.ApplicationCommands.Copy };
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Paste", Command = System.Windows.Input.ApplicationCommands.Paste };
			cm.Items.Add(cmi);
			#endregion

			//CommandBindings.Add(new CommandBinding(IDEUICommands.ReformatDoc,ReformatFileCmd));
			CommandBindings.Add(new CommandBinding(IDEUICommands.CommentBlock, CommentBlock));
			CommandBindings.Add(new CommandBinding(IDEUICommands.UncommentBlock, UncommentBlock));

			// Init parser loop
			parseThread = new Thread(ParserLoop);
			parseThread.IsBackground = true;
			parseThread.Start();
		}

		/*
		public override void Reload()
		{
			base.Reload();
			CanRefreshSemanticHighlightings = true;
		}*/

		public void UpdateFoldings()
		{
			if (foldingManager == null)
				return;

			foreach (var fs in foldingManager.AllFoldings)
				if (fs.IsFolded)
					foldedNodeNames.Add(fs.Tag as string);

			foldingManager.Clear();

			updateFoldingsInternal(SyntaxTree);
		}

		void updateFoldingsInternal(IBlockNode block)
		{
			if (block == null)
				return;

			if (!(block is IAbstractSyntaxTree) && !block.BlockStartLocation.IsEmpty && block.EndLocation > block.BlockStartLocation)
			{
				var fn = foldingManager.CreateFolding(
					Editor.Document.GetOffset(block.BlockStartLocation.Line, block.BlockStartLocation.Column),
					Editor.Document.GetOffset(block.EndLocation.Line, block.EndLocation.Column));
				//fn.Title = (block as AbstractNode).ToString(false,false);
				var nn = fn.Tag = block.ToString();

				if (foldedNodeNames.Contains(nn))
					fn.IsFolded = true;
			}

			if (block.Count > 0)
				foreach (var n in block)
					updateFoldingsInternal(n as IBlockNode);
		}

		void lookup_Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (isUpdatingLookupDropdowns || e.AddedItems.Count < 1)
				return;

			var completionData = e.AddedItems[0] as DCompletionData;

			if (completionData == null)
				return;

			Editor.TextArea.Caret.Position = new TextViewPosition(completionData.Node.StartLocation.Line, completionData.Node.StartLocation.Column);
			Editor.TextArea.Caret.BringCaretToView();
			Editor.Focus();
		}

		#region Code operations
		void CommentBlock(object s, ExecutedRoutedEventArgs e)
		{
			if (false)
			{
				/*
				int cOff = Editor.CaretOffset;
				Editor.Text = Commenting.comment(Editor.Text, Editor.SelectionStart, Editor.SelectionStart + Editor.SelectionLength);
				Editor.CaretOffset = cOff;
				var loc = Editor.Document.GetLocation(cOff);
				Editor.ScrollTo(loc.Line, loc.Column);*/
			}
			else
			{
				if (Editor.SelectionLength < 1)
				{
					Editor.Document.Insert(Editor.Document.GetOffset(Editor.TextArea.Caret.Line, 0), "//");
				}
				else
				{
					Editor.Document.UndoStack.StartUndoGroup();

					bool a, b, IsInBlock, IsInNested;
					DResolver.CommentSearching.IsInCommentAreaOrString(Editor.Text, Editor.SelectionStart, out a, out b, out IsInBlock, out IsInNested);

					if (!IsInBlock && !IsInNested)
					{
						Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, "*/");
						Editor.Document.Insert(Editor.SelectionStart, "/*");
					}
					else
					{
						Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, "+/");
						Editor.Document.Insert(Editor.SelectionStart, "/+");
					}

					Editor.SelectionLength -= 2;

					Editor.Document.UndoStack.EndUndoGroup();
				}
			}
		}

		void UncommentBlock(object s, ExecutedRoutedEventArgs e)
		{
			var CaretOffset = Editor.CaretOffset;
			#region Remove line comments first
			var ls = Editor.Document.GetLineByNumber(Editor.TextArea.Caret.Line);
			int commStart = CaretOffset;
			for (; commStart > ls.Offset; commStart--)
			{
				if (Editor.Document.GetCharAt(commStart) == '/' && commStart > 0 &&
					Editor.Document.GetCharAt(commStart - 1) == '/')
				{
					// Check if DDoc comment
					bool isDDoc=commStart>1 && Editor.Document.GetCharAt(commStart-2)=='/';
					Editor.Document.Remove(commStart - (isDDoc?2:1), isDDoc?3:2);
					return;
				}
			}
			#endregion
			#region If no single-line comment was removed, delete multi-line comment block tags
			if (CaretOffset < 2) return;
			int off = CaretOffset - 2;

			// Seek the comment block opener
			commStart = DResolver.CommentSearching.LastIndexOf(Editor.Text, false, off);
			int nestedCommStart = DResolver.CommentSearching.LastIndexOf(Editor.Text, true, off);
			if (commStart < 0 && nestedCommStart < 0) return;

			// Seek the fitting comment block closer
			int off2 = off + (Math.Max(nestedCommStart, commStart) == off ? 2 : 0);
			int commEnd = DResolver.CommentSearching.IndexOf(Editor.Text, false, off2);
			int commEnd2 = DResolver.CommentSearching.IndexOf(Editor.Text, true, off2);

			if (nestedCommStart > commStart && commEnd2 > nestedCommStart)
			{
				commStart = nestedCommStart;
				commEnd = commEnd2;
			}

			if (commStart < 0 || commEnd < 0) return;

			Editor.Document.UndoStack.StartUndoGroup();
			Editor.Document.Remove(commEnd, 2);
			Editor.Document.Remove(commStart, 2);

			if (commStart != off) Editor.CaretOffset = off;

			Editor.Document.UndoStack.EndUndoGroup();
			#endregion
		}

		void ContextMenu_GotoDefinition_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				if (SyntaxTree == null)
					return;

				var rr = DResolver.ResolveType(Editor.Text, Editor.CaretOffset,
					new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line),
					new ResolverContext { ParseCache = ParseCache, ImportCache = ImportCache, ScopedBlock = lastSelectedBlock },
					true, true);

				ResolveResult res = null;
				// If there are multiple types, show a list of those items
				if (rr != null && rr.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in rr)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						res = rr[dlg.List.SelectedIndex];
					}
				}
				else if (rr.Length == 1)
					res = rr[0];
				else
				{
					MessageBox.Show("No symbol found!");
					return;
				}

				INode n = null;

				if (res is MemberResult)
					n = (res as MemberResult).ResolvedMember;
				else if (res is TypeResult)
					n = (res as TypeResult).ResolvedTypeDefinition;
				else if (res is ModuleResult)
					n = (res as ModuleResult).ResolvedModule;
				else
				{
					MessageBox.Show("Select valid symbol!");
					return;
				}

				var mod = n.NodeRoot as IAbstractSyntaxTree;
				if (mod == null)
					return;
				CoreManager.Instance.OpenFile(mod.FileName, n.StartLocation.Line, n.StartLocation.Column);
			}
			catch { }
		}

		void ContextMenu_AddImportStatement_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				if (SyntaxTree == null)
					return;

				var rr = DResolver.ResolveType(Editor.Text, Editor.CaretOffset,
					new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line),
					new ResolverContext { ParseCache = ParseCache, ImportCache = ImportCache, ScopedBlock = lastSelectedBlock },
					true, true);

				ResolveResult res = null;
				// If there are multiple types, show a list of those items
				if (rr != null && rr.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in rr)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						res = rr[dlg.List.SelectedIndex];
					}
				}
				else if (rr.Length == 1)
					res = rr[0];
				else
				{
					MessageBox.Show("No symbol found!");
					return;
				}

				INode n = null;

				if (res is MemberResult)
					n = (res as MemberResult).ResolvedMember;
				else if (res is TypeResult)
					n = (res as TypeResult).ResolvedTypeDefinition;

				if (n == null)
				{
					MessageBox.Show("Select valid symbol!");
					return;
				}

				var mod = n.NodeRoot as IAbstractSyntaxTree;
				if (mod == null)
					return;

				if (mod == SyntaxTree)
				{
					MessageBox.Show("Symbol is part of the current module. No import required!");
					return;
				}

				if (SyntaxTree.ContainsImport(mod.ModuleName))
				{
					MessageBox.Show("Module " + mod.ModuleName + " already imported!");
					return;
				}

				var loc = DParser.FindLastImportStatementEndLocation(Editor.Text);
				Editor.Document.BeginUpdate();
				Editor.Document.Insert(Editor.Document.GetOffset(loc.Line + 1, 0), "import " + mod.ModuleName + ";\r\n");
				KeysTyped = true;
				Editor.Document.EndUpdate();
			}
			catch { }
		}
		#endregion

		void Document_Changed(object sender, ICSharpCode.AvalonEdit.Document.DocumentChangeEventArgs e)
		{
			KeysTyped = true;
			Modified = true;
		}

		#region Code Completion
		/// <summary>
		/// Parses the current document content
		/// </summary>
		public void Parse()
		{
			IsParsing = true;
			string code = "";

			Dispatcher.Invoke(new Action(() => code = Editor.Text));

			DModule newAst = null;
			try
			{
				hp.Start();
				var parser = DParser.Create(new StringReader(code));
				code = null;

				newAst = parser.Parse();

				hp.Stop();

				ParseTime = hp.Duration;
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.Parser);
			}

			if (SyntaxTree != null && newAst != null)
				lock (SyntaxTree)
				{
					SyntaxTree.ParseErrors = newAst.ParseErrors;
					SyntaxTree.AssignFrom(newAst);
					UpdateImportCache();
				}
			else
				SyntaxTree = newAst;

			lastSelectedBlock = null;

			SyntaxTree.FileName = AbsoluteFilePath;
			SyntaxTree.ModuleName = ProposedModuleName;

			//TODO: Make semantic highlighting 1) faster and 2) redraw symbols immediately
			UpdateSemanticHightlighting();
			//CanRefreshSemanticHighlightings = false;

			UpdateBlockCompletionData();
			
			if (parseOperation != null && parseOperation.Status != DispatcherOperationStatus.Completed)
				parseOperation.Abort();

			parseOperation = Dispatcher.BeginInvoke(new Action(() =>
			{
				try
				{
					CoreManager.Instance.MainWindow.SecondLeftStatusText = Math.Round(hp.Duration * 1000, 3).ToString() + "ms (Parsing duration)";
					UpdateFoldings();
					CoreManager.ErrorManagement.RefreshErrorList();
					RefreshErrorHighlightings();
				}
				catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.System); }
			}));

			IsParsing = false;
		}

		public void ParserLoop()
		{
			// Initially parse the document
			Parse();
			bool HasBeenUpdatingParseCache = false;
			KeysTyped = false;

			while (true)
			{
				var cc = CompilerConfiguration;

				// While no keys were typed, do nothing
				while (!KeysTyped)
				{
					if (HasBeenUpdatingParseCache && !cc.ASTCache.IsParsing)
					{
						UpdateImportCache();
						UpdateSemanticHightlighting(true); // Perhaps new errors were detected
						HasBeenUpdatingParseCache = false;
					}
					else if (cc.ASTCache.IsParsing)
						HasBeenUpdatingParseCache = true;

					Thread.Sleep(50);
				}

				// Reset keystyped state for waiting again
				KeysTyped = false;

				// If a key was typed, wait.
				Thread.Sleep(1500);

				// If no other key was typed after waiting, parse the file
				if (KeysTyped)
					continue;

				// Prevent parsing it again; Assign 'false' to it before parsing the document, so if something was typed while parsing, it'll simply parse again
				KeysTyped = false;

				Parse();
			}
		}

		public void UpdateSemanticHightlighting(bool RedrawErrors=false)
		{
			if (!DSettings.Instance.UseSemanticHighlighting || SyntaxTree == null || CompilerConfiguration.ASTCache.IsParsing)
				return;

			var hp2 = new HighPrecisionTimer.HighPrecTimer();
			hp2.Start();

			var res = CodeScanner.ScanSymbols(new ResolverContext
			{
				ImportCache = ImportCache,
				ParseCache = ParseCache,
				// For performance reasons, do not scan down aliases
				ResolveAliases = false
				// Note: for correct results, base classes and variable types have to get resolved
			}, SyntaxTree);

			hp2.Stop();

			#region Step 3: Create/Update markers
			try
			{
				Dispatcher.Invoke(new Action<
					Dictionary<IdentifierDeclaration, ResolveResult>,
					List<IdentifierDeclaration>,
					HighPrecisionTimer.HighPrecTimer>
					((Dictionary<IdentifierDeclaration, ResolveResult> resolvedItems,
						List<IdentifierDeclaration> unresolvedItems,
						HighPrecisionTimer.HighPrecTimer highPrecTimer) =>
			{
				// Clear old markers
				foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
					if (marker is CodeSymbolMarker)
						marker.Delete();

				if (resolvedItems.Count > 0)
					foreach (var kv in resolvedItems)
						if (kv.Key.Location.Line > 0)
						{
							var m = new CodeSymbolMarker(this, kv.Key) { ResolveResult = kv.Value };
							MarkerStrategy.Add(m);

							m.Redraw();
						}

				SemanticErrors.Clear();

				if (unresolvedItems.Count > 0)
					foreach (var id in unresolvedItems)
						if (id.Location.Line > 0)
						{
							SemanticErrors.Add(new DSemanticError
							{
								FileName=AbsoluteFilePath,
								IsSemantic = true,
								Message = id.ToString() + " couldn't get resolved",
								Location = id.Location,
								MarkerColor=Colors.Blue
							});
						}

				if (RedrawErrors)
					CoreManager.ErrorManagement.RefreshErrorList();

				CoreManager.Instance.MainWindow.LeftStatusText =
					Math.Round(highPrecTimer.Duration * 1000, 2).ToString() +
					"ms (Semantic Highlighting)";
			}), DispatcherPriority.Background,
				res.ResolvedIdentifiers, res.UnresolvedIdentifiers, hp2);
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.System);
			}
			#endregion
		}

		public class CodeSymbolMarker : TextMarker
		{
			public readonly EditorDocument EditorDocument;
			public readonly IdentifierDeclaration Id;
			public ResolveResult ResolveResult;

			public CodeSymbolMarker(EditorDocument EditorDoc, IdentifierDeclaration Id, int StartOffset, int Length)
				: base(EditorDoc.MarkerStrategy, StartOffset, Length)
			{
				this.EditorDocument = EditorDoc;
				this.Id = Id;
				Init();
			}
			public CodeSymbolMarker(EditorDocument EditorDoc, IdentifierDeclaration Id)
				: base(EditorDoc.MarkerStrategy, EditorDoc.Editor.Document.GetOffset(Id.Location.Line, Id.Location.Column), Id.ToString(false).Length)
			{
				this.EditorDocument = EditorDoc;
				this.Id = Id;
				Init();
			}

			void Init()
			{
				this.MarkerType = TextMarkerType.None;
				ForegroundColor = Color.FromRgb(0x2b, 0x91, 0xaf);
			}
		}

		readonly List<GenericError> SemanticErrors = new List<GenericError>();

		public override System.Collections.Generic.IEnumerable<GenericError> ParserErrors
		{
			get
			{
				if (SyntaxTree != null)
				{
					var l = new List<GenericError>(SyntaxTree.ParseErrors.Count);
					foreach (var pe in SyntaxTree.ParseErrors)
						l.Add(new DParseError(pe) { Project = HasProject ? Project : null, FileName = AbsoluteFilePath });
					l.AddRange(SemanticErrors);
					return l;
				}
				return null;
			}
		}

		public CodeLocation CaretLocation
		{
			get { return new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line); }
		}

		void _insertTypeDataInternal(IBlockNode Parent, ref DCompletionData selectedItem, List<DCompletionData> types)
		{
			if (Parent != null)
				foreach (var n in Parent)
				{
					var completionData = new DCompletionData(n);
					if (selectedItem == null && CaretLocation >= n.StartLocation && CaretLocation <= n.EndLocation)
						selectedItem = completionData;
					types.Add(completionData);
				}
		}

		/// <summary>
		/// If different code block was selected, 
		/// update the list of items that are available in the current scope
		/// </summary>
		public void UpdateBlockCompletionData()
		{
			try
			{
				// Update highlit bracket offsets
				if (DSettings.Instance.EnableMatchingBracketHighlighting)
					CurrentlyHighlitBrackets = DBracketSearcher.SearchBrackets(Editor.Document, Editor.CaretOffset);
				else
					CurrentlyHighlitBrackets = null;


				if (SyntaxTree == null)
				{
					lookup_Members.ItemsSource = lookup_Types.ItemsSource = null;
					return;
				}

				IStatement curStmt = null;
				var curBlock = DResolver.SearchBlockAt(SyntaxTree, CaretLocation, out curStmt);

				if (blockCompletionDataOperation != null && blockCompletionDataOperation.Status != DispatcherOperationStatus.Completed)
					blockCompletionDataOperation.Abort();

				lastSelectedBlock = curBlock;

				blockCompletionDataOperation = Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						#region Update the type & member selectors
						isUpdatingLookupDropdowns = true; // Temporarily disable SelectionChanged event handling

						// First fill the Types-Dropdown
						var types = new List<DCompletionData>();
						DCompletionData selectedItem = null;
						var l1 = new List<INode> { SyntaxTree };
						var l2 = new List<INode>();

						while (l1.Count > 0)
						{
							foreach (var n in l1)
							{
								// Show all type declarations of the current module
								if (n is DClassLike)
								{
									var completionData = new DCompletionData(n);
									if (CaretLocation >= n.StartLocation && CaretLocation <= n.EndLocation)
										selectedItem = completionData;
									types.Add(completionData);
								}

								if (n is IBlockNode)
								{
									var ch = (n as IBlockNode).Children;
									if (ch != null)
										l2.AddRange(ch);
								}
							}

							l1.Clear();
							l1.AddRange(l2);
							l2.Clear();
						}

						if (selectedItem != null && selectedItem.Node is IBlockNode)
							curBlock = selectedItem.Node as IBlockNode;

						// For better usability, pre-sort items
						types.Sort();

						lookup_Types.ItemsSource = types;
						lookup_Types.SelectedItem = selectedItem;

						if (curBlock is IBlockNode)
						{
							selectedItem = null;
							// Fill the Members-Dropdown
							var members = new List<DCompletionData>();

							// Search a parent class to show all this one's members and to select that member where the caret currently is located
							var watchedParent = curBlock as IBlockNode;

							while (watchedParent != null && !(watchedParent is DClassLike || watchedParent is DEnum))
								watchedParent = watchedParent.Parent as IBlockNode;

							if (watchedParent != null)
								foreach (var n in watchedParent)
								{
									var cData = new DCompletionData(n);
									if (selectedItem == null && CaretLocation >= cData.Node.StartLocation && CaretLocation < cData.Node.EndLocation)
										selectedItem = cData;
									members.Add(cData);
								}

							members.Sort();

							lookup_Members.ItemsSource = members;
							lookup_Members.SelectedItem = selectedItem;
						}
						else
						{
							lookup_Members.ItemsSource = null;
							lookup_Members.SelectedItem = null;
						}

						isUpdatingLookupDropdowns = false;
						#endregion
					}
					catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
				}), DispatcherPriority.Background);
			}
			catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
		}

		void TextArea_SelectionChanged(object sender, EventArgs e)
		{
			UpdateBlockCompletionData();
		}

		/// <summary>
		/// Key: Path of the accessed item
		/// </summary>
		public readonly Dictionary<string, string> LastSelectedCCItems = new Dictionary<string, string>();
		ICompletionData lastSelectedCompletionData = null;

		/// <summary>
		/// Needed for pre-selection when completion list becomes opened next time
		/// </summary>
		string lastCompletionListResultPath = "";

		void ShowCodeCompletionWindow(string EnteredText)
		{
			try
			{
				if (string.IsNullOrEmpty(EnteredText) || !(char.IsLetter(EnteredText[0]) || EnteredText[0] == '.') || !DCodeCompletionSupport.CanShowCompletionWindow(this) || Editor.IsReadOnly)
					return;
				
				/*
				 * Note: Once we opened the completion list, it's not needed to care about a later refill of that list.
				 * The completionWindow will search the items that are partly typed into the editor automatically and on its own.
				 * - So there's just an initial filling required.
				 */

				if (completionWindow != null)
					return;
				/*
				if (showCompletionWindowOperation != null &&showCompletionWindowOperation.Status != DispatcherOperationStatus.Completed)
					showCompletionWindowOperation.Abort();
				*/
				// Init completion window here
				completionWindow = new CompletionWindow(Editor.TextArea);
				completionWindow.CompletionList.InsertionRequested += new EventHandler(CompletionList_InsertionRequested);
				//completionWindow.CloseAutomatically = true;

				DCodeCompletionSupport.BuildCompletionData(
					this, 
					completionWindow.CompletionList.CompletionData, 
					EnteredText, 
					out lastCompletionListResultPath);

				// If no data present, return
				if (completionWindow.CompletionList.CompletionData.Count < 1)
				{
					completionWindow = null;
					return;
				}

				// Care about item pre-selection
				var selectedString = "";

				if (lastCompletionListResultPath != null &&
					!LastSelectedCCItems.TryGetValue(lastCompletionListResultPath, out selectedString))
						LastSelectedCCItems.Add(lastCompletionListResultPath, "");

				if (!string.IsNullOrEmpty(selectedString))
				{
					// Prevent hiding all items that are not named as 'selectedString' .. after having selected our item, reset the filter property
					completionWindow.CompletionList.IsFiltering = false;
					completionWindow.CompletionList.SelectItem(selectedString);
					completionWindow.CompletionList.IsFiltering = true;
				}
				else // Select first item by default
					completionWindow.CompletionList.SelectedItem = completionWindow.CompletionList.CompletionData[0];

				completionWindow.Closed += (object o, EventArgs _e) => { 
					// 'Backup' the selected completion data
					lastSelectedCompletionData = completionWindow.CompletionList.SelectedItem;
					completionWindow = null; // After the window closed, reset it to null
				}; 
				completionWindow.Show();
			}
			catch (Exception ex) { ErrorLogger.Log(ex); completionWindow = null; }
		}

		void CompletionList_InsertionRequested(object sender, EventArgs e)
		{
			// After item got inserted, overwrite last-selected-item string
			if (lastCompletionListResultPath != null && lastSelectedCompletionData!=null)
				LastSelectedCCItems[lastCompletionListResultPath] = lastSelectedCompletionData.Text;
		}

		public void CloseCompletionPopups()
		{
			if (completionWindow != null)
			{
				completionWindow.Close();
				completionWindow = null;
			}

			if (insightWindow != null)
			{
				insightWindow.Close();
				insightWindow = null;
			}
		}

		/// <summary>
		/// Shows the popup that displays the currently accessed function and its parameters
		/// </summary>
		/// <param name="EnteredText"></param>
		void ShowInsightWindow(string EnteredText)
		{
			if (!DSettings.Instance.UseMethodInsight ||
				(EnteredText == "," && insightWindow != null && insightWindow.IsVisible))
				return;

			try
			{
				var data = D_IDE.D.CodeCompletion.DMethodOverloadProvider.Create(this);

				if (data == null)
					return;

				insightWindow = new OverloadInsightWindow(Editor.TextArea);
				insightWindow.Provider = data;

				var tt = new ToolTip();
				(insightWindow as Control).Background = tt.Background;

				insightWindow.Show();

			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		public bool CanShowCodeCompletionPopup
		{
			get
			{
				return
					DSettings.Instance.UseCodeCompletion &&
					SyntaxTree != null && //(SyntaxTree.ParseErrors!=null?SyntaxTree.ParseErrors.Count() <1 :true) &&
					!DResolver.CommentSearching.IsInCommentAreaOrString(Editor.Text, Editor.CaretOffset);
			}
		}

		void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (completionWindow != null)
			{
				// If entered key isn't part of the identifier anymore, close the completion window and insert the item text.
				if (!DCodeCompletionSupport.IsIdentifierChar(e.Text[0]))
					if (DSettings.Instance.ForceCodeCompetionPopupCommit)
						completionWindow.CompletionList.RequestInsertion(e);
					else
						completionWindow.Close();
			}

			// Return if there are parser errors - just to prevent crashes
			if (!CanShowCodeCompletionPopup)
				return;

			if (string.IsNullOrWhiteSpace(e.Text))
				return;

			// Note: Show completion window even before the first key has been processed by the editor!
			else if (char.IsLetter(e.Text[0]) && !DResolver.IsTypeIdentifier(Editor.Text, Editor.CaretOffset))
				ShowCodeCompletionWindow(e.Text);
		}

		void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			// If typed a block-related char, update line indentation
			if (e.Text == "{" || e.Text == "}")
				indentationStrategy.UpdateIndentation(e.Text);

			// Show the cc window after the dot has been inserted in the text because the cc win would overwrite it anyway
			if (e.Text == "." && CanShowCodeCompletionPopup)
				ShowCodeCompletionWindow(e.Text);

			else if (e.Text == "," || e.Text == "(")
				ShowInsightWindow(e.Text);

			else if (e.Text == ")" && insightWindow != null && insightWindow.IsLoaded)
				insightWindow.Close();
		}
		#endregion

		#region Editor events

		#region Document ToolTips
		void Editor_MouseHoverStopped(object sender, System.Windows.Input.MouseEventArgs e)
		{
			editorToolTip.IsOpen = false;
		}

		void Editor_MouseHover(object sender, System.Windows.Input.MouseEventArgs e)
		{
			try
			{
				var edpos = e.GetPosition(Editor);
				var pos = Editor.GetPositionFromPoint(edpos);
				if (pos.HasValue)
				{
					int offset = Editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
					// Avoid showing a tooltip if the cursor is located after a line-end
					var vpos = Editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(pos.Value.Line, Editor.Document.GetLineByNumber(pos.Value.Line).TotalLength), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineMiddle);
					// Add TextView position to Editor-related point
					vpos = Editor.TextArea.TextView.TranslatePoint(vpos, Editor);

					var ttArgs = new ToolTipRequestArgs(edpos.X <= vpos.X, pos.Value);
					try
					{
						bool handled = false;
						// Prefer showing error markers' error messages
						foreach (var tm in MarkerStrategy.TextMarkers)
							if (tm is ErrorMarker && tm.StartOffset <= offset && offset <= tm.EndOffset)
							{
								var em = tm as ErrorMarker;

								ttArgs.ToolTipContent = em.Error.Message;

								handled = true;
								break;
							}
						
						if (!handled)
							DCodeCompletionSupport.BuildToolTip(this, ttArgs);
					}
					catch (Exception ex)
					{
						ErrorLogger.Log(ex);
						return;
					}

					// If no content present, close and return
					if (ttArgs.ToolTipContent == null)
					{
						editorToolTip.IsOpen = false;
						return;
					}

					editorToolTip.PlacementTarget = this; // required for property inheritance
					editorToolTip.Content = ttArgs.ToolTipContent;
					editorToolTip.IsOpen = true;
					e.Handled = true;
				}
			}
			catch { }
		}
		#endregion
		#endregion

		/// <summary>
		/// Reformats all code lines.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void ReformatFileCmd(object sender, ExecutedRoutedEventArgs e)
		{
			indentationStrategy.IndentLines(Editor.Document, 1, Editor.Document.LineCount);
		}

		public string ModuleCode
		{
			get
			{
				return Editor.Document.Text;
			}
			set
			{
				Editor.Document.Text = value;
			}
		}

		public int CaretOffset
		{
			get
			{
				return Editor.CaretOffset;
			}
			set
			{
				Editor.CaretOffset = value;
			}
		}
	}

	public class ToolTipRequestArgs
	{
		public ToolTipRequestArgs(bool isDoc, TextViewPosition pos)
		{
			InDocument = isDoc;
			Position = pos;
		}

		public bool InDocument { get; protected set; }
		public TextViewPosition Position { get; protected set; }
		public int Line { get { return Position.Line; } }
		public int Column { get { return Position.Column; } }
		public object ToolTipContent { get; set; }
	}
}
