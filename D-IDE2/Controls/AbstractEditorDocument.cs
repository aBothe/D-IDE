using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;
using AvalonDock;
using D_IDE.Core;

namespace D_IDE
{
	public abstract class AbstractEditorDocument:DockableContent
	{
		protected TextEditor _Editor=new TextEditor();
		protected IModule _Module;

		public TextEditor Editor { get { return _Editor; } }
		public IModule Module { get { return _Module; } }
	}
}
