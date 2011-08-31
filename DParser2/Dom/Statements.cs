using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;

namespace D_Parser.Dom.Statements
{
	#region Generics
	public interface IStatement
	{
		CodeLocation StartLocation { get; set; }
		CodeLocation EndLocation { get; set; }
		IStatement Parent { get; set; }
		INode ParentNode { get; set; }

		string ToCode();
	}

	public abstract class AbstractStatement:IStatement
	{
		public virtual CodeLocation StartLocation { get; set; }
		public virtual CodeLocation EndLocation { get; set; }
		public virtual IStatement Parent { get; set; }

		INode parent;
		public virtual INode ParentNode {
			get
			{
				if(Parent!=null)
					return Parent.ParentNode;
				return parent;
			}
			set
			{
				if (Parent != null)
					Parent.ParentNode = value;
				parent = value;
			}
		}

		public abstract string ToCode();

		public override string ToString()
		{
			return ToCode();
		}
	}

	/// <summary>
	/// Represents a statement that can contain other statements, which become scoped under that containing statement.
	/// So, most statements with a ScopeStatement-appendix are inherit this abstract class.
	/// </summary>
	public abstract class ScopingStatement : AbstractStatement
	{
		public virtual IStatement ScopedStatement { get; set; }
	}
	#endregion

	public class BlockStatement : AbstractStatement, IEnumerable<IStatement>
	{
		readonly List<IStatement> _Statements = new List<IStatement>();

		public IEnumerator<IStatement>  GetEnumerator()
		{
 			return _Statements.GetEnumerator();
		}

		System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
		{
 			return _Statements.GetEnumerator();
		}

		public override string ToCode()
		{
			var ret = "{"+Environment.NewLine;

			foreach (var s in _Statements)
				ret += s.ToCode()+Environment.NewLine;

			return ret + "}";
		}

		public void Add(IStatement s)
		{
			if (s == null)
				return;
			s.Parent = this;
			_Statements.Add(s);
		}

		public List<IStatement> Statements
		{
			get { return _Statements; }
		}

		public INode[] Declarations
		{
			get
			{
				var l = new List<INode>();

				foreach (var s in _Statements)
					if (s is DeclarationStatement && (s as DeclarationStatement).Declaration != null)
							l.AddRange((s as DeclarationStatement).Declaration);

				return l.ToArray();
			}
		}

		/// <summary>
		/// Walks up the statement scope hierarchy and enlists all declarations that have been made BEFORE the caret position. 
		/// (If CodeLocation.Empty given, this parameter will be ignored)
		/// </summary>
		public static INode[] GetItemHierarchy(IStatement Statement, CodeLocation Caret)
		{
			var l = new List<INode>();

			var curScope=Statement as BlockStatement;

			if (curScope == null && Statement is ScopingStatement)
			{
				var curPar = Statement;
				while (curPar is ScopingStatement)
					curPar = (curPar as ScopingStatement).Parent;
				curScope = curPar as BlockStatement;
			}

			while (curScope != null)
			{
				foreach (var i in curScope)
				{
					// if i doesn't contain a Declaration OR is located after the caret's location, disregard it
					if (Caret!=CodeLocation.Empty?(i.StartLocation > Caret):false)
						continue;

					if(i is DeclarationStatement && (i as DeclarationStatement).Declaration!=null)
						l.AddRange((i as DeclarationStatement).Declaration);
				}

				var curPar=curScope.Parent;
				while (curPar is ScopingStatement)
					curPar = (curPar as ScopingStatement).Parent;

				curScope = curPar as BlockStatement;
			}

			return l.ToArray();
		}

		public virtual IStatement SearchStatement(CodeLocation Where)
		{
			return SearchBlockStatement(this, Where);
		}

		/// <summary>
		/// Scans the current scope. If a scoping statement was found, also these ones get searched then recursively.
		/// </summary>
		/// <param name="Where"></param>
		/// <returns></returns>
		public IStatement SearchStatementDeeply(CodeLocation Where)
		{
			var s = SearchStatement(Where);

			while (s!=null)
			{
				if (s is BlockStatement)
				{
					var s2 = (s as BlockStatement).SearchStatement(Where);

					if (s == s2)
						break;

					if (s2 != null)
						s = s2;
				}
				else if (s is ScopingStatement)
				{
					var s2 = (s as ScopingStatement).ScopedStatement;

					if (s2 != null && Where >= s2.StartLocation && Where <= s2.EndLocation)
						s = s2;
					else 
						break;
				}
				else break;
			}

			return s;
		}

