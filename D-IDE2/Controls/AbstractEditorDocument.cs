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
		public abstract TextEditor Editor { get; }
		public abstract IModule Module { get; }
	}
}
