using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvalonDock;
using ICSharpCode.AvalonEdit;
using System.Reflection;
using D_IDE.Core;

namespace D_IDE
{
	public class EditorDocument:AbstractEditorDocument
	{
		public readonly TextEditor _editor = new ICSharpCode.AvalonEdit.TextEditor();
		public readonly IModule _module;

		public override IModule Module
		{
			get { return _module; }
		}

		public override TextEditor Editor
		{
			get { return _editor; }
		}

		public EditorDocument()
		{
			AddChild(Editor);

			Init();
		}

		void Init()
		{
			Editor.ShowLineNumbers = true;
		}
	}
}
