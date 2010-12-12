using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parser.Core;
using System.Windows;

namespace D_IDE.Core
{
	public interface ILanguageBinding
	{
		#region Generic properties
		string LanguageName { get; }
		string[] LanguageExtensions { get; }

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
		/// Icon getter for projects. Can return null if <see cref="ProjectsSupported"/> is set to false.
		/// Should return an Icon or Image object.
		/// </summary>
		/// <returns></returns>
		object GetProjectIcon();
		object GetProjectIcon(IProject Project);

		/// <summary>
		/// Get standard module icon.
		/// </summary>
		/// <returns></returns>
		object GetModuleIcon();
		object GetModuleIcon(IModule Module);

		/// <summary>
		/// Used for outline and for code completion features.
		/// Returns an icon or image that indicates a specific node type.
		/// Only called if <see cref="CanUseCodeCompletion"/> is set to true
		/// </summary>
		/// <param name="Node"></param>
		/// <returns></returns>
		object GetNodeIcon(INode Node);


		IProject CreateEmptyProject();
		IProject OpenProject(string FileName);

		IModule CreateEmptyModule();
		IModule OpenModule(string FileName);

		IDebugProvider DebugProvider { get; }
	}
}
