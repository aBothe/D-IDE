using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using D_IDE.Core;
using D_IDE.Core.Controls;
using D_Parser;
using D_Parser.Core;
using D_Parser.Resolver;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.AddIn;
using D_IDE.D.DEditor;

namespace D_IDE.D
{
	public class DEditorDocument:EditorDocument
	{
		#region Properties
		ComboBox lookup_Types;
		ComboBox lookup_Members;
		ToolTip editorToolTip = new ToolTip();
		DIndentationStrategy indentationStrategy;
		FoldingManager foldingManager;

		IAbstractSyntaxTree _unboundTree;
		public IAbstractSyntaxTree SyntaxTree { 
			get {
				if (HasProject)
				{
					var prj = Project as DProject;
					if(prj!=null)
						return prj.ParsedModules[AbsoluteFilePath];
				}

				return _unboundTree;
			}
			set {
				if(value!=null)
				value.FileName = AbsoluteFilePath;
				if (HasProject)
				{
					var prj = Project as DProject;
					if (prj != null)
						prj.ParsedModules[AbsoluteFilePath]=value;
				}
				_unboundTree=value;
			}
		}

		bool KeysTyped = false;
		Thread parseThread = null;

		bool isUpdatingLookupDropdowns = false;
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
			var stk = new Grid() {
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height=24,
				VerticalAlignment=VerticalAlignment.Top
			};

