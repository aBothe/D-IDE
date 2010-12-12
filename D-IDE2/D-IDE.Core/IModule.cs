using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	public interface IModule
	{
		string FileName { get; set; }
		SourceFileType FileType { get; }

		/// <summary>
		/// Can be null if it's a stand-alone module
		/// </summary>
		IProject Project { get; }
		/// <summary>
		/// Must not be null.
		/// </summary>
		ILanguageBinding LanguageBinding { get; }
		/// <summary>
		/// Can be null if <see cref="LanguageBinding"/> negates CodeCompletion
		/// </summary>
		ISourceModule CodeNode { get; }

		/// <summary>
		/// Chance for the module to reparse its code structure.
		/// </summary>
		void Refresh();

		#region Build stuff
		string OutputFile { get; set; }
		bool WasBuiltSuccessfully { get; }

		void Build();
		void BuildIncrementally();
		void CleanUpOutput();

		List<BuildError> LastBuildErrors { get; }
		#endregion
	}
}
