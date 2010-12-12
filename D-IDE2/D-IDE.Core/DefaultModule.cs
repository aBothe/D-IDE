using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parser.Core;

namespace D_IDE.Core
{
	/// <summary>
	/// Neutral module that where all its extra features like Debugging, building or code completion are inactive
	/// </summary>
	public class DefaultModule:IModule
	{
		string _FileName="";
		IProject _Project;

		public string FileName
		{
			get			{				return _FileName;			}
			set			{				_FileName = value;			}
		}

		public DefaultModule() { }
		public DefaultModule(IProject Project) { _Project = Project; }
		public DefaultModule(string File) { _FileName = File; }
		public DefaultModule(string File, IProject Project) { _FileName = File; _Project = Project; }

		public IProject Project	{	get { return _Project; }	}
		public ILanguage Language	{			get { return null; }		}
		public bool CanUseDebugging		{			get { return false; }		}
		public bool CanUseCodeCompletion		{			get { return false; }		}
		public bool CanBuild		{			get { return false; }		}
		public bool CanBuildToSingleModule		{			get { return false; }		}

		/// <summary>
		/// Since code completion isn't activated in the DefaultModule, do nothing
		/// </summary>
		public void Refresh(){}

		public void BuildIncrementally() { }

		public ISourceModule CodeNode		{ get { return null; }	}

		public ILanguageBinding LanguageBinding
		{
			get { throw new NotImplementedException(); }
		}

		public string OutputFile
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

		public bool WasBuiltSuccessfully
		{
			get { throw new NotImplementedException(); }
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
	}
}
