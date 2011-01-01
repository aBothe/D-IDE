using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public interface IAbstractSyntaxTree: IBlockNode
	{
		string FileName { get; set; }
		string ModuleName { get; set; }
		List<ParserError> ParseErrors { get; }
		Dictionary<ITypeDeclaration, bool> Imports { get; set; }
		bool ContainsImport(ITypeDeclaration ImportIdentifier);
	}

	public class ParserError
	{
		public readonly bool IsSemantic;
		public readonly string Message;
		public readonly int Token;
		public readonly CodeLocation Location;
	}

	public interface IBlockNode: INode, IEnumerable<INode>
	{
		CodeLocation BlockStartLocation { get; set; }
		INode[] Children { get; }

		void Add(INode Node);
		void AddRange(IEnumerable<INode> Nodes);
		int Count { get; }
		void Clear();

		INode this[int i] { get; set; }
		INode this[string Name] { get; set; }
	}

	public interface INode
	{
		string Name { get; set; }
		string Description { get; set; }
		ITypeDeclaration Type { get; set; }

		CodeLocation StartLocation { get; set; }
		CodeLocation EndLocation { get; set; }

		/// <summary>
		/// Assigns a node's properties
		/// </summary>
		/// <param name="Other"></param>
		void Assign(INode Other);

		INode Parent { get; set; }
		INode NodeRoot { get; set; }
	}

	public interface ITypeDeclaration
	{
		ITypeDeclaration Base { get; set; }
		string ToString();

		ITypeDeclaration MostBasic { get; set; }
	}
}
