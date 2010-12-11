using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	public interface IProject:IEnumerable<IModule>
	{
		string Name { get; set; }
		string FileName { get; set; }
		ISolution Solution { get; }
		List<IProject> DependentProjects { get; }

		bool Save();
		void LoadFromFile(string FileName);

		Dictionary<ILanguage, IModule[]> ModulesByLanguage { get; }
		List<IModule> Modules { get; }
		string[] Files { get; }

		IModule this[string FileName] { get; set; }

		void Add(string FileName);
		void Remove(string FileName);
		void Rename(string OldFileName, string NewFileName);

		#region Build properties & methods
		string BaseDirectory { get; set; }
		string OutputFile { get; set; }
		string OutputDirectory { get; set; }

		void Build();
		void BuildIncrementally();
		void CleanUpOutputDirectory();
		#endregion
	}
}