			// Give it two columns that have an equal width
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });

			// Move the editor away from the upper boundary
			Editor.Margin = new Thickness() { Top = stk.Height };
			
			MainEditorContainer.Children.Add(stk);

			lookup_Types = new ComboBox() { HorizontalAlignment=HorizontalAlignment.Stretch	};
			lookup_Members = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch };

			lookup_Types.SelectionChanged +=lookup_Types_SelectionChanged;
			lookup_Members.SelectionChanged+=lookup_Types_SelectionChanged;

			stk.Children.Add(lookup_Types);
			stk.Children.Add(lookup_Members);

			#region Setup dropdown item template
			var lookupItemTemplate =lookup_Members.ItemTemplate=lookup_Types.ItemTemplate= new DataTemplate { DataType = typeof(DCompletionData) };

			var sp = new FrameworkElementFactory(typeof( StackPanel));
			sp.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);
			sp.SetBinding(StackPanel.ToolTipProperty,new Binding("Description"));

			var iTemplate_Img = new FrameworkElementFactory( typeof(Image));
			iTemplate_Img.SetBinding(Image.SourceProperty,new Binding("Image"));
			iTemplate_Img.SetValue(Image.MarginProperty,new Thickness(1,1,4,1));
			sp.AppendChild(iTemplate_Img);

			var iTemplate_Name = new FrameworkElementFactory(typeof(TextBlock));
			iTemplate_Name.SetBinding(TextBlock.TextProperty,new Binding("PureNodeString"));
			sp.AppendChild(iTemplate_Name);

			lookupItemTemplate.VisualTree = sp;
			#endregion

			// Important: Move the members-lookup to column 1
			lookup_Members.SetValue(Grid.ColumnProperty,1);
			#endregion

			// Register CodeCompletion events
			Editor.TextArea.TextEntering += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntering);
			Editor.TextArea.TextEntered += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntered);
			Editor.Document.TextChanged += new EventHandler(Document_TextChanged);
			Editor.TextArea.Caret.PositionChanged += new EventHandler(TextArea_SelectionChanged);
			Editor.MouseHover += new System.Windows.Input.MouseEventHandler(Editor_MouseHover);
			Editor.MouseHoverStopped += new System.Windows.Input.MouseEventHandler(Editor_MouseHoverStopped);

			Editor.TextArea.IndentationStrategy= indentationStrategy = new DIndentationStrategy(this);
			foldingManager= ICSharpCode.AvalonEdit.Folding.FoldingManager.Install(Editor.TextArea);

			#region Init context menu
			var cm = new ContextMenu();
			Editor.ContextMenu = cm;

			var cmi = new MenuItem() { Header = "Add import directive", ToolTip="Add an import directive to the document if type cannot be resolved currently"};
			cmi.Click += ContextMenu_AddImportStatement_Click;
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Go to definition", ToolTip = "Go to the definition that defined the currently hovered item" };
			cmi.Click += new System.Windows.RoutedEventHandler(ContextMenu_GotoDefinition_Click);
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Toggle Breakpoint", 
				ToolTip = "Toggle breakpoint on the currently selected line",
				Command=D_IDE.Core.Controls.IDEUICommands.ToggleBreakpoint
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

			cmi = new MenuItem(){	Header = "Cut",	Command = System.Windows.Input.ApplicationCommands.Cut	};
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Copy", Command = System.Windows.Input.ApplicationCommands.Copy };
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Paste", Command = System.Windows.Input.ApplicationCommands.Paste };
			cm.Items.Add(cmi);
			#endregion

			//CommandBindings.Add(new CommandBinding(IDEUICommands.ReformatDoc,ReformatFileCmd));
			CommandBindings.Add(new CommandBinding(IDEUICommands.CommentBlock,CommentBlock));
			CommandBindings.Add(new CommandBinding(IDEUICommands.UncommentBlock,UncommentBlock));

			Parse();
		}

		List<string> foldedNodeNames = new List<string>();
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

			if (!(block is IAbstractSyntaxTree) && !block.BlockStartLocation.IsEmpty)
			{
				var fn=foldingManager.CreateFolding(
					Editor.Document.GetOffset(block.BlockStartLocation.Line, block.BlockStartLocation.Column),
					Editor.Document.GetOffset(block.EndLocation.Line, block.EndLocation.Column));
				//fn.Title = (block as AbstractNode).ToString(false,false);
				var nn=fn.Tag = block.ToString();

				if (foldedNodeNames.Contains(nn))
					fn.IsFolded = true;
			}

			foreach (var n in block)
				updateFoldingsInternal(n as IBlockNode);
		}

		void lookup_Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (isUpdatingLookupDropdowns)
				return;

			var completionData = e.AddedItems[0] as DCompletionData;

			if (completionData == null)
				return;

			Editor.TextArea.Caret.Position = new TextViewPosition(completionData.Node.StartLocation.Line,completionData.Node.StartLocation.Column);
			Editor.TextArea.Caret.BringCaretToView();
			Editor.Focus();
		}

		#region Code operations
		void CommentBlock(object s, ExecutedRoutedEventArgs e)
		{
			if (Editor.SelectionLength<1)
			{
				Editor.Document.Insert(Editor.Document.GetOffset(Editor.TextArea.Caret.Line,0),"//");
			}
			else
			{
				Editor.Document.UndoStack.StartUndoGroup();

				bool a, b, IsInBlock, IsInNested;
				DCodeResolver.Commenting.IsInCommentAreaOrString(Editor.Text,Editor.SelectionStart, out a, out b, out IsInBlock, out IsInNested);

				if (!IsInBlock && !IsInNested)
				{
					Editor.Document.Insert(Editor.SelectionStart+Editor.SelectionLength, "*/");
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
					Editor.Document.Remove(commStart - 1, 2);
					return;
				}
			}
			#endregion
			#region If no single-line comment was removed, delete multi-line comment block tags
			if (CaretOffset < 2) return;
			int off = CaretOffset - 2;

			// Seek the comment block opener
			commStart = DCodeResolver.Commenting.LastIndexOf(Editor.Text, false, off);
			int nestedCommStart = DCodeResolver.Commenting.LastIndexOf(Editor.Text, true, off);
			if (commStart < 0 && nestedCommStart < 0) return;

			// Seek the fitting comment block closer
			int off2 = off + (Math.Max(nestedCommStart, commStart) == off ? 2 : 0);
			int commEnd = DCodeResolver.Commenting.IndexOf(Editor.Text, false, off2);
			int commEnd2 = DCodeResolver.Commenting.IndexOf(Editor.Text, true, off2);

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

				var types = DCodeResolver.ResolveTypeDeclarations(
					SyntaxTree,
					Editor.Text,
					Editor.CaretOffset,
					new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line),
					false,
					DCodeCompletionSupport.EnumAvailableModules(this) // std.cstream.din.getc(); <<-- It's resolvable but not imported explictily! So also scan the global cache!
					//DCodeResolver.ResolveImports(EditorDocument.SyntaxTree,EnumAvailableModules(EditorDocument))
					, true
					).ToArray();

				INode n = null;
				// If there are multiple types, show a list of those items
				if (types.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in types)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						n = types[dlg.List.SelectedIndex];
					}
				}
				else if (types.Length == 1)
					n = types[0];
				else
				{
					MessageBox.Show("No symbol found!");
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

				var types = DCodeResolver.ResolveTypeDeclarations(
					SyntaxTree,
					Editor.Text,
					Editor.CaretOffset,
					new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line),
					false,
					DCodeCompletionSupport.EnumAvailableModules(this) // std.cstream.din.getc(); <<-- It's resolvable but not imported explictily! So also scan the global cache!
					, true
					).ToArray();

				INode n = null;
				// If there are multiple types, show a list of those items
				if (types.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in types)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						n = types[dlg.List.SelectedIndex];
					}
				}
				else if (types.Length == 1)
					n = types[0];
				else {
					MessageBox.Show("No symbol found!");
					return; }

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
					MessageBox.Show("Module "+mod.ModuleName+" already imported!");
					return;
				}

				var loc = DParser.FindLastImportStatementEndLocation(Editor.Text);
				Editor.Document.BeginUpdate();
				Editor.Document.Insert(Editor.Document.GetOffset(loc.Line+1,0),"import "+mod.ModuleName+";\r\n");
				KeysTyped = true;
				Editor.Document.EndUpdate();
			}
			catch { }
		}
		#endregion

		void Document_TextChanged(object sender, EventArgs e)
		{
			if (parseThread == null || !parseThread.IsAlive)
			{
				// This thread will continously check if the file was modified.
				// If so, it'll reparse
				parseThread = new Thread(() =>
				{
					Thread.CurrentThread.IsBackground = true;
					while (true)
					{
						// While no keys were typed, do nothing
						while (!KeysTyped)
							Thread.Sleep(50);

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
				});
				parseThread.Start();
			}

			KeysTyped = true;
		}

		#region Code Completion

		public string ProposedModuleName
		{
			get {
				if (HasProject)
					return Path.ChangeExtension(RelativeFilePath, null).Replace('\\', '.');
				else 
					return Path.GetFileNameWithoutExtension(FileName);
			}
		}

		/// <summary>
		/// Parses the current document content
		/// </summary>
		public void Parse()
		{
			if (parseOperation != null && parseOperation.Status != DispatcherOperationStatus.Completed)
				parseOperation.Abort();

			parseOperation= Dispatcher.BeginInvoke(new Action(()=>{
				try{
					if (SyntaxTree != null)
						lock (SyntaxTree)
							DParser.UpdateModuleFromText(SyntaxTree, Editor.Text);
					else
						SyntaxTree =DParser.ParseString(Editor.Text);
					SyntaxTree.FileName = AbsoluteFilePath;
					SyntaxTree.ModuleName = ProposedModuleName;

					UpdateBlockCompletionData();
					UpdateFoldings();
				}catch(Exception ex){ErrorLogger.Log(ex,ErrorType.Warning,ErrorOrigin.System);}
				CoreManager.ErrorManagement.RefreshErrorList();
			}));
		}

		public override System.Collections.Generic.IEnumerable<GenericError> ParserErrors
		{
			get
			{
				if (SyntaxTree != null)
					foreach (var pe in SyntaxTree.ParseErrors)
						yield return new DParseError(pe) { Project=HasProject?Project:null, FileName=AbsoluteFilePath};
			}
		}

		public CodeLocation CaretLocation
		{
			get { return new CodeLocation(Editor.TextArea.Caret.Column,Editor.TextArea.Caret.Line); }
		}

		public IBlockNode lastSelectedBlock{get;protected set;}
		IEnumerable<ICompletionData> currentEnvCompletionData = null;

		DispatcherOperation blockCompletionDataOperation = null;
		//DispatcherOperation showCompletionWindowOperation = null;
		DispatcherOperation parseOperation = null;


		/// <summary>
		/// If different code block was selected, 
		/// update the list of items that are available in the current scope
		/// </summary>
		public void UpdateBlockCompletionData()
		{
			// Update highlit bracket offsets
			if (DSettings.Instance.EnableMatchinBracketHighlighting)
				CurrentlyHighlitBrackets = DBracketSearcher.SearchBrackets(Editor.Document, Editor.CaretOffset);
			else
				CurrentlyHighlitBrackets = null;


			if (SyntaxTree == null)
			{
				lookup_Members.ItemsSource =lookup_Types.ItemsSource= null;
				currentEnvCompletionData = null;
				return;
			}

			var curBlock = DCodeResolver.SearchBlockAt(SyntaxTree, CaretLocation);
			if (curBlock != lastSelectedBlock)
			{
				if (blockCompletionDataOperation != null && blockCompletionDataOperation.Status != DispatcherOperationStatus.Completed)
					blockCompletionDataOperation.Abort();

				lastSelectedBlock = curBlock;

				blockCompletionDataOperation = Dispatcher.BeginInvoke(new Action(() =>
				{
					var l = new List<ICompletionData>();
					DCodeCompletionSupport.Instance.BuildCompletionData(this, l, null);
					currentEnvCompletionData = l;

					#region Update the type & member selectors
					isUpdatingLookupDropdowns = true; // Temporarily disable SelectionChanged event handling

					// First fill the Types-Dropdown
					var types = new List<DCompletionData>();
					DCompletionData selectedItem=null;
					// Show all members of the current module
					if(SyntaxTree!=null)
						foreach (var n in SyntaxTree){
							var completionData=new DCompletionData(n);
							if (selectedItem == null && CaretLocation >= n.StartLocation && CaretLocation < n.EndLocation)
								selectedItem = completionData;
							types.Add(completionData);
						}
					lookup_Types.ItemsSource = types;
					lookup_Types.SelectedItem = selectedItem;
					selectedItem = null;

					// Fill the Members-Dropdown
					var members = new List<DCompletionData>();

					// Search a parent class to show all this one's members and to select that member where the caret currently is located
					var watchedParent = curBlock as IBlockNode;
					while (watchedParent!=null && !(watchedParent is DClassLike || watchedParent is DEnum))
						watchedParent = watchedParent.Parent as IBlockNode;

					if(watchedParent!=null)
						foreach (var n in watchedParent)
						{
							var cData = new DCompletionData(n);
							if (selectedItem == null && CaretLocation >= cData.Node.StartLocation && CaretLocation < cData.Node.EndLocation)
								selectedItem = cData;
							members.Add(cData);
						}
					lookup_Members.ItemsSource = members;
					lookup_Members.SelectedItem = selectedItem;

					isUpdatingLookupDropdowns = false;
					#endregion
				}));
			}
			else
			// Update the member selection anyway
			if(lookup_Members.ItemsSource!=null)
				foreach (DCompletionData cData in lookup_Members.ItemsSource)
					if (CaretLocation >= cData.Node.StartLocation && CaretLocation < cData.Node.EndLocation)
					{
						lookup_Members.SelectedItem = cData;
						break;
					}
		}

		void TextArea_SelectionChanged(object sender, EventArgs e)
		{
			UpdateBlockCompletionData();
		}

		CompletionWindow completionWindow;
		InsightWindow insightWindow;

		void ShowCodeCompletionWindow(string EnteredText)
		{
			try
			{
				if (string.IsNullOrEmpty(EnteredText) || !(char.IsLetter(EnteredText[0]) || EnteredText[0]=='.') || !DCodeCompletionSupport.Instance.CanShowCompletionWindow(this) || Editor.IsReadOnly)
					return;

				/*
				 * Note: Once we opened the completion list, it's not needed to care about a later refill of that list.
				 * The completionWindow will search the items that are partly typed into the editor automatically and on its own.
				 * - So there's just an initial filling required.
				 */

				var ccs = DCodeCompletionSupport.Instance;

				if (completionWindow != null)
					return;
				/*
				if (showCompletionWindowOperation != null &&showCompletionWindowOperation.Status != DispatcherOperationStatus.Completed)
					showCompletionWindowOperation.Abort();
				*/
				// Init completion window here
				completionWindow = new CompletionWindow(Editor.TextArea);
				completionWindow.CloseAutomatically = true;

				//Dispatcher.Invoke(new Action(()=>{
					if (string.IsNullOrEmpty(EnteredText))
						foreach (var i in currentEnvCompletionData)
							completionWindow.CompletionList.CompletionData.Add(i);
					else
						ccs.BuildCompletionData(this, completionWindow.CompletionList.CompletionData, EnteredText);

					// If no data present, return
					if (completionWindow.CompletionList.CompletionData.Count < 1)
					{
						completionWindow = null;
						return;
					}

					completionWindow.Closed += (object o, EventArgs _e) => { completionWindow = null; }; // After the window closed, reset it to null
					completionWindow.Show();
				//}));
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		void ShowInsightWindow(string EnteredText)
		{
			//TODO: Show insight window and do all the function name resolution stuff...  Note: Remember deciding whether entering the template or normal arguments! foo!(int,bool)(23,"my String"); 
		}

		public bool CanShowCodeCompletionPopup
		{
			get {
				return 
					DSettings.Instance.UseCodeCompletion &&
					SyntaxTree!=null && //(SyntaxTree.ParseErrors!=null?SyntaxTree.ParseErrors.Count() <1 :true) &&
					!DCodeResolver.Commenting.IsInCommentAreaOrString(Editor.Text, Editor.CaretOffset);
			}
		}

		void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (completionWindow != null)
			{
				// If entered key isn't part of the identifier anymore, close the completion window and insert the item text.
				if (!DCodeCompletionSupport.Instance.IsIdentifierChar(e.Text[0]))
					completionWindow.Close(); //TODO: Rather close than insert if non-identifier-char has been detected?
					//completionWindow.CompletionList.RequestInsertion(e);
			}

			// Return if there are parser errors - just to prevent crashes
			if (!CanShowCodeCompletionPopup)
				return;

			if (string.IsNullOrWhiteSpace(e.Text))
				return;

			else if (e.Text == "," || e.Text == "(")
				ShowInsightWindow(e.Text);

			// Note: Show completion window even before the first key has been processed by the editor!
			else if(e.Text!=".")
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
		}
		#endregion

		#region Editor events
		void Editor_TextChanged(object sender, EventArgs e)
		{
			Modified = true;

			// Relocate/Update build errors
			foreach (var m in MarkerStrategy.TextMarkers)
			{
				var bem = m as ErrorMarker;
				if(bem==null)
					continue;

				var nloc=bem.EditorDocument.Editor.Document.GetLocation(bem.StartOffset);
				bem.Error.Line = nloc.Line;
				bem.Error.Column = nloc.Column;
			}			
		}

		void Document_LineCountChanged(object sender, EventArgs e)
		{
			// Relocate breakpoint positions - when not being in debug mode!
			if (!CoreManager.DebugManagement.IsDebugging)
				foreach (var mk in MarkerStrategy.TextMarkers)
				{
					var bpm = mk as BreakpointMarker;
					if (bpm != null)
					{
						bpm.Breakpoint.Line = Editor.Document.GetLineByOffset(bpm.StartOffset).LineNumber;
					}
				}
		}

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
					// Avoid showing a tooltip if the cursor is located after a line-end
					var vpos = Editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(pos.Value.Line, Editor.Document.GetLineByNumber(pos.Value.Line).TotalLength), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineMiddle);
					// Add TextView position to Editor-related point
					vpos = Editor.TextArea.TextView.TranslatePoint(vpos, Editor);

					var ttArgs = new ToolTipRequestArgs(edpos.X <= vpos.X, pos.Value);
					try
					{
						DCodeCompletionSupport.Instance.BuildToolTip(this, ttArgs);
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