		public static IStatement SearchBlockStatement(BlockStatement BlockStmt, CodeLocation Where)
		{
			// First check if one sub-statement is located at the code location
			foreach (var s in BlockStmt._Statements)
				if (Where >= s.StartLocation && Where <= s.EndLocation)
					return s;

			// If nothing was found, check if this statement fits to the coordinates
			if (Where >= BlockStmt.StartLocation && Where <= BlockStmt.EndLocation)
				return BlockStmt;

			// If not, return null
			return null;
		}
	}

	public class LabeledStatement : AbstractStatement
	{
		public string Identifier;

		public override string ToCode()
		{
			return Identifier + ":";
		}
	}

	public class IfStatement : ScopingStatement
	{
		public bool IsStatic = false;
		public IExpression IfCondition;
		public DVariable IfVariable;

		public IStatement ThenStatement
		{
			get { return ScopedStatement; }
			set { ScopedStatement = value; }
		}
		public IStatement ElseStatement;

		public override CodeLocation EndLocation
		{
			get
			{
				if (ScopedStatement == null)
					return base.EndLocation;
				return ElseStatement!=null?ElseStatement.EndLocation:ScopedStatement. EndLocation;
			}
			set
			{
				if (ScopedStatement == null)
					base.EndLocation = value;
			}
		}

		public override string ToCode()
		{
			var ret = (IsStatic?"static ":"")+ "if(";

			if (IfCondition != null)
				ret += IfCondition.ToString();

			ret += ")"+Environment.NewLine;

			if (ScopedStatement != null)
				ret += ScopedStatement. ToCode();

			if (ElseStatement != null)
				ret += Environment.NewLine + "else " + ElseStatement.ToCode();

			return ret;
		}
	}

	public class WhileStatement : ScopingStatement
	{
		public IExpression Condition;

		public override CodeLocation EndLocation
		{
			get
			{
				if (ScopedStatement == null)
					return base.EndLocation;
				return ScopedStatement.EndLocation;
			}
			set
			{
				if (ScopedStatement == null)
					base.EndLocation = value;
			}
		}

		public override string ToCode()
		{
			var ret= "while(";

			if (Condition != null)
				ret += Condition.ToString();

			ret += ") "+Environment.NewLine;

			if (ScopedStatement != null)
				ret += ScopedStatement.ToCode();

			return ret;
		}
	}

	public class ForStatement : ScopingStatement
	{
		public IStatement Initialize;
		public IExpression Test;
		public IExpression Increment;

		public override string ToCode()
		{
			var ret = "for(";

			if (Initialize != null)
				ret += Initialize.ToCode();

			ret+=';';

			if (Test != null)
				ret += Test.ToString();

			ret += ';';

			if (Increment != null)
				ret += Increment.ToString();

			ret += ')';

			if (ScopedStatement != null)
				ret += ' '+ScopedStatement.ToCode();

			return ret;
		}
	}

	public class ForeachStatement : ScopingStatement
	{
		public bool IsRangeStatement
		{
			get { return UpperAggregate != null; }
		}
		public bool IsReverse = false;
		public DVariable[] ForeachTypeList;
		public IExpression Aggregate;

		/// <summary>
		/// Used in ForeachRangeStatements. The Aggregate field will be the lower expression then.
		/// </summary>
		public IExpression UpperAggregate;

		public override string ToCode()
		{
			var ret=(IsReverse?"foreach_reverse":"foreach")+'(';

			foreach (var v in ForeachTypeList)
				ret += v.ToString() + ',';

			ret=ret.TrimEnd(',')+';';

			if (Aggregate != null)
				ret += Aggregate.ToString();

			if (IsRangeStatement)
				ret += ".." + UpperAggregate.ToString();

			ret += ')';

			if (ScopedStatement != null)
				ret += ' ' + ScopedStatement.ToCode();

			return ret;
		}
	}

	public class SwitchStatement : ScopingStatement
	{
		public bool IsFinal;
		public IExpression SwitchExpression;

		public override string ToCode()
		{
			var ret = "switch(";

			if (SwitchExpression != null)
				ret += SwitchExpression.ToString();

			ret+=')';

			if (ScopedStatement != null)
				ret += ' '+ScopedStatement.ToCode();

			return ret;
		}

		public class CaseStatement : AbstractStatement
		{
			public bool IsCaseRange
			{
				get { return LastExpression != null; }
			}

			public IExpression ArgumentList;

