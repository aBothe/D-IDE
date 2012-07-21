using System;
using System.IO;


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

		#region Settings
		public virtual bool CanUseSettings { get { return false; } }
		public virtual void SaveSettings(string SuggestedFileName) { throw new NotImplementedException(); } 
		public virtual void LoadSettings(string SuggestedFileName) { throw new NotImplementedException(); } 
		public virtual AbstractSettingsPage SettingsPage { get { throw new NotImplementedException(); } }
		#endregion

		public virtual object SmallProjectIcon { get { throw new NotImplementedException(); } }
		public virtual object LargeProjectIcon { get { throw new NotImplementedException(); } }

		/// <summary>
		/// File types and extensions supported by this binding
		/// </summary>
		public abstract FileTemplate[] ModuleTemplates { get; }
		/// <summary>
		/// Project types and extensions supported by this binding. Can be null if projects not supported
		/// </summary>
		public virtual FileTemplate[] ProjectTemplates { get { throw new NotImplementedException(); } }

		/// <summary>
		/// If true, D-IDE can create Language specific projects. 
		/// </summary>
		public virtual bool ProjectsSupported { get { return false; } }

		public virtual bool CanUseDebugging { get { return false; } }
		public virtual bool CanBuild { get { return false; } }

		public static AbstractLanguageBinding SearchBinding(string file, out bool IsProject)
		{
			IsProject = false;
			foreach (var lang in LanguageLoader.Bindings)
				if ((lang.ProjectsSupported && (IsProject=lang.CanHandleProject(file))) || lang.CanHandleFile(file))
					return lang;
			return null;
		}
		#endregion

		public virtual Project CreateEmptyProject(string Name, string ProjectFile,FileTemplate ProjectType) { throw new NotImplementedException(); } 
		public virtual Project OpenProject(Solution Solution, string FileName) {  throw new NotImplementedException(); }

		public bool CanHandleProject(string ProjectFile)
		{
			if (string.IsNullOrEmpty(ProjectFile))
				return false;

			var ext = Path.GetExtension(ProjectFile).ToLower();

			if (ProjectsSupported)
				foreach (var p in ProjectTemplates)
					if (p.Extensions != null)
						foreach (var e in p.Extensions)
							if (e.ToLower() == ext)
								return true;
			return false;
		}

		/// <summary>
		/// Returns if project can build the file named 'FileName'
		/// </summary>
		public bool CanHandleFile(string FileName)
		{
			if (string.IsNullOrEmpty(FileName))
				return false;

			var ext = Path.GetExtension(FileName).ToLower();

			foreach (var p in ModuleTemplates)
				if (p.Extensions != null)
					foreach (var e in p.Extensions)
						if (e.ToLower() == ext)
							return true;
			return false;
		}

		#region Editor support
		public virtual bool SupportsEditor(string SourceFile)
		{
			return false;
		}

		public virtual EditorDocument OpenFile(Project Project, string SourceFile) {
			return new EditorDocument(SourceFile); 
		}
		#endregion

		public static string CreateSettingsFileName(AbstractLanguageBinding Binding)
		{
			return IDEInterface.ConfigDirectory + "\\" + Util.PurifyFileName(Binding.LanguageName) + ".config.xml";
		}

		/// <summary>
		/// Language build support. Only requested if CanBuild returns true.
		/// </summary>
		public virtual AbstractBuildSupport BuildSupport { get { return null; } }

		/// <summary>
		/// Debug symbol resolving suppport. Only requested if CanUseDebugging is set to true
		/// </summary>
		public virtual GenericDebugSupport DebugSupport { get { return null; } }
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
