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
	public abstract class AbstractEditorDocument : DocumentContent
	{
		public AbstractEditorDocument() { }
		public AbstractEditorDocument(string file)
		{
			this.FileName = file;
		}
		public AbstractEditorDocument(Project prj, string file)
		{
			Project = prj;
			FileName = file;
		}

		public Project Project = null;
		string _FileName;
		public string FileName
		{
			get { return _FileName; }
			set
			{
				_FileName = value;
				if (String.IsNullOrEmpty(value)) // Enforce not-empty filenames
					_FileName = "Undefined";
				Modified = false;
			}
		}

		/// <summary>
		/// Checks if file is stand-alone and not bound to any project.
		/// </summary>
		public bool IsUnboundNonExistingFile
		{
			get { return Project == null && Path.GetDirectoryName(_FileName) == ""; }
		}

		public string RelativeFilePath
		{
			get
			{
				if (Path.IsPathRooted(FileName) && Project != null)
					return FileName.Remove(0, Project.BaseDirectory.Length).Trim('\\');
				return FileName;
			}
			set { FileName = value; }
		}

		public string AbsoluteFilePath
		{
			get
			{
				if (Path.IsPathRooted(FileName) || Project == null)
					return FileName;
				return Project.BaseDirectory + "\\" + FileName;
			}
			set { FileName = value; }
		}

		public bool Modified
		{
			get { return Title != null && Title.EndsWith("*"); }
			set { Title = Path.GetFileName(FileName) + (value ? "*" : ""); }
		}

		public abstract void Save();
		public abstract void Reload();
	}
}
