using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public abstract class AbstractSourceModule:AbstractBlockNode, ISourceModule
	{
		string _FileName;
		Dictionary<ITypeDeclaration, bool> _Imports = new Dictionary<ITypeDeclaration, bool>();
		List<ParserError> LastParseErrors = new List<ParserError>();

		/// <summary>
		/// Name alias
		/// </summary>
		public string ModuleName
		{
			get { return Name; }
			set { Name = value; }
		}

		public string FileName
		{
			get
			{
				return _FileName;
			}
			set
			{
				_FileName = value;
			}
		}

		public List<ParserError> ParseErrors
		{
			get { return LastParseErrors; }
		}

		public Dictionary<ITypeDeclaration, bool> Imports
		{
			get
			{
				return _Imports;
			}
			set
			{
				_Imports = value;
			}
		}

		public bool ContainsImport(ITypeDeclaration type)
		{
			foreach (var kv in _Imports)
				if (kv.Key.ToString() == type.ToString())
					return true;
			return false;
		}
	}
}