			/// <summary>
			/// Used for CaseRangeStatements
			/// </summary>
			public IExpression LastExpression;

			public IStatement[] ScopeStatementList;

			public override string ToCode()
			{
				var ret= "case "+ArgumentList.ToString()+':' + (IsCaseRange?(" .. case "+LastExpression.ToString()+':'):"")+Environment.NewLine;

				foreach (var s in ScopeStatementList)
					ret += s.ToCode()+Environment.NewLine;

				return ret;
			}
		}

		public class DefaultStatement : AbstractStatement
		{
			public IStatement[] ScopeStatementList;

			public override string ToCode()
			{
				var ret = "default:"+Environment.NewLine;

				foreach (var s in ScopeStatementList)
					ret += s.ToCode() + Environment.NewLine;

				return ret;
			}
		}
	}

	public class ContinueStatement : AbstractStatement
	{
		public string Identifier;

		public override string ToCode()
		{
			return "continue"+(string.IsNullOrEmpty(Identifier)?"":(' '+Identifier))+';';
		}
	}

	public class BreakStatement : AbstractStatement
	{
		public string Identifier;

		public override string ToCode()
		{
			return "break" + (string.IsNullOrEmpty(Identifier) ? "" : (' ' + Identifier)) + ';';
		}
	}

	public class ReturnStatement : AbstractStatement
	{
		public IExpression ReturnExpression;

		public override string ToCode()
		{
			return "return" + (ReturnExpression==null ? "" : (' ' + ReturnExpression.ToString())) + ';';
		}
	}

	public class GotoStatement : AbstractStatement
	{
		public enum GotoStmtType
		{
			Identifier=DTokens.Identifier,
			Case=DTokens.Case,
			Default=DTokens.Default
		}

		public string LabelIdentifier;
		public IExpression CaseExpression;
		public GotoStmtType StmtType = GotoStmtType.Identifier;

		public override string ToCode()
		{
			switch (StmtType)
			{
				case GotoStmtType.Identifier:
					return "goto " + LabelIdentifier+';';
				case GotoStmtType.Default:
					return "goto default;";
				case GotoStmtType.Case:
					return "goto"+(CaseExpression==null?"":(' '+CaseExpression.ToString()))+';';
			}

			return null;
		}
	}

	public class WithStatement : ScopingStatement
	{
		public IExpression WithExpression;
		public ITypeDeclaration WithSymbol;

		public override string ToCode()
		{
			var ret = "with(";

			if (WithExpression != null)
				ret += WithExpression.ToString();
			else if (WithSymbol != null)
				ret += WithSymbol.ToString();

			ret += ')';

			if (ScopedStatement != null)
				ret += ScopedStatement.ToCode();

			return ret;
		}
	}

	public class SynchronizedStatement : ScopingStatement
	{
		public IExpression SyncExpression;

		public override string ToCode()
		{
			var ret="synchronized";

			if (SyncExpression != null)
				ret += '(' + SyncExpression.ToString() + ')';

			if (ScopedStatement != null)
				ret += ' ' + ScopedStatement.ToCode();

			return ret;
		}
	}

	public class TryStatement : ScopingStatement
	{
		public CatchStatement[] Catches;
		public FinallyStatement FinallyStmt;

		public override string ToCode()
		{
			var ret= "try " + (ScopedStatement!=null? (' '+ScopedStatement.ToCode()):"");

			if (Catches != null && Catches.Length > 0)
				foreach (var c in Catches)
					ret += Environment.NewLine + c.ToCode();

			if (FinallyStmt != null)
				ret += Environment.NewLine + FinallyStmt.ToCode();

			return ret;
		}

		public class CatchStatement : ScopingStatement
		{
			public DVariable CatchParameter;

			public override string ToCode()
			{
				return "catch" + (CatchParameter != null ? ('(' + CatchParameter.ToString() + ')') : "")
					+ (ScopedStatement != null ? (' ' + ScopedStatement.ToCode()) : "");
			}
		}

		public class FinallyStatement : ScopingStatement
		{
			public override string ToCode()
			{
				return "finally" + (ScopedStatement != null ? (' ' + ScopedStatement.ToCode()) : "");
			}
		}
	}

	public class ThrowStatement : AbstractStatement
	{
		public IExpression ThrowExpression;

		public override string ToCode()
		{
			return "throw" + (ThrowExpression==null ? "" : (' ' + ThrowExpression.ToString())) + ';';
		}
	}

