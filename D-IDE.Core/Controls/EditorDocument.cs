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
		TextEditor editor = new TextEditor();
		public TextEditor Editor { get { return editor; } }
		public Grid MainEditorContainer { get; protected set; }

		public TextMarkerService MarkerStrategy { get; protected set; }

		public bool ReadOnly
		{
			get { return Editor.IsReadOnly; }
			set { Editor.IsReadOnly = true; }
		}
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
				sf.FileName = AbsoluteFilePath;

				if (!sf.ShowDialog().Value)
					return false;
				else
					AbsoluteFilePath = sf.FileName;
			}
			try
			{
				if(Modified)
					Editor.Save(AbsoluteFilePath);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			Modified = false;
			return true;
		}

		public override void Reload()
		{
			if (File.Exists(AbsoluteFilePath))
				Editor.Load(AbsoluteFilePath);

			UpdateSyntaxHighlighter();
			Modified = false;
		}
		#endregion


		void Init()
		{
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
			Editor.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e)=> Editor.Focus() );

			CommandBindings.Add(new CommandBinding(ApplicationCommands.Save,Save_event));
			CommandBindings.Add(new CommandBinding(IDEUICommands.ToggleBreakpoint,ToggleBreakpoint_event));

			var gr = new Grid();
			MainEditorContainer = gr;
			AddChild(gr);
			gr.Children.Add(Editor);

			Editor.Margin = new System.Windows.Thickness(0);
			Editor.BorderBrush = null;

			Reload();

			Editor.ShowLineNumbers = true;
			Editor.TextChanged += new EventHandler(Editor_TextChanged);
			Editor.Document.LineCountChanged += new EventHandler(Document_LineCountChanged);

			// Register Marker strategy
			var tv = Editor.TextArea.TextView;
			MarkerStrategy=new TextMarkerService(Editor);
			tv.Services.AddService(typeof(TextMarkerService), MarkerStrategy);
			tv.LineTransformers.Add(MarkerStrategy);
			tv.BackgroundRenderers.Add(MarkerStrategy);

			Editor.TextArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(TextArea_MouseRightButtonDown);

			// Initially draw all probably required text markers
			RefreshErrorHighlightings();
			RefreshBreakpointHighlightings();
			RefreshDebugHighlightings();
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
				:base(EditorDoc.MarkerStrategy,EditorDoc.Editor.Document.GetOffset(Error.Line,Error.Column),false)
			{
				this.EditorDocument = EditorDoc;
				this.Error = Error;
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
