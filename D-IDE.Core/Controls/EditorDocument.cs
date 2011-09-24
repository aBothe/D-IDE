using System;
using System.IO;
using System.Linq;
using D_IDE.Core;
using D_IDE.Core.Controls.Editor;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using D_IDE.Core.Controls;
using System.Windows;
using ICSharpCode.AvalonEdit.AddIn;
using System.Text;

namespace D_IDE.Core
{
	public interface IEditorDocument : IAbstractEditor
	{
		TextEditor Editor { get; }
		bool ReadOnly { get; set; }
		IEnumerable<GenericError> ParserErrors { get; }
	}

	public class EditorDocument:AbstractEditorDocument,IEditorDocument
	{
		#region Properties
		BracketHighlightRenderer bracketHightlighter;
		TextEditor editor = new TextEditor();
		public TextEditor Editor { get { return editor; } }
		public Grid MainEditorContainer { get; protected set; }

		public TextMarkerService MarkerStrategy { get; protected set; }

		public bool ReadOnly
		{
			get { return Editor.IsReadOnly; }
			set { Editor.IsReadOnly = true; }
		}
		DateTime lastWriteTime;
		#endregion

		public EditorDocument()
		{
			Init();
		}

		public EditorDocument(string file)
			: base(file)
		{
			Init();
		}

		#region Abstract Editor method overloads
		public override bool Save()
		{
			/*
			 * If the file is still undefined, open a save file dialog
			 */
			if (IsUnboundNonExistingFile)
			{
				var sf = new Microsoft.Win32.SaveFileDialog();
				sf.Filter = "All files (*.*)|*.*";
				sf.FileName = AbsoluteFilePath;

				if (!sf.ShowDialog().Value)
					return false;
				else
				{
					AbsoluteFilePath = sf.FileName;
					Modified = true;
				}
			}
			try
			{
				if (Modified)
				{
					Editor.Save(AbsoluteFilePath);
					lastWriteTime = File.GetLastWriteTimeUtc(AbsoluteFilePath);
				}
			}
			catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			Modified = false;
			return true;
		}

		public override void Reload()
		{
			if (File.Exists(AbsoluteFilePath))
			{
				var caretOffset = Editor.CaretOffset;
				Editor.Load(AbsoluteFilePath);
				if (Editor.Document.TextLength >= caretOffset)
					Editor.CaretOffset = caretOffset;
				lastWriteTime = File.GetLastWriteTimeUtc(AbsoluteFilePath);
			}

			UpdateSyntaxHighlighter();
			
			Modified = false;
		}

		/// <summary>
		/// 'Outside' means that the file has been modified somewhere else but not in D-IDE ;-D
		/// </summary>
		public bool HasBeenModifiedOutside
		{
			get { return File.Exists(AbsoluteFilePath)? lastWriteTime != File.GetLastWriteTimeUtc(AbsoluteFilePath):true; }
		}

		public void DoOutsideModificationCheck()
		{
			if (HasBeenModifiedOutside && lastWriteTime!=DateTime.MinValue)
			{
				var mbr = MessageBox.Show(CoreManager.Instance.MainWindow as Window,"Reload file?", "File has been modified", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
				if (mbr == MessageBoxResult.Yes)
					Reload();
			}
		}
		#endregion


		void Init()
		{
			// Apply common editor settings to this instance
			CommonEditorSettings.Instance.AssignToEditor(Editor);

			// If Ctrl+MouseWheel was pressed/turned, increase/decrease font size
			Editor.PreviewMouseWheel += new MouseWheelEventHandler((object sender, MouseWheelEventArgs e)=>
			{
				if (Keyboard.IsKeyDown(Key.LeftCtrl) ||	Keyboard.IsKeyDown(Key.RightCtrl))
				{
					CommonEditorSettings.Instance.FontSize = CommonEditorSettings.Instance.FontSize+( e.Delta > 0 ? 1 : -1);
					CommonEditorSettings.Instance.AssignAllOpenEditors();
					e.Handled = true;
				}
			});

			/*
			 * HACK: To re-focus the editor when this document is activated (the user chose this document tab page)
			 * , it's simply needed to catch the Loaded-Event which calls Focus on the editor instance then!
			 */
			Editor.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
				DoOutsideModificationCheck();
				Editor.Focus(); 
			});

			CommandBindings.Add(new CommandBinding(ApplicationCommands.Save,Save_event));
			CommandBindings.Add(new CommandBinding(IDEUICommands.ToggleBreakpoint,ToggleBreakpoint_event));

			var gr = new Grid();
			MainEditorContainer = gr;
			AddChild(gr);
			gr.Children.Add(Editor);

			Editor.Margin = new System.Windows.Thickness(0);
			Editor.BorderBrush = null;

			// Init bracket hightlighter
			bracketHightlighter = new BracketHighlightRenderer(Editor.TextArea.TextView);

			Editor.ShowLineNumbers = true;
			Editor.TextChanged += new EventHandler(Editor_TextChanged);
			Editor.Document.PropertyChanged+=new System.ComponentModel.PropertyChangedEventHandler(Document_PropertyChanged);

			// Register Marker strategy
			MarkerStrategy = new TextMarkerService(Editor);

			Editor.TextArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(TextArea_MouseRightButtonDown);

			// Make UTF-16 encoding default
			Editor.Encoding = Encoding.Unicode;

