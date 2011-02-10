using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace D_IDE.Core
{
	public class Solution:IEnumerable<Project>
	{
		public const string SolutionExtension = ".idesln";

		public Solution() { }
		public Solution(string file)
		{
			FileName = file;
			Load();
		}

		public void Load()
		{
			if (!File.Exists(FileName))
				return;

			var x = XmlTextReader.Create(FileName);
			_ProjectFiles.Clear();
			try
			{
				while (x.Read())
					switch (x.LocalName)
					{
						case "name":
							Name = x.ReadString();
							break;
						case "project":
							var f = x.ReadString();
							_ProjectFiles.Add(f);
							if (x.HasAttributes && x.GetAttribute("isStartProject") !="false")
								_StartPrjFile = f;
							break;
					}
			}
			catch{}

			x.Close();
		}

		public string ToAbsoluteFileName(string file)
		{
			if (Path.IsPathRooted(file))
				return file;
			return BaseDirectory + "\\" + file;
		}
		public string ToRelativeFileName(string file)
		{
			if (Path.IsPathRooted(file) && file.StartsWith(BaseDirectory))
				return file.Substring(BaseDirectory.Length).Trim('\\');
			return file;
		}

		public Project ByName(string Name)
		{
			foreach (var p in ProjectCache)
				if (p.Name == Name)
					return p;
			return null;
		}

		#region Project management
		/// <summary>
		/// Adds a project to the solution.
		/// Sets its solution property to 'this' solution.
		/// Adds it to the project cache.
		/// </summary>
		/// <param name="Project"></param>
		public bool AddProject(Project Project)
		{
			if (!AddProject(Project.FileName))
				return false;
			
			if (!ProjectCache.Contains(Project))
				ProjectCache.Add(Project);
			Project.Solution = this;
			return true;
		}

		public bool AddProject(string file)
		{
			var relPath = ToRelativeFileName(file);
			if (_ProjectFiles.Contains(relPath))
				return false;

			_ProjectFiles.Add(relPath);
			return true;
		}

		public bool IsProjectLoaded(string file)
		{
			var absPath = ToAbsoluteFileName(file);
			foreach (var p in ProjectCache)
				if (p.FileName == absPath)
					return true;
			return false;
		}

		public bool UnloadProject(Project Project)
		{
			return ProjectCache.Remove(Project);
		}

		public void ExcludeProject(string file)
		{
			UnloadProject(this[file]);
			_ProjectFiles.Remove(ToRelativeFileName(file));
		}
		#endregion

		/// <summary>
		/// Saves the solution to file named <see cref="FileName"/>
		/// </summary>
		public bool Save()
		{
			/*
			 * If the file is still undefined, open a save file dialog
			 */
			if (String.IsNullOrEmpty(FileName))
			{
				var sf = new Microsoft.Win32.SaveFileDialog();
				sf.Filter="Solution (*"+SolutionExtension+")|*"+SolutionExtension;

				if (!sf.ShowDialog().Value)
					return false;
				else
					FileName = sf.FileName;
			}
			Util.CreateDirectoryRecursively(Path.GetDirectoryName(FileName));


			try
			{
				var x = XmlTextWriter.Create(FileName);

				x.WriteStartDocument();
				x.WriteStartElement("solution");

				x.WriteStartElement("name");
				x.WriteCData(Name);
				x.WriteEndElement();

				x.WriteStartElement("projects");

				foreach (var s in _ProjectFiles)
				{
					x.WriteStartElement("project");
					if (s == _StartPrjFile)
						x.WriteAttributeString("isStartProject", "true");
					x.WriteCData(s);
					x.WriteEndElement();
				}

				x.WriteEndElement(); // projects
				x.WriteEndElement(); // solution

				x.Flush();
				x.Close();
			}
			catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			return true;
		}

		#region Properties
		public string Name;
		public string FileName;
		public string BaseDirectory
		{
			get { return Path.GetDirectoryName(FileName); }
			set { FileName = value + Path.GetFileName(FileName); }
		}

		public bool ContainsProject(string file)
		{
			var relPath = ToRelativeFileName(file);
			return _ProjectFiles.Count(s => s == relPath) > 0;
		}

		public bool ContainsProject(Project prj)
		{
			return ContainsProject(prj.FileName);
		}

		readonly List<string> _ProjectFiles = new List<string>();
		string _StartPrjFile;

		public Project this[string file]
		{
			get {
				var absPath = ToAbsoluteFileName(file);
				foreach (var p in ProjectCache)
					if (ToAbsoluteFileName( p.FileName) == absPath)
						return p;
				return null;
			}
		}

		public string[] ProjectFiles {
			get { 
				return _ProjectFiles.ToArray(); 
			}
		}
		
		public readonly List<Project> ProjectCache = new List<Project>();

		public Project StartProject
		{
			get {
				if (string.IsNullOrEmpty(_StartPrjFile) && _ProjectFiles.Count>0)
					_StartPrjFile = _ProjectFiles[0];

				return this[_StartPrjFile];
			}
			set {
				if (_ProjectFiles.Contains(ToRelativeFileName( value.FileName)))
					_StartPrjFile =ToRelativeFileName( value.FileName);
				else throw new Exception("Project "+value.Name+" is not part of this Solution");
			}
		}
		#endregion

		public Dictionary<Project, GenericError[]> LastBuildErrors
		{
			get
			{
				var ret = new Dictionary<Project, GenericError[]>(ProjectCache.Count);

				foreach (var p in ProjectCache)
					ret.Add(p,p.LastBuildResult.BuildErrors.ToArray());

				return ret;
			}
		}

		public IEnumerator<Project> GetEnumerator()
		{
			return ProjectCache.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ProjectCache.GetEnumerator();
		}
	}
}
