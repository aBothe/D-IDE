using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.Linq;
using System.IO;

namespace D_IDE.Core
{
	public class Project:IEnumerable<string>
	{
		public Project() { }
		public Project(string prjFile) { FileName = prjFile; ReloadProject(); }
		public Project(Solution sln, string prjFile)
		{
			Solution = sln;
			FileName = prjFile;

			ReloadProject();
		}


		/// <summary>
		/// Central method to load a project whereas its file extension is used to identify
		/// the generic project type.
		/// </summary>
		public static Project LoadProjectFromFile(string FileName)
		{
			string ls = FileName.ToLower();

			foreach (var lang in from l in LanguageLoader.Bindings where l.ProjectsSupported select l)
				foreach (var pt in lang.ProjectTemplates)
					if(pt.Extensions!=null)
						foreach (var ext in pt.Extensions)
							if (ls.EndsWith(ext))
								return lang.OpenProject(FileName);
			return null;
		}

		#region Properties
		public string Name;
		public string FileName;
		public string BaseDirectory {
			get { return Path.GetDirectoryName(FileName); }
			set	{ FileName = value + Path.GetFileName(FileName); }
		}
		public Solution Solution;

		protected readonly List<string> _Files = new List<string>();

		/// <summary>
		/// Contain all project files including the file paths of the modules.
		/// All paths should be relative to the project base directory
		/// </summary>
		public string[] Files { get { return _Files.ToArray(); } }
		public bool ContainsFile(string file)
		{
			var relPath = ToRelativeFileName(file);
			return _Files.Contains(relPath);
		}
		/// <summary>
		/// Contains relative paths of empty but used directories
		/// </summary>
		public readonly List<string> SubDirectories = new List<string>();
		public readonly List<string> LastOpenedFiles = new List<string>();
		
		public string ToAbsoluteFileName(string file)
		{
			if (Path.IsPathRooted(file))
				return file;
			return BaseDirectory + "\\" + file;
		}

		public string ToRelativeFileName(string file)
		{
			if (Path.IsPathRooted(file))
				return file.Remove(0, BaseDirectory.Length).Trim('\\');
			return file;
		}

		public IEnumerator<string> GetEnumerator() { return _Files.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()	{	return _Files.GetEnumerator();	}
		#endregion

		public bool Save()
		{
			return true;
		}

		public void ReloadProject()
		{

		}

		public bool Add(string FileName)
		{
			return true;
		}

		public void Remove(string FileName)
		{

		}

		public bool Rename(string OldFileName, string NewFileName)
		{
			return true;
		}

		#region Build properties
		/// <summary>
		/// These files get copied into the output directory before compiling
		/// </summary>
		public readonly List<string> ExternalDepencies = new List<string>();
		public readonly List<Project> ProjectDependencies = new List<Project>();
		public readonly List<BuildError> LastBuildErrors = new List<BuildError>();

		public string OutputFile { get; set; }
		public string OutputDirectory { get; set; }
		public OutputTypes OutputType { get; set; }
		#endregion
	}

	public enum OutputTypes
	{
		/// <summary>
		/// Normal console-based application
		/// </summary>
		Executable,
		/// <summary>
		/// Executable that needs no console, e.g. Win32 executables
		/// </summary>
		CommandWindowLessExecutable,
		/// <summary>
		/// Windows DLL
		/// </summary>
		DynamicLibary,
		/// <summary>
		/// Non-Executable
		/// </summary>
		Other
	}

	public class BuildError
	{
		/// <summary>
		/// Can be null
		/// </summary>
		public readonly Project Project;
		public readonly string FileName;
		public readonly string Message;
		public readonly CodeLocation Location;
	}
}
