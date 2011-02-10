using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.Linq;
using System.IO;
using System.Xml;
using System.ComponentModel;

namespace D_IDE.Core
{
	public class Project:IEnumerable<SourceModule>
	{
		#region Properties
		public Solution Solution;
		public string Name { get; set; }

		/// <summary>
		/// Absolute file path
		/// </summary>
		public string FileName;
		public string BaseDirectory
		{
			get { return Path.GetDirectoryName(FileName); }
		}

		public virtual AbstractProjectSettingsPage[] LanguageSpecificProjectSettings { get { return null; } }

		/// <summary>
		/// Contain all project files including the file paths of the modules.
		/// All paths should be relative to the project base directory
		/// </summary>
		public SourceModule[] Files { get { return _Files.ToArray(); } }
		/// <summary>
		/// Project's build version
		/// </summary>
		public ProjectVersion Version = new ProjectVersion();
		public bool AutoIncrementBuildNumber = true;

		/// <summary>
		/// Contains relative paths of empty but used directories
		/// </summary>
		public readonly List<string> SubDirectories = new List<string>();
		public readonly List<string> LastOpenedFiles = new List<string>();
		protected readonly List<SourceModule> _Files = new List<SourceModule>();
		public Project[] ProjectDependencies
		{
			get
			{
				var ret = new List<Project>(RequiredProjects.Count);
				foreach (var file in RequiredProjects)
					if (Solution.IsProjectLoaded(file))
						ret.Add(Solution[file]);
				return ret.ToArray();
			}
		}
		public SourceModule[] GetFilesInDirectory(string dir)
		{
			var relDir = ToRelativeFileName(dir);
			return _Files.Where(m => m.FileName.StartsWith(relDir)).ToArray();
		}
		public IEnumerable<SourceModule> CompilableFiles
		{
			get { return from f in _Files where f.Action == SourceModule.BuildAction.Compile select f; }
		}

		public readonly List<string> RequiredProjects = new List<string>();
		public BuildResult LastBuildResult { get; set; }
		public string ExecutingArguments { get; set; }
		protected string OutputFileOverride { get; set; }
		public string OutputFile
		{
			get
			{
				string ext = ".exe";
				if (OutputType == OutputTypes.DynamicLibary)
					ext = ".dll";

				if (string.IsNullOrEmpty(OutputFileOverride))
					return ToAbsoluteFileName("bin\\" + Util.PurifyFileName(Name) + ext);
				else return ToAbsoluteFileName(OutputFileOverride);
			}
			set
			{
				OutputFileOverride = value;
			}
		}
		public string OutputDirectory
		{
			get { return Path.GetDirectoryName(OutputFile); }
		}

		public OutputTypes OutputType { get; set; }

		public bool EnableBuildVersioning = false;
		public bool AlsoStoreChangedSources = false;
		#endregion

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

		public bool ContainsFile(string file)
		{
			var relPath = ToRelativeFileName(file);
			return _Files.Count(o => o.FileName==relPath)>0;
		}

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

