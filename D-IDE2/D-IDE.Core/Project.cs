using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.Linq;
using System.IO;
using System.Xml;

namespace D_IDE.Core
{
	public class Project:IEnumerable<ProjectModule>
	{
		public Project() { }
		public Project(Solution sln, string prjFile)
		{
			Solution = sln;
			FileName = sln.ToAbsoluteFileName( prjFile);

			ReloadProject();
		}


		/// <summary>
		/// Central method to load a project whereas its file extension is used to identify
		/// the generic project type.
		/// </summary>
		public static Project LoadProjectFromFile(Solution sln,string FileName)
		{
			if (!File.Exists(sln.ToAbsoluteFileName(FileName)))
			{
				ErrorLogger.Log(new FileNotFoundException("Couldn't load project because the file \"" + FileName + "\" was not found", FileName));
				return null;
			}
				string ls = FileName.ToLower();

				foreach (var lang in from l in LanguageLoader.Bindings where l.ProjectsSupported select l)
					foreach (var pt in lang.ProjectTemplates)
						if (pt.Extensions != null)
							foreach (var ext in pt.Extensions)
								if (ls.EndsWith(ext))
								{
									var ret = lang.OpenProject(sln, FileName);
									if (ret != null)
										sln.AddProject(ret); // Ensure the project is loaded into the cache
									return ret;
								}
				ErrorLogger.Log(new FileLoadException("Unkown project type", FileName));
				return null;
		}

		#region Properties
		public string Name;
		/// <summary>
		/// Absolute file path
		/// </summary>
		public string FileName;
		public string BaseDirectory {
			get { return Path.GetDirectoryName(FileName); }
			set	{ FileName = value + Path.GetFileName(FileName); }
		}
		public Solution Solution;

		protected readonly List<ProjectModule> _Files = new List<ProjectModule>();
		public readonly List<string> ProjectFileDependencies = new List<string>();

		public Project[] ProjectDependencies
		{
			get
			{
				var ret = new List<Project>();
				foreach (var file in ProjectFileDependencies)
					ret.Add(Solution[file]);
				return ret.ToArray();
			}
		}

		public ProjectModule[] GetFilesInDirectory(string dir)
		{
			var relDir = ToRelativeFileName(dir);
			return _Files.Where(m => m.FileName.StartsWith(relDir)).ToArray();
		}

		/// <summary>
		/// Contain all project files including the file paths of the modules.
		/// All paths should be relative to the project base directory
		/// </summary>
		public ProjectModule[] Files { get { return _Files.ToArray(); } }
		public IEnumerable<ProjectModule> CompilableFiles
		{
			get { return from f in _Files where f.Action == ProjectModule.BuildAction.Compile select f; }
		}

		public bool ContainsFile(string file)
		{
			var relPath = ToRelativeFileName(file);
			return _Files.Count(o => o.FileName==relPath)>0;
		}
		/// <summary>
		/// Contains relative paths of empty but used directories
		/// </summary>
		public readonly List<string> SubDirectories = new List<string>();
		public readonly List<string> LastOpenedFiles = new List<string>();
		
		public string ToAbsoluteFileName(string file)
		{
			var f = file.Trim('\\');
			if (Path.IsPathRooted(f))
				return f;
			return BaseDirectory + "\\" + f;
		}
		public string ToRelativeFileName(string file)
		{
			var f = file.Trim('\\');
			if (Path.IsPathRooted(f) && f.StartsWith(BaseDirectory))
				return f.Substring(BaseDirectory.Length).Trim('\\');
			return f;
		}

