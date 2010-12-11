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
		public EditorDocument() { _Module = new DefaultModule(); }

		public EditorDocument(IModule Module)
		{
			_Module = Module;
			Init();
		}

		void Init()
		{
			AddChild(Editor);
			Editor.ShowLineNumbers = true;
		}
	}
}
