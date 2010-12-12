using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.Windows;

namespace D_IDE.Core
{
	public interface IProject:IEnumerable<IModule>
	{
		string Name { get; set; }
		string FileName { get; set; }
		ISolution Solution { get; }

		bool Save();
		void Reload();
		void LoadFromFile(string FileName);

		Dictionary<ILanguage, IModule[]> ModulesByLanguage { get; }
		List<IModule> Modules { get; }
		string[] Files { get; }

		IModule this[string FileName] { get; set; }

		void Add(string FileName);
		void Remove(string FileName);
		void Rename(string OldFileName, string NewFileName);

		object ProjectIcon { get; }

		#region Build properties
		string BaseDirectory { get; }
		string OutputFile { get; }
		string OutputDirectory { get; }

		void BuildIncrementally();
		void Build();
		void CleanUpOutput();
		List<BuildError> LastBuildErrors { get; }
		#endregion

		void ShowProjectSettingsDialog();
	}

	public class BuildError
	{
		public readonly string Message;
		public readonly CodeLocation Location;
	}
}
