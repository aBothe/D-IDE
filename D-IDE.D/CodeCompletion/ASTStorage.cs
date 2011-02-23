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

		public readonly List<ASTCollection> ParsedDictionaries = new List<ASTCollection>();
		/* Notes:
		 *  When a single, unbound module looks up files, it's allowed only to seek within the global files.
		 *  
		 *  When a project module tries to look up imports, it can use the global cache as well as 
		 */

		public void Remove(string Dict)
		{
			foreach(var c in ParsedDictionaries.ToArray())
				if (c.BaseDictionary == Dict)
					ParsedDictionaries.Remove(c);
		}

		public bool ContainsDictionary(string Dict)
		{
			foreach (var c in ParsedDictionaries)
				if (c.BaseDictionary == Dict)
					return true;
			return false;
		}

		public void ParseDictionary(string Dictionary)
		{
			foreach (var c in ParsedDictionaries)
				if (c.BaseDictionary == Dictionary)
				{
					c.Update();
					return;
				}

			if (!System.IO.Directory.Exists(Dictionary))
				throw new Exception("Cannot parse \""+Dictionary+"\". Directory does not exist!");

			var nc = new ASTCollection(Dictionary);
			ParsedDictionaries.Add(nc);

			nc.Update();
		}

		public IEnumerator<ASTCollection> GetEnumerator()
		{
			return ParsedDictionaries.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ParsedDictionaries.GetEnumerator();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Module"></param>
		/// <param name="ModuleSolution">Can be null. If so, only globally cached trees will be searched</param>
		/// <returns></returns>
		public AbstractSyntaxTree[] ResolveImports(AbstractSyntaxTree Module,Solution ModuleSolution)
		{
			//TODO: Do import resolution
			return null;
		}


	}

	public class ASTCollection:List<AbstractSyntaxTree>
	{
		public string BaseDictionary { get; set; }

		public ASTCollection() { }

		public ASTCollection(string baseDir)
		{
			BaseDictionary = baseDir;
		}

		/// <summary>
		/// Parse the base directory.
		/// </summary>
		public void Update()
		{
			Clear();

			//TODO: Scan the base directory for D sources (*.d?) -- .d as well as .di!
		}
	}
}
