using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvalonDock;
using ICSharpCode.AvalonEdit;

namespace D_IDE
{
	class DSourceDocument : DockableContent
	{
		public readonly TextEditor Editor = new ICSharpCode.AvalonEdit.TextEditor();

		public DSourceDocument()
		{
			AddChild(Editor);

			Init();

			Editor.Document.Text = "Hallo Welt!";
		}

		void Init()
		{
			Editor.ShowLineNumbers = true;
		}
	}
}
