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

		public string GetSolutionRelatedPath(string path)
		{
			if (!Path.IsPathRooted(path))
				return path;
			if (path.StartsWith(BaseDir))
				return path.Substring(BaseDir.Length).Trim('\\');
			return path;
		}

		#region Project management
		/// <summary>
		/// Adds a project to the solution.
		/// Sets its solution property to 'this' solution.
		/// Adds it to the project cache.
		/// </summary>
		/// <param name="Project"></param>
		public void AddProject(Project Project)
		{
			if (ProjectCache.Contains(Project))
				return;

			var prjPath = GetSolutionRelatedPath(Project.FileName);
			if (!_ProjectFiles.Contains(prjPath))
				_ProjectFiles.Add(prjPath);

			ProjectCache.Add(Project);
			Project.Solution = this;
		}

		public void AddProject(string file)
		{
			if (_ProjectFiles.Contains(file))
				return;

			_ProjectFiles.Add(file);
		}

		public bool IsProjectLoaded(string file)
		{
			foreach (var p in ProjectCache)
				if (p.FileName == file)
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
			_ProjectFiles.Remove(file);
		}
		#endregion

		/// <summary>
		/// Saves the solution to file named <see cref="FileName"/>
		/// </summary>
		public void Save()
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
					x.WriteAttributeString("isStartProject","true");
				x.WriteCData(s);
				x.WriteEndElement();
			}

			x.WriteEndElement(); // projects
			x.WriteEndElement(); // solution

			x.Flush();
			x.Close();
		}

		#region Properties
		public string Name { get; set; }
		public string FileName { get; set; }
		public string BaseDir {
			get {
				return Path.GetDirectoryName(FileName);
			}
		}

		readonly List<string> _ProjectFiles = new List<string>();
		string _StartPrjFile;

		public Project this[string file]
		{
			get {
				foreach (var p in ProjectCache)
					if (p.FileName == file)
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
				return this[_StartPrjFile];
			}
			set {
				if (_ProjectFiles.Contains(value.FileName))
					_StartPrjFile = value.FileName;
				else throw new Exception("Project "+value.Name+" is not part of this Solution");
			}
		}
		#endregion

		/// <summary>
		/// Builds the solution incrementally
		/// </summary>
		public void Build()
		{

		}

		/// <summary>
		/// Cleans the output and build the solution again
		/// </summary>
		public void Rebuild()
		{

		}

		/// <summary>
		/// Cleans the output/ Removes output directory
		/// </summary>
		public void CleanUpOutput()
		{

		}

		public Dictionary<Project, BuildError[]> LastBuildErrors
		{
			get
			{
				var ret = new Dictionary<Project, BuildError[]>(ProjectCache.Count);

				foreach (var p in ProjectCache)
					ret.Add(p,p.LastBuildErrors.ToArray());

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
