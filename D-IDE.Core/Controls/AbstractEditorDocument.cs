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
	public interface IAbstractEditor
	{
		Project Project { get; }
		bool HasProject { get; }
		string FileName { get; set; }
		string RelativeFilePath { get; set; }
		string AbsoluteFilePath { get; set;}
		bool Modified { get; }
		AbstractLanguageBinding LanguageBinding { get; }
	}

	public abstract class AbstractEditorDocument : DocumentContent,IAbstractEditor
	{
		public AbstractEditorDocument() { }
		public AbstractEditorDocument(string file)
		{
			this.FileName = file;
		}

		public Project Project
		{
			get
			{
				if (CoreManager.CurrentSolution != null)
				{
					var prjs=CoreManager.CurrentSolution.ProjectCache.Where(prj => prj.ContainsFile(FileName)).ToArray();
					if (prjs.Length > 0)
						return prjs[0];
				}
				return null;
			}
		}
		public bool HasProject
		{
			get {
				return Project != null;
			}
		}

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

				// Automatically search for the best-fitting language binding
				LanguageBinding = null;
				foreach (var lang in LanguageLoader.Bindings)
					if (lang.CanHandleFile(FileName))
					{
						LanguageBinding = lang;
						break;
					}
			}
		}

		/// <summary>
		/// Checks if file is stand-alone and not bound to any project.
		/// </summary>
		public bool IsUnboundNonExistingFile
		{
			get { return !HasProject && Path.GetDirectoryName(_FileName) == ""; }
		}

		public string RelativeFilePath
		{
			get
			{
				if (Path.IsPathRooted(FileName) && HasProject)
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

		public abstract bool Save();
		public abstract void Reload();


		public AbstractLanguageBinding LanguageBinding		{			get;	private set;		}
	}
}
