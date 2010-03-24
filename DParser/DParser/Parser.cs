using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser;

namespace D_Parser
{
	class Parser : IParser
	{
		DParser mp;
		public DataType dom;
		public List<string> imports;

		public Parser(DLexer lexer)
		{
			System.Windows.Forms.MessageBox.Show("Parser");
			mp = new DParser(lexer);
		}
		#region IParser Member

		public CompilationUnit CompilationUnit
		{
			get {
				if (dom == null) Parse();
				CompilationUnit cu = new CompilationUnit();
				cu.StartLocation = dom.StartLocation;
				cu.Children = dom.Children;
				cu.EndLocation = dom.EndLocation;
				cu.UserData = dom;
				return cu;
			}
		}

		ICSharpCode.NRefactory.Parser.Errors IParser.Errors
		{
			get { return mp.errors; }
		}

		public ILexer Lexer
		{
			get { return mp.lexer; }
		}

		public void Parse()
		{
			dom=mp.Parse("",out imports);
		}

		public BlockStatement ParseBlock()
		{
			BlockStatement bs = new BlockStatement();
			bs.StartLocation = dom.StartLocation;
			bs.Children = dom.Children;
			bs.EndLocation = dom.EndLocation;
			bs.UserData = dom;
			return bs;
		}

		public Expression ParseExpression()
		{
			return Expression.Null;
		}

		bool IParser.ParseMethodBodies
		{
			get
			{
				return true;
			}
			set
			{
				
			}
		}

		List<INode> IParser.ParseTypeMembers()
		{
			return dom.Children;
		}

		#endregion

		#region IDisposable Member

		void IDisposable.Dispose()
		{
			dom = null;
			mp = null;
			imports.Clear();
		}

		#endregion

		#region IParser Member


		public TypeReference ParseTypeReference()
		{
			return new TypeReference("System.Object");
		}

		#endregion
	}
}