		public IEnumerator<ProjectModule> GetEnumerator() { return _Files.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()	{	return _Files.GetEnumerator();	}
		#endregion

		#region Saving & Loading
		protected delegate bool CustomReadEventHandler(XmlReader reader) ;
		protected delegate void CustomWriteEventHandler(XmlWriter writer);
		/// <summary>
		/// Enables custom data reading when reading from a project file.
		/// Gets called after every XmlReader.Read();
		/// </summary>
		protected CustomReadEventHandler OnReadElementFromFile;
		protected CustomWriteEventHandler OnWriteToFile;

		public bool Save()
		{
			if (String.IsNullOrEmpty(FileName))
				return false;

			var xw = XmlWriter.Create(FileName);
			xw.WriteStartDocument();

			xw.WriteStartElement("project");

			xw.WriteStartElement("name");
			xw.WriteCData(Name);
			xw.WriteEndElement();

			xw.WriteStartElement("files");
			foreach (var m in _Files)
			{
				xw.WriteStartElement("file");
				xw.WriteAttributeString("lastModified", m.LastModified.ToString());
				xw.WriteAttributeString("buildAction", ((int)m.Action).ToString());
				xw.WriteCData(m.FileName);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("dirs");
			// Only save 'empty' pre-added subdirectories
			foreach (var m in SubDirectories.Where(s=>GetFilesInDirectory(s).Length<1))
			{
				xw.WriteStartElement("dir");
				xw.WriteCData(m);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("lastopen");
			foreach (var s in LastOpenedFiles)
			{
				xw.WriteStartElement("file");
				xw.WriteCData(s);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("deps");
			foreach (var s in RequiredProjects)
			{
				xw.WriteStartElement("file");
				xw.WriteCData(s);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

				xw.WriteStartElement("enablesubversioning");
				xw.WriteAttributeString("value", EnableBuildVersioning ? "true" : "false");
				xw.WriteEndElement();

				xw.WriteStartElement("alsostoresources");
				xw.WriteAttributeString("value", AlsoStoreChangedSources?"true":"false");
				xw.WriteEndElement();

				xw.WriteStartElement("lastversioncount");
				xw.WriteAttributeString("value", LastBuildVersionCount.ToString());
				xw.WriteEndElement();

			if (OnWriteToFile != null)
				OnWriteToFile(xw);

			xw.WriteEndDocument();
			xw.Close();

			return true;
		}

		public bool ReloadProject()
		{
			var absPath = FileName;
			if (Solution != null)
				absPath = Solution.ToAbsoluteFileName(absPath);

			if (!File.Exists(absPath))
				return false;

			var xr = new XmlTextReader(absPath);
			XmlReader xsr = null;

			while (xr.Read())
			{
				if (OnReadElementFromFile != null)
					if (OnReadElementFromFile(xr)) // Continue if the event has handled the element
						continue;

				if (xr.NodeType == XmlNodeType.Element)
				{
					switch (xr.LocalName)
					{
						default: break;
						case "name":
							xr.Read();
							Name = xr.ReadString();
							break;

						case "files":
							_Files.Clear();

							xsr = xr.ReadSubtree();
							while (xsr.Read())
							{
								if (xsr.LocalName != "file") continue;
									long mod = 0;
									ProjectModule.BuildAction act =  ProjectModule.BuildAction.Compile;
									if (xsr.MoveToAttribute("lastModified"))
										mod = Convert.ToInt64(xsr.Value);
									if (xsr.MoveToAttribute("buildAction"))
										act=(ProjectModule.BuildAction)Convert.ToInt32(xsr.Value);
									xsr.MoveToElement();

									string _fn = xsr.ReadString();
									_Files.Add(new ProjectModule() { FileName=_fn, Action=act, LastModified=mod});
							}
							break;

						case "dirs":
							SubDirectories.Clear();

							xsr = xr.ReadSubtree();
							while (xsr.Read())
							{
								if (xsr.LocalName != "dir") continue;

								var dir = xsr.ReadString();
								SubDirectories.Add(dir);
							}
							break;

						case "lastopen":
							LastOpenedFiles.Clear();

							xsr = xr.ReadSubtree();
							while (xsr.Read())
							{
								if (xsr.NodeType == XmlNodeType.CDATA)
								{
									LastOpenedFiles.Add(xr.ReadString());
								}
							}
							break;

						case "deps":
							RequiredProjects.Clear();

							xsr = xr.ReadSubtree();
							while (xsr.Read())
							{
								if (xsr.NodeType == XmlNodeType.CDATA)
								{
									RequiredProjects.Add(xr.ReadString());
								}
							}
							break;

						case "enablesubversioning":
							if (xr.MoveToAttribute("value"))
							{
								EnableBuildVersioning = xr.Value == "true";
							}
							break;

						case "alsostoresources":
							if (xr.MoveToAttribute("value"))
							{
								AlsoStoreChangedSources = xr.Value == "true";
							}
							break;

						case "lastversioncount":
							if (xr.MoveToAttribute("value"))
							{
								try
								{
									LastBuildVersionCount = Convert.ToInt32(xr.Value);
								}
								catch { }
							}
							break;
					}
				}
			}
			xr.Close();
			return true;
		}
		#endregion

		public bool Add(string FileName)
		{
			if (ContainsFile(FileName))
			{
				ErrorLogger.Log(new ProjectException(this,"Project already contains "+FileName));
				return false;
			}

			// Check if other projects of the same solution own these files
			if(Solution!=null)
			foreach (var p in Solution)
				if (p.BaseDirectory == BaseDirectory && p.ContainsFile(FileName))
				{
					ErrorLogger.Log(new ProjectException(this,"An other Project is already containing "+FileName));
					return false;
				}

			_Files.Add(new ProjectModule() {  FileName=ToRelativeFileName(FileName), Action=ProjectModule.BuildAction.Compile});
			return true;
		}

		public bool Remove(string FileName)
		{
			var relPath=ToRelativeFileName(FileName);
			bool r= _Files.RemoveAll(m => m.FileName==relPath) >0;
			if(!r)
				ErrorLogger.Log(new ProjectException(this, "Couldn't remove " + FileName+ " from project"));
			return r;
		}

		#region Build properties

		public readonly List<string> RequiredProjects = new List<string>();
		public readonly List<BuildError> LastBuildErrors = new List<BuildError>();

		public string OutputFile { get; set; }
		public string OutputDirectory { get; set; }
		public OutputTypes OutputType { get; set; }

		public bool EnableBuildVersioning = false;
		public bool AlsoStoreChangedSources = false;
		public int LastBuildVersionCount = 0;
		#endregion
	}

	public class ProjectException : Exception
	{
		public readonly Project Project;
		public ProjectException(Project prj, string msg)
			: base(msg)
		{
			Project = prj;
		}
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

	public class ProjectModule{
		public string FileName;

		public long LastModified;
		public BuildAction Action=BuildAction.None;

		public enum BuildAction
		{
			None=0,
			Compile=1,
			CopyToOutput=2
		}
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
