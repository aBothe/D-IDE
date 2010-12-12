using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;

namespace D_IDE.D
{
	public class DModule:IModule
	{
		public string FileName
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

		public IProject Project
		{
			get { throw new NotImplementedException(); }
		}

		public ILanguageBinding LanguageBinding
		{
			get { throw new NotImplementedException(); }
		}

		public Parser.Core.ISourceModule CodeNode
		{
			get { throw new NotImplementedException(); }
		}

		public void Refresh()
		{
			throw new NotImplementedException();
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

		public void BuildIncrementally()
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


		public SourceFileType FileType
		{
			get { throw new NotImplementedException(); }
		}
	}
}
