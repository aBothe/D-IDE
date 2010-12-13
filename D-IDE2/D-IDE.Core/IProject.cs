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
		SourceFileType ProjectType { get; }
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
		OutputTypes OutputType { get; }

		/// <summary>
		/// These files get copied into the output directory before compiling
		/// </summary>
		string[] ExternalDependencies { get; }
		IProject[] ProjectDependencies { get; }

		void BuildIncrementally();
		void Build();
		void CleanUpOutput();
		List<BuildError> LastBuildErrors { get; }
		#endregion

		void ShowProjectSettingsDialog();
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
		public readonly string FileName;
		/// <summary>
		/// Can be null if error occurs somewhere externally
		/// </summary>
		public readonly IModule Module;
		public readonly string Message;
		public readonly CodeLocation Location;
	}
}
