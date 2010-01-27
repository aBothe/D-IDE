using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Windows.Forms;
using System.Drawing;
using ICSharpCode.TextEditor;
using D_IDE;

namespace D_Parser.CodeCompletion
{
	class DCodeCompletionWindow : AbstractCompletionWindow
	{
		ICompletionData[] completionData;
		CodeCompletionListView   codeCompletionListView;
		VScrollBar    vScrollBar = new VScrollBar();
		ICompletionDataProvider dataProvider;
		IDocument document;
		
		public int                      startOffset;
		public int                      endOffset;
		DeclarationViewWindow    declarationViewWindow = null;
		Rectangle workingScreen;
		
		public static DCodeCompletionWindow ShowCompletionWindow(Form parent, TextEditorControl control, string fileName, ICompletionDataProvider completionDataProvider, char firstChar)
		{
			ICompletionData[] completionData = completionDataProvider.GenerateCompletionData(fileName, control.ActiveTextAreaControl.TextArea, firstChar);
			if (completionData == null || completionData.Length == 0) {
				return null;
			}
			DCodeCompletionWindow codeCompletionWindow = new DCodeCompletionWindow(completionDataProvider, completionData, parent, control);
			codeCompletionWindow.ShowCompletionWindow();
			return codeCompletionWindow;
		}
		
		DCodeCompletionWindow(ICompletionDataProvider completionDataProvider, ICompletionData[] completionData, Form parentForm, TextEditorControl control) : base(parentForm, control)
		{
			this.dataProvider = completionDataProvider;
			this.completionData = completionData;
			this.document = control.Document;
			
			workingScreen = Screen.GetWorkingArea(Location);
			startOffset = control.ActiveTextAreaControl.Caret.Offset + 1;
			endOffset   = startOffset;
			if (completionDataProvider.PreSelection != null) {
				startOffset -= completionDataProvider.PreSelection.Length + 1;
				endOffset--;
			}
			
			codeCompletionListView = new CodeCompletionListView(completionData);
			codeCompletionListView.ImageList = completionDataProvider.ImageList;
			codeCompletionListView.Dock = DockStyle.Fill;
			codeCompletionListView.SelectedItemChanged += new EventHandler(CodeCompletionListViewSelectedItemChanged);
			codeCompletionListView.DoubleClick += new EventHandler(CodeCompletionListViewDoubleClick);
			codeCompletionListView.Click  += new EventHandler(CodeCompletionListViewClick);
			Controls.Add(codeCompletionListView);
			
			const int MaxListLength = 15;
			if (completionData.Length > MaxListLength) {
				vScrollBar.Dock = DockStyle.Right;
				vScrollBar.Minimum = 0;
				vScrollBar.Maximum = completionData.Length - 1;
				vScrollBar.SmallChange = 1;
				vScrollBar.LargeChange = MaxListLength;
				codeCompletionListView.FirstItemChanged += new EventHandler(CodeCompletionListViewFirstItemChanged);
				Controls.Add(vScrollBar);
			}
			
			this.drawingSize = new Size(codeCompletionListView.ItemHeight * 10,
			                            codeCompletionListView.ItemHeight * Math.Min(MaxListLength, completionData.Length));
			SetLocation();
			
			if (declarationViewWindow == null) {
				declarationViewWindow = new DeclarationViewWindow(parentForm);
			}
			SetDeclarationViewLocation();
			declarationViewWindow.ShowDeclarationViewWindow();
			declarationViewWindow.MouseMove += ControlMouseMove;
			control.Focus();
			CodeCompletionListViewSelectedItemChanged(this, EventArgs.Empty);
			
			if (completionDataProvider.DefaultIndex >= 0) {
				codeCompletionListView.SelectIndex(completionDataProvider.DefaultIndex);
			}
			
			if (completionDataProvider.PreSelection != null) {
				CaretOffsetChanged(this, EventArgs.Empty);
			}
			
			vScrollBar.ValueChanged += VScrollBarValueChanged;
			document.DocumentAboutToBeChanged += DocumentAboutToBeChanged;
			control.ActiveTextAreaControl.TextArea.KeyEventHandler += TextArea_KeyEventHandler;
		}

		bool TextArea_KeyEventHandler(char ch)
		{
			if(!Char.IsLetterOrDigit(ch) && ch!='_')
			{
				InsertSelectedItem(ch);
			}
			return false;
		}
		
		bool inScrollUpdate;
		
		void CodeCompletionListViewFirstItemChanged(object sender, EventArgs e)
		{
			if (inScrollUpdate) return;
			inScrollUpdate = true;
			vScrollBar.Value = Math.Min(vScrollBar.Maximum, codeCompletionListView.FirstItem);
			inScrollUpdate = false;
		}
		
		void VScrollBarValueChanged(object sender, EventArgs e)
		{
			if (inScrollUpdate) return;
			inScrollUpdate = true;
			codeCompletionListView.FirstItem = vScrollBar.Value;
			codeCompletionListView.Refresh();
			control.ActiveTextAreaControl.TextArea.Focus();
			inScrollUpdate = false;
		}
		
		void SetDeclarationViewLocation()
		{
			//  This method uses the side with more free space
			int leftSpace = Bounds.Left - workingScreen.Left;
			int rightSpace = workingScreen.Right - Bounds.Right;
			Point pos;
			// The declaration view window has better line break when used on
			// the right side, so prefer the right side to the left.
			if (rightSpace * 2 > leftSpace)
				pos = new Point(Bounds.Right, Bounds.Top);
			else
				pos = new Point(Bounds.Left - declarationViewWindow.Width, Bounds.Top);
			if (declarationViewWindow.Location != pos) {
				declarationViewWindow.Location = pos;
			}
		}
		