	public class ScopeGuardStatement : ScopingStatement
	{
		public const string ExitScope = "exit";
		public const string SuccessScope = "success";
		public const string FailureScope = "failure";

		public string GuardedScope=ExitScope;

		public override string ToCode()
		{
			return "scope("+GuardedScope+')'+ (ScopedStatement==null?"":ScopedStatement.ToCode());
		}
	}

	public class AsmStatement : AbstractStatement
	{
		public string[] Instructions;

		public override string ToCode()
		{
			var ret = "asm {";

			if (Instructions != null && Instructions.Length > 0)
			{
				foreach (var i in Instructions)
					ret += Environment.NewLine + i + ';';
				ret += Environment.NewLine;
			}

			return ret+'}';
		}
	}

	public class PragmaStatement : ScopingStatement
	{
		public string PragmaIdentifier;
		public IExpression[] ArgumentList;

		public override string ToCode()
		{
			var ret = "pragma(" + PragmaIdentifier;

			if (ArgumentList != null && ArgumentList.Length > 0)
				foreach (var arg in ArgumentList)
					ret += ',' + arg.ToString();

			ret+=')';

			if (ScopedStatement != null)
				ret += ' '+ScopedStatement.ToCode();
			else
				ret+=';'; // An empty pragma is possible

			return ret;
		}
	}

	public class MixinStatement : AbstractStatement
	{
		public IExpression MixinExpression;

		public override string ToCode()
		{
			return "mixin("+(MixinExpression==null?"":MixinExpression.ToString())+");";
		}
	}

	public abstract class ConditionStatement : ScopingStatement
	{
		public IStatement ElseStatement;

		

		public class DebugStatement : ConditionStatement
		{
			public object DebugIdentifierOrLiteral;
			public override string ToCode()
			{
				var ret = "debug";

				if(DebugIdentifierOrLiteral!=null)
					ret+='('+DebugIdentifierOrLiteral.ToString()+')';

				if (ScopedStatement != null)
					ret += ' ' + ScopedStatement.ToCode();

				if (ElseStatement != null)
					ret += " else " + ElseStatement.ToCode();

				return ret;
			}
		}

		public class VersionStatement : ConditionStatement
		{
			public object VersionIdentifierOrLiteral;
			public override string ToCode()
			{
				var ret = "version";

				if (VersionIdentifierOrLiteral != null)
					ret += '(' + VersionIdentifierOrLiteral.ToString() + ')';

				if (ScopedStatement != null)
					ret += ' ' + ScopedStatement.ToCode();

				if (ElseStatement != null)
					ret += " else " + ElseStatement.ToCode();

				return ret;
			}
		}
	}

	public class AssertStatement : AbstractStatement
	{
		public bool IsStatic = false;
		public IExpression AssertExpression;

		public override string ToCode()
		{
			return (IsStatic?"static ":"")+"assert("+(AssertExpression!=null?AssertExpression.ToString():"")+");";
		}
	}

	public class VolatileStatement : ScopingStatement
	{
		public override string ToCode()
		{
			return "volatile "+ScopedStatement==null?"":ScopedStatement.ToCode();
		}
	}

	public class ExpressionStatement : AbstractStatement
	{
		public IExpression Expression;

		public override string ToCode()
		{
			return Expression.ToString()+';';
		}
	}

	public class DeclarationStatement : AbstractStatement
	{
		/// <summary>
		/// Declarations done by this statement. Contains more than one item e.g. on int a,b,c;
		/// </summary>
		public INode[] Declaration;

		public override string ToCode()
		{
			if (Declaration == null || Declaration.Length < 0)
				return ";";

			var r = Declaration[0].ToString();

			for (int i = 1; i < Declaration.Length; i++)
			{
				var d = Declaration[i];
				r += ',' + d.Name;

				var dv=d as DVariable;
				if (dv != null && dv.Initializer != null)
					r += '=' + dv.Initializer.ToString();
			}

			return r+';';
		}
	}

	public class TemplateMixin : AbstractStatement
	{
		public string TemplateId;
		public IExpression[] Arguments;
		public string MixinId;

		public override string ToCode()
		{
			var r = "mixin "+TemplateId;

			if (Arguments != null && Arguments.Length > 0)
			{
				r+='(';
				foreach(var arg in Arguments)
					r+=arg.ToString()+',';
				r=r.TrimEnd(',')+')';
			}

			if(!string.IsNullOrEmpty(MixinId))
				r+=' '+MixinId;

			return r+';';
		}
	}
}
