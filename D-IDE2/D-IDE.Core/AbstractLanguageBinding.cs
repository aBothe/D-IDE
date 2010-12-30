using Parser.Core;
using System.IO;
using DebugEngineWrapper;

namespace D_IDE.Core
{
	public abstract class AbstractLanguageBinding
	{
		#region Generic properties
		public abstract string LanguageName { get; }
		/// <summary>
		/// Should return a normal 32x32 image (WPF/GDI+) object
		/// </summary>
		public abstract object LanguageIcon { get; }

		public abstract object SmallProjectIcon { get; }
		public abstract object LargeProjectIcon { get; }

		/// <summary>
		/// File types and extensions supported by this language
		/// </summary>
		public abstract FileTemplate[] ModuleTemplates { get; }
		/// <summary>
		/// Project types and extensions supported by this language
		/// </summary>
		public abstract FileTemplate[] ProjectTemplates { get; }

		/// <summary>
		/// If true, D-IDE can create Language specific projects. 
		/// </summary>
		public abstract bool ProjectsSupported { get; }

		public abstract bool CanUseDebugging { get; }
		public abstract bool CanUseCodeCompletion { get; }
		public abstract bool CanBuild { get; }
		public abstract bool CanBuildToSingleModule { get; }
		#endregion

		/// <summary>
		/// Must not be null if <see cref="CanUseCodeCompletion"/> is set to true
		/// </summary>
		public abstract ILanguage Language { get; }

		public abstract Project CreateEmptyProject(FileTemplate ProjectType);
		public abstract Project OpenProject(string FileName);

		public abstract bool BuildProject(Project Project);
		public abstract BuildError[] BuildSingleModule(string FileName);


		public bool CanHandleProject(string ProjectFile)
		{
			var ext = Path.GetExtension(ProjectFile).ToLower();

			if (ProjectsSupported)
				foreach (var p in ProjectTemplates)
					if (p.Extensions != null)
						foreach (var e in p.Extensions)
							if (e.ToLower() == ext)
								return true;
			return false;
		}

		public bool CanHandleFile(string FileName)
		{
			var ext = Path.GetExtension(FileName).ToLower();

			foreach (var p in ModuleTemplates)
				if (p.Extensions != null)
					foreach (var e in p.Extensions)
						if (e.ToLower() == ext)
							return true;
			return false;
		}

		/// <summary>
		/// Retrieves the value of a debug symbol.
		/// Debugging requires CodeCompletion to enable better node search.
		/// Only called if CanUseDebugging is true.
		/// </summary>
		/// <param name="ModuleTree"></param>
		/// <param name="ScopedSrcLine"></param>
		/// <param name="sym"></param>
		/// <returns></returns>
		public abstract string BuildSymbolValueString(
			Parser.Core.AbstractSyntaxTree ModuleTree, 
			uint ScopedSrcLine, 
			DebugScopedSymbol sym);
	}

	public class FileTemplate
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string[] Extensions { get; set; }

		/// <summary>
		/// For project/file creation purposes; Can be null
		/// </summary>
		public string DefaultFilePrefix { get; set; }

		public object LargeImage { get; set; }
		public object SmallImage { get; set; }
	}
}
