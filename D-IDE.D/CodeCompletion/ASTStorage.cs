using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parser.Core;
using D_IDE.Core;

namespace D_IDE.D.CodeCompletion
{
	/// <summary>
	/// Class which is responsible for caching all parsed D source files.
	/// There are two cache types:
	///		- Locally and
	///		- Globally cached ASTs
	///	While the local ASTs won't be saved when D-IDE exits, the global sources will be stored permanently to grant access to them everytime.
	/// </summary>
	public class ASTStorage:IEnumerable<ASTCollection>
	{
		public static ASTStorage Instance = new ASTStorage();

		public readonly List<ASTCollection> ParsedGlobalDictionaries = new List<ASTCollection>();
		/* Notes:
		 *  When a single, unbound module looks up files, it's allowed only to seek within the global files.
		 *  
		 *  When a project module tries to look up imports, it can use the global cache as well as 
		 */

		public void Remove(string Dict)
		{
			foreach(var c in ParsedGlobalDictionaries.ToArray())
				if (c.BaseDictionary == Dict)
					ParsedGlobalDictionaries.Remove(c);
		}

		public bool ContainsDictionary(string Dict)
		{
			foreach (var c in ParsedGlobalDictionaries)
				if (c.BaseDictionary == Dict)
					return true;
			return false;
		}

		public void ParseDictionary(string Dictionary)
		{
			foreach (var c in ParsedGlobalDictionaries)
				if (c.BaseDictionary == Dictionary)
				{
					c.UpdateFromBaseDirectory();
					return;
				}

			if (!System.IO.Directory.Exists(Dictionary))
				throw new Exception("Cannot parse \""+Dictionary+"\". Directory does not exist!");

			var nc = new ASTCollection(Dictionary);
			ParsedGlobalDictionaries.Add(nc);

			nc.UpdateFromBaseDirectory();
		}

		public IEnumerator<ASTCollection> GetEnumerator()
		{
			return ParsedGlobalDictionaries.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ParsedGlobalDictionaries.GetEnumerator();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Module"></param>
		/// <param name="ModuleSolution">Can be null. If so, only globally cached trees will be searched</param>
		/// <returns></returns>
		public IAbstractSyntaxTree[] ResolveImports(IAbstractSyntaxTree Module,Solution ModuleSolution)
		{
			//TODO: Do import resolution
			return null;
		}


	}

	public class ASTCollection:List<IAbstractSyntaxTree>
	{
		public string BaseDictionary { get; set; }

		public ASTCollection() { }

		public ASTCollection(string baseDir)
		{
			BaseDictionary = baseDir;
		}

		public void Remove(string file,bool ByModuleName)
		{
			foreach (var c in ToArray())
				if (ByModuleName ? c.ModuleName == file : c.FileName == file)
				{
					Remove(c);
					return;
				}
		}

		public bool ContainsDictionary(string file,bool ByModuleName)
		{
			foreach (var c in ToArray())
				if (ByModuleName ? c.ModuleName == file : c.FileName == file)
					return true;
			return false;
		}
		
		public new void Add(IAbstractSyntaxTree tree)
		{
			if (tree == null)
				return;

			Remove(tree.FileName, false);
			base.Add(tree);
		}

		public IAbstractSyntaxTree this[string file]
		{
			get { return this[file, false]; }
			set { this[file, false] = value; }
		}

		public IAbstractSyntaxTree this[string AbsoluteFileName,bool ByModuleName]
		{
			get{
				foreach (var ast in this)
					if (ByModuleName ? ast.ModuleName == AbsoluteFileName : ast.FileName == AbsoluteFileName)
						return ast;
				return null;
			}
			set
			{
				Remove(AbsoluteFileName, ByModuleName);
				base.Add(value);
			}
		}

		/// <summary>
		/// Parse the base directory.
		/// </summary>
		public void UpdateFromBaseDirectory()
		{
			Clear();

			//TODO: Scan the base directory for D sources (*.d?) -- .d as well as .di!
		}
	}
}
