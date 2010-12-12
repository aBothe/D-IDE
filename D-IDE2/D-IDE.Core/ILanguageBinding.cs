using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	public interface ILanguageBinding
	{
		/// <summary>
		/// This is the very basic interface which lets a ILanguageBinding interact with D-IDE.
		/// </summary>
		IDEInterface IDE { get; set; }

		#region Generic properties
		string LanguageName { get; }
		/// <summary>
		/// File types and extensions supported by this language
		/// </summary>
		SourceFileType[] ModuleTypes { get; }
		SourceFileType[] ProjectTypes { get; }

		/// <summary>
		/// If true, D-IDE can create Language specific projects. 
		/// Otherwise only modules are allowed to be created
		/// </summary>
		bool ProjectsSupported { get; }
		bool CanUseDebugging { get; }
		bool CanUseCodeCompletion { get; }
		bool CanBuild { get; }
		bool CanBuildToSingleModule { get; }
		#endregion

		/// <summary>
		/// Must not be null if <see cref="CanUseCodeCompletion"/> is set to true
		/// </summary>
		ILanguage Language { get; }

		/// <summary>
		/// Used for outline and for code completion features.
		/// Returns an icon or image that indicates a specific node type.
		/// Only called if <see cref="CanUseCodeCompletion"/> is set to true
		/// </summary>
		/// <param name="Node"></param>
		/// <returns></returns>
		object GetNodeIcon(INode Node);


		IProject CreateEmptyProject(SourceFileType ProjectType);
		IProject OpenProject(string FileName);

		IModule CreateEmptyModule(SourceFileType FileType);
		IModule OpenModule(string FileName);

		IDebugProvider DebugProvider { get; }
	}

	public class SourceFileType
	{
		public string Name;
		public string Description;
		public string[] Extensions;

		public object LargeImage;
		public object SmallImage;
	}
}