			// Load file contents if file path given
			Reload();

			// Initially draw all probably required text markers
			RefreshErrorHighlightings();
			RefreshBreakpointHighlightings();
			RefreshDebugHighlightings();
		}

		void Document_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "LineCount" && !CoreManager.DebugManagement.IsDebugging)
			{
				// Relocate breakpoint positions - when not being in debug mode!
				foreach (var mk in MarkerStrategy.TextMarkers)
				{
					var bpm = mk as BreakpointMarker;
					if (bpm != null)
					{
						bpm.Breakpoint.Line = Editor.Document.GetLineByOffset(bpm.StartOffset).LineNumber;
					}
				}
			}
		}

		void Save_event(object sender, RoutedEventArgs e)
		{
			Save();
		}

		void ToggleBreakpoint_event(object sender, RoutedEventArgs e)
		{
			CoreManager.BreakpointManagement.ToggleBreakpoint(AbsoluteFilePath, Editor.TextArea.Caret.Line);
			RefreshBreakpointHighlightings();
		}
		
		#region Syntax Highlighting

		/// <summary>
		/// Sets the bracket offsets that shall be highlighted
		/// </summary>
		protected BracketSearchResult CurrentlyHighlitBrackets
		{
			set
			{
				bracketHightlighter.SetHighlight(value);
			}
		}
		
		
		/// <summary>
		/// Associates a new syntax highligther with the editor
		/// </summary>
		public void UpdateSyntaxHighlighter()
		{
			try{
				var hi = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(FileName));
				Editor.SyntaxHighlighting = hi;
			}catch{}
		}

		public static IHighlightingDefinition GetHighlighting(string file)
		{
			return null;
		}

		public class ErrorMarker:TextMarker
		{
			public readonly EditorDocument EditorDocument;
			public readonly GenericError Error;

			public ErrorMarker(EditorDocument EditorDoc, GenericError Error)
				:base(EditorDoc.MarkerStrategy)
			{
				this.EditorDocument = EditorDoc;
				this.Error = Error;

				ForegroundColor = Error.ForegroundColor;
				BackgroundColor = Error.BackgroundColor;
				if (Error.MarkerColor.HasValue)
					MarkerColor = Error.MarkerColor.Value;

				// Init offsets manually
				StartOffset=EditorDoc.Editor.Document.GetOffset(Error.Line,Error.Column);

				if (Error.Length > 0)
					Length = Error.Length;
				else
					CalculateWordOffset(StartOffset, false);
			}
		}

		public class BreakpointMarker : TextMarker
		{
			public readonly BreakpointWrapper Breakpoint;

			public BreakpointMarker(EditorDocument EditorDoc, BreakpointWrapper breakPoint)
				:base(EditorDoc.MarkerStrategy,EditorDoc.Editor.Document.GetOffset(breakPoint.Line,0),true)
			{
				this.Breakpoint = breakPoint;

				MarkerType = TextMarkerType.None;
				BackgroundColor = Colors.DarkRed;
				ForegroundColor = Colors.White;
			}
		}

		public void RefreshErrorHighlightings()
		{
			// Clear old markers
			foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
				if (marker is ErrorMarker)
					marker.Delete();

			foreach (var err in CoreManager.ErrorManagement.GetErrorsForFile(AbsoluteFilePath))
			{
				var m = new ErrorMarker(this, err);
				MarkerStrategy.Add(m);

				m.Redraw();
			}
		}

		public void RefreshBreakpointHighlightings()
		{
			// Clear old markers
			foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
				if (marker is BreakpointMarker)
					marker.Delete();

			var bps = CoreManager.BreakpointManagement.GetBreakpointsAt(AbsoluteFilePath);
			if(bps!=null)
				foreach (var bpw in bps)
				{
					var m = new BreakpointMarker(this, bpw);
					MarkerStrategy.Add(m);

					m.Redraw();
				}
		}

		/// <summary>
		/// Current instruction frames
		/// </summary>
		public void RefreshDebugHighlightings()
		{
			foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
				if (marker is CoreManager.DebugManagement.DebugStackFrameMarker)
					marker.Delete();

			if (!CoreManager.DebugManagement.IsDebugging)
				return;

			var bps = CoreManager.DebugManagement.Engine.CallStack;
			if (bps != null)
				foreach (var stack in bps)
				{
					string fn;
					uint ln;
					if (CoreManager.DebugManagement.Engine.Symbols.GetLineByOffset(stack.InstructionOffset, out fn, out ln))
					{
						if (AbsoluteFilePath.EndsWith(fn) && ln<Editor.Document.LineCount)
						{
							var m = new CoreManager.DebugManagement.DebugStackFrameMarker(MarkerStrategy, stack, Editor.Document.GetOffset((int)ln,0));
							MarkerStrategy.Add(m);
							m.Redraw();
						}
					}
				}
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
			//CoreManager.Instance.MainWindow.RefreshErrorList();			
		}

		void TextArea_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// Automatically move the caret when right-clicking
			var position = Editor.GetPositionFromPoint(e.GetPosition(Editor));
			if (position.HasValue)
				Editor.TextArea.Caret.Position = position.Value;
		}
		#endregion

		public virtual IEnumerable<GenericError> ParserErrors
		{
			get { return null; }
		}
	}
}
