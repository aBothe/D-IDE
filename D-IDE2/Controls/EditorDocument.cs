using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;
using AvalonDock;
using D_IDE.Core;
using System.IO;

namespace D_IDE
{
	public class EditorDocument:AbstractEditorDocument
	{
		#region Generic properties
		public EditorDocument()
		{
			Init();
		}

		public EditorDocument(IModule mod):base(mod)
		{
			Init();
		}

		public EditorDocument(string file):base(file)
		{
			Init();
		}

		public EditorDocument(IProject prj, string file):base(prj,file)
		{
			Init();
		}

		public readonly TextEditor Editor = new TextEditor();
		
		#endregion

		void Init()
		{
			AddChild(Editor);
			Editor.Margin = new System.Windows.Thickness(0);
			Editor.BorderBrush = null;

			Editor.ShowLineNumbers = true;
			Editor.TextChanged += new EventHandler(Editor_TextChanged);

			Reload();
		}

		#region Editor events
		void Editor_TextChanged(object sender, EventArgs e)
		{
			Modified = true;
		}
		#endregion

		public override void Save()
		{
			Editor.Save(AbsoluteFilePath);
			Modified = false;
		}

		public override void Reload()
		{
			Editor.Load(AbsoluteFilePath);
			Modified = false;
		}
	}
}
