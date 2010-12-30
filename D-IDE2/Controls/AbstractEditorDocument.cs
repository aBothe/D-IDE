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
		public AbstractEditorDocument(IModule mod)
		{
			Project = mod.Project;
			FileName = mod.FileName;
		}
		public AbstractEditorDocument(string file)
		{
			this.FileName = file;
		}
		public AbstractEditorDocument(IProject prj, string file)
		{
			Project = prj;
			FileName = file;
		}

		public IProject Project = null;
		string _FileName;
		public string FileName
		{
			get { return _FileName; }
			set
			{
				_FileName = value;
				Modified = false;
			}
		}

		public string RelativeFilePath
		{
			get
			{
				if (Path.IsPathRooted(FileName) && Project != null)
					return FileName.Remove(0, IDEManager.ProjectManagement.ProjectBaseDirectory(Project).Length).Trim('\\');
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
				return IDEManager.ProjectManagement.ProjectBaseDirectory(Project) + "\\" + FileName;
			}
			set { FileName = value; }
		}

		public IModule Module
		{
			get
			{
				return IDEManager.FileManagement.GetModule(Project, FileName);
			}
		}

		public bool Modified
		{
			get { return Title != null && Title.EndsWith("*"); }
			set { Title = Path.GetFileName(_FileName) + (value ? "*" : ""); }
		}

		public abstract void Save();
		public abstract void Reload();
	}
}
