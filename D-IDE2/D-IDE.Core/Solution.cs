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
			if (Path.IsPathRooted(file))
				return file.Remove(0, BaseDirectory.Length).Trim('\\');
			return file;
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

			var prjPath = ToRelativeFileName(Project.FileName);
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
