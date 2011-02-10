using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.IO;

namespace D_IDE.Core
{
	public abstract class AbstractModule
	{
		public string FileName { get; set; }
		/// <summary>
		/// The module's directory
		/// </summary>
		public string FileDirectory
		{
			get { return Path.GetDirectoryName(FileName); }
			set { FileName = value + Path.GetFileName(FileName); }
		}
		public bool IsAbsoluteFileName { get { return Path.IsPathRooted(FileName); } }

		public SourceFileType FileType { get; protected set; }

		/// <summary>
		/// Can be null if it's a stand-alone module
		/// </summary>
		public AbstractProject Project { get; set; }
		
		/// <summary>
		/// Must not be null.
		/// </summary>
		public readonly AbstractLanguageBinding LanguageBinding;

		/// <summary>
		/// Can be null if <see cref="LanguageBinding"/> negates CodeCompletion
		/// </summary>
		public IAbstractSyntaxTree CodeNode { get;protected set; }

		/// <summary>
		/// Chance for the module to reparse its code structure.
		/// </summary>
		public abstract void Refresh();

		public abstract string OutputFile { get; set; }
		public abstract bool WasBuiltSuccessfully { get; protected set; }

		public abstract void Build();
		public abstract void BuildIncrementally();
		public abstract void CleanUpOutput();

		protected readonly List<BuildError> _LastBuildErrors = new List<BuildError>();
		public BuildError[] LastBuildErrors { get { return _LastBuildErrors.ToArray(); } }
	}
}