		protected override void SetLocation()
		{
			base.SetLocation();
			if (declarationViewWindow != null) {
				SetDeclarationViewLocation();
			}
		}
		
		public void HandleMouseWheel(MouseEventArgs e)
		{
			int MAX_DELTA  = 120; // basically it's constant now, but could be changed later by MS
			int multiplier = e.Delta / MAX_DELTA;
			multiplier *= System.Windows.Forms.SystemInformation.MouseWheelScrollLines * vScrollBar.SmallChange;
			
			int newValue;
			if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines > 0) {
				newValue = this.vScrollBar.Value - (control.TextEditorProperties.MouseWheelScrollDown ? 1 : -1) * multiplier;
			} else {
				newValue = this.vScrollBar.Value - (control.TextEditorProperties.MouseWheelScrollDown ? 1 : -1) * multiplier;
			}
			vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, newValue));
		}

		void CodeCompletionListViewSelectedItemChanged(object sender, EventArgs e)
		{
			ICompletionData data = codeCompletionListView.SelectedCompletionData;
			if (data != null && data.Description != null && data.Description.Length > 0) {
				declarationViewWindow.Description = data.Description;
				SetDeclarationViewLocation();
			} else {
				declarationViewWindow.Description = null;
			}
		}
		
		public override bool ProcessKeyEvent(char ch)
		{
			switch (dataProvider.ProcessKey(ch)) {
				case CompletionDataProviderKeyResult.BeforeStartKey:
					// increment start+end, then process as normal char
					++startOffset;
					++endOffset;
					return base.ProcessKeyEvent(ch);
				case CompletionDataProviderKeyResult.NormalKey:
					// just process normally
					return base.ProcessKeyEvent(ch);
				case CompletionDataProviderKeyResult.InsertionKey:
					return InsertSelectedItem(ch);
				default:
					throw new InvalidOperationException("Invalid return value of dataProvider.ProcessKey");
			}
		}
		
		void DocumentAboutToBeChanged(object sender, DocumentEventArgs e)
		{
			// => startOffset test required so that this startOffset/endOffset are not incremented again
			//    for BeforeStartKey characters
			if (e.Offset >= startOffset && e.Offset <= endOffset) {
				if (e.Length > 0) { // length of removed region
					endOffset -= e.Length;
				}
				if (!string.IsNullOrEmpty(e.Text)) {
					endOffset += e.Text.Length;
				}
			}
		}
		
		protected override void CaretOffsetChanged(object sender, EventArgs e)
		{
			int offset = control.ActiveTextAreaControl.Caret.Offset;
			if (offset == startOffset) {
				return;
			}
			if (offset < startOffset || offset > endOffset) {
				Close();
			} else {
				try
				{
					codeCompletionListView.SelectItemWithStart(control.Document.GetText(startOffset, offset - startOffset));
				}
				catch { }
			}
		}
		
		protected override bool ProcessTextAreaKey(Keys key)
		{
			if (!Visible) {
				return false;
			}

			switch (key) {
				case Keys.Escape:
				case Keys.Delete:
					Close();
					return true;
				case Keys.Home:
					codeCompletionListView.SelectIndex(0);
					return true;
				case Keys.End:
					codeCompletionListView.SelectIndex(completionData.Length-1);
					return true;
				case Keys.PageDown:
					codeCompletionListView.PageDown();
					return true;
				case Keys.PageUp:
					codeCompletionListView.PageUp();
					return true;
				case Keys.Down:
					codeCompletionListView.SelectNextItem();
					return true;
				case Keys.Up:
					codeCompletionListView.SelectPrevItem();
					return true;
				case Keys.Tab:
				case Keys.Return:
					InsertSelectedItem('\0');
					return true;
			}
			return base.ProcessTextAreaKey(key);
		}
		
		void CodeCompletionListViewDoubleClick(object sender, EventArgs e)
		{
			InsertSelectedItem('\0');
		}
		
		void CodeCompletionListViewClick(object sender, EventArgs e)
		{
			control.ActiveTextAreaControl.TextArea.Focus();
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				document.DocumentAboutToBeChanged -= DocumentAboutToBeChanged;
				control.ActiveTextAreaControl.TextArea.KeyEventHandler -= TextArea_KeyEventHandler;
				if (codeCompletionListView != null) {
					codeCompletionListView.Dispose();
					codeCompletionListView = null;
				}
				if (declarationViewWindow != null) {
					declarationViewWindow.Dispose();
					declarationViewWindow = null;
				}
			}
			base.Dispose(disposing);
		}
		
		bool InsertSelectedItem(char ch)
		{
			document.DocumentAboutToBeChanged -= DocumentAboutToBeChanged;
			control.ActiveTextAreaControl.TextArea.KeyEventHandler -= TextArea_KeyEventHandler;
			if(codeCompletionListView == null) return false;
			ICompletionData data = codeCompletionListView.SelectedCompletionData;
			bool result = false;
			if (data != null) {
				control.BeginUpdate();
				
				try {
					if (endOffset - startOffset > 0) {
						control.Document.Remove(startOffset, endOffset - startOffset);
					}
					if(startOffset <= document.TextLength)
					result = dataProvider.InsertAction(data, control.ActiveTextAreaControl.TextArea, startOffset, ch);
				} finally {
					control.EndUpdate();
				}
			}
			Close();
			return result;
		}
	}
}