		public IEnumerator<SourceModule> GetEnumerator() { return _Files.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()	{	return _Files.GetEnumerator();	}

		#region Saving & Loading
		protected virtual void SaveLanguageSpecificSettings(XmlWriter xw) {}
		protected virtual void LoadLanguageSpecificSettings(XmlReader xr) { }

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
				// Do not save the last modification timestamp because after re-opening the project it shall be rebuilt completely
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

			xw.WriteStartElement("version");
			xw.WriteAttributeString("autoincrementbuild",AutoIncrementBuildNumber.ToString());
			xw.WriteCData(Version.ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("languagespecific");
			SaveLanguageSpecificSettings(xw);

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
								var act=SourceModule.BuildAction.None;
									if (xsr.MoveToAttribute("buildAction"))
										act=(SourceModule.BuildAction)Convert.ToInt32(xsr.Value);
									xsr.MoveToElement();

									string _fn = xsr.ReadString();
									_Files.Add(new SourceModule() {Project=this, FileName=_fn, Action=act});
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
								EnableBuildVersioning = xr.Value == "true";
							break;

						case "alsostoresources":
							if (xr.MoveToAttribute("value"))
								AlsoStoreChangedSources = xr.Value == "true";
							break;

						case "version":
							if (xr.MoveToAttribute("autoincrementbuild"))
								AutoIncrementBuildNumber = xr.GetAttribute("autoincrementbuild").ToLower()=="true";
							Version.Parse(xr.ReadString());
							break;

						case "languagespecific":
							LoadLanguageSpecificSettings(xr.ReadSubtree());
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

			_Files.Add(new SourceModule() {Project=this,  FileName=ToRelativeFileName(FileName), Action=SourceModule.BuildAction.Compile});
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

		/// <summary>
		/// Copies all files to project's output directory
		/// </summary>
		public void CopyCopyableOutputFiles()
		{
			CopyCopyableOutputFiles(OutputDirectory);
		}

		/// <summary>
		/// Copies all files which are marked as 'Copy' to targetDirectory
		/// </summary>
		public void CopyCopyableOutputFiles(string targetDirectory)
		{
			string fn = "";
			foreach (var pf in _Files)
			{
				if (pf.Action != SourceModule.BuildAction.CopyToOutput)
					continue;

				fn = ToAbsoluteFileName(pf.FileName);
				string tg=Path.Combine(targetDirectory, Path.GetFileName(pf.FileName));

				pf.LastBuildResult = new BuildResult() { 
					TargetFile=tg,
					SourceFile=fn};

				if (!File.Exists(fn))
				{
					pf.LastBuildResult.BuildErrors.Add(new GenericError() { Message=fn+" not found"});
					continue;
				}

				try
				{
					File.Copy(ToAbsoluteFileName(fn), tg);
					pf.LastBuildResult.Successful = true;
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
					pf.LastBuildResult.BuildErrors.Add(new GenericError() { Message=ex.Message});
				}
			}
		}
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

	/// <summary>
	/// Represents a project's source file
	/// </summary>
	public class SourceModule{
		public Project Project { get; set; }
		public string FileName { get; set; }

		public string AbsoluteFileName
		{
			get
			{
				if (Project != null)
					return Project.ToAbsoluteFileName(FileName);
				return FileName;
			}
		}

		/// <summary>
		/// Contains the UTC timestamp. Describes when the last build of this file was
		/// </summary>
		protected long LastModified=0;
		public BuildAction Action=BuildAction.CopyToOutput;
		public BuildResult LastBuildResult { get; set; }

		/// <summary>
		/// Returns true if file was edited since the last build
		/// </summary>
		public bool Modified
		{
			get
			{
				return LastModified!=File.GetLastWriteTimeUtc(AbsoluteFileName).ToFileTime();
			}
		}

		public void ResetModifiedTime()
		{
			LastModified = File.GetLastWriteTimeUtc(AbsoluteFileName).ToFileTime();
		}

		public enum BuildAction
		{
			/// <summary>
			/// File will be disregarded when building
			/// </summary>
			None=0,
			/// <summary>
			/// File will be compiled and linked to the target file
			/// </summary>
			Compile=1,
			/// <summary>
			/// File will be copied to output directory
			/// </summary>
			CopyToOutput=2
		}
	}

	#region Errors
	public class GenericError
	{
		public enum ErrorType
		{
			Error=0,
			Warning=2,
			Info=3
		}

		[System.ComponentModel.DefaultValue(ErrorType.Error)]
		public ErrorType Type { get; set; }
		/// <summary>
		/// Can be null
		/// </summary>
		public Project Project { get; set; }
		public string FileName { get; set; }
		public string Message { get; set; }
		public CodeLocation Location { get; set; }

		public override string ToString()
		{
			return Message+ " ("+FileName+":"+Location.Line+")";
		}
	}

	public class BuildError:GenericError
	{
		public BuildError() { }
		public BuildError(string Message) { this.Message = Message; }
		public BuildError(string Message, string FileName, CodeLocation Location)
		{
			this.Message = Message;
			this.FileName = FileName;
			this.Location = Location;
		}
	}

	public class ParseError : GenericError
	{
		public readonly ParserError ParserError;

		public ParseError(ParserError err)
		{
			this.ParserError = err;

			if (err.IsSemantic)
				Type = ErrorType.Warning;
		}

		public new string Message
		{
			get { return ParserError.Message; }
		}

		public new CodeLocation Location
		{
			get { return new CodeLocation(ParserError.Location.Column,ParserError.Location.Line); }
		}

		public bool IsSemantic
		{
			get { return ParserError.IsSemantic; }
		}
	}
	#endregion

	public class ProjectVersion
	{
		public int Major { get; set; }
		public int Minor { get; set; }
		public int Build { get; set; }
		public int Revision { get; set; }

		public void IncrementBuild() { Build++; Revision = 0; }

		public ProjectVersion() { }
		public ProjectVersion(string versionString) { Parse(versionString); }
		public void Parse(string versionString)
		{
			var p = versionString.Split('.');
			if (p.Length > 3)
			{
				Major = Convert.ToInt32(p[0]);
				Minor = Convert.ToInt32(p[1]);
				Build = Convert.ToInt32(p[2]);
				Revision = Convert.ToInt32(p[3]);
			}
		}

		public override string ToString()
		{
			return Major.ToString() + "." + Minor.ToString() + "." + Build.ToString() + "." + Revision.ToString();
		}
	}
}
