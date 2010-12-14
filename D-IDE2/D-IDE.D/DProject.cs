using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;

namespace D_IDE.D
{
	public class DProject : IProject
	{
		public string Name
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

		public string FileName
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

		public ISolution Solution
		{
			get { throw new NotImplementedException(); }
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

		public Dictionary<Parser.Core.ILanguage, IModule[]> ModulesByLanguage
		{
			get { throw new NotImplementedException(); }
		}

		public List<IModule> Modules
		{
			get { throw new NotImplementedException(); }
		}

		public string[] Files
		{
			get { throw new NotImplementedException(); }
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

		public object ProjectIcon
		{
			get { throw new NotImplementedException(); }
		}

		public string BaseDirectory
		{
			get { throw new NotImplementedException(); }
		}

		public string OutputFile
		{
			get { throw new NotImplementedException(); }
		}

		public string OutputDirectory
		{
			get { throw new NotImplementedException(); }
		}

		public OutputTypes OutputType
		{
			get { throw new NotImplementedException(); }
		}

		public string[] ExternalDependencies
		{
			get { throw new NotImplementedException(); }
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
			get { throw new NotImplementedException(); }
		}


		public IProject[] ProjectDependencies
		{
			get { throw new NotImplementedException(); }
		}
	}
}
