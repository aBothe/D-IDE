using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;
using AvalonDock;
using D_IDE.Core;
using System.IO;
using D_IDE.Controls;

namespace D_IDE
{
	public class EditorDocument:AbstractEditorDocument
	{
		#region Generic properties
		public EditorDocument()
		{
			Init();
		}

		public EditorDocument(string file):base(file)
		{
			Init();
		}

		public EditorDocument(Project prj, string file):base(prj,file)
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
				Editor.Save(AbsoluteFilePath);
			}
			catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			Modified = false;
			return true;
		}

		public override void Reload()
		{
			if(File.Exists(AbsoluteFilePath))
				Editor.Load(AbsoluteFilePath);
			Modified = false;
		}
	}
}
