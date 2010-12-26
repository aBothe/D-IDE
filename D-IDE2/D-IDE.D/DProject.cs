using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using Parser.Core;

namespace D_IDE.D
{
	public class DProject : IProject
	{
		public string Name		{			get;			set;		}
		public string FileName		{			get;			set;		}
        public Solution Solution{get;      set;        }
		public readonly List<string> _Files = new List<string>();

		public DProject(SourceFileType ft)
		{
			ProjectType = ft;
		}

		public bool Save()
		{
			throw new NotImplementedException();
		}

		public void Reload()
		{
			throw new NotImplementedException();
		}

		public void LoadFromFile(string FileName)
		{
			throw new NotImplementedException();
		}

		public Dictionary<ILanguage, IModule[]> ModulesByLanguage
		{
			get { throw new NotImplementedException(); }
		}

		public List<IModule> ModuleCache
		{
			get { throw new NotImplementedException(); }
		}
		
		public string[] Files
		{
			get { return _Files.ToArray(); }
		}

		public IModule this[string FileName]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void Add(string FileName)
		{
			throw new NotImplementedException();
		}

		public void Remove(string FileName)
		{
			throw new NotImplementedException();
		}

		public void Rename(string OldFileName, string NewFileName)
		{
			throw new NotImplementedException();
		}

		public string OutputFile
		{
			get;
			set;
		}

		public string OutputDirectory
		{
			get;
			set;
		}

		public OutputTypes OutputType { get; set; }

		public string[] ExternalDependencies
		{
			set;
			get;
		}

		public void BuildIncrementally()
		{
			throw new NotImplementedException();
		}

		public void Build()
		{
			throw new NotImplementedException();
		}

		public void CleanUpOutput()
		{
			throw new NotImplementedException();
		}

		public List<BuildError> LastBuildErrors
		{
			get { throw new NotImplementedException(); }
		}

		public void ShowProjectSettingsDialog()
		{
			throw new NotImplementedException();
		}

		public IEnumerator<IModule> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}


		public SourceFileType ProjectType
		{
			get;
			set;
		}


		public IProject[] ProjectDependencies
		{
			get;
			set;
		}

		List<string> subdirs = new List<string>();
		public List<string> SubDirectories
		{
			get { return subdirs; }
		}
	}
}
