using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;

namespace D_Parser
{
	public class DExpressionDecl : AbstractTypeDeclaration
	{
		public IExpression Expression;

		public DExpressionDecl() { }

		public DExpressionDecl(IExpression dExpression)
		{
			this.Expression = dExpression;
		}

		public override string ToString()
		{
			return Expression.ToString();
		}
	}

	public interface IExpression
	{
	}

	public abstract class OperatorBasedExpression : IExpression
	{
		public virtual IExpression LeftOperand { get; set; }
		public virtual IExpression RightOperand { get; set; }
		public int OperatorToken { get; protected set; }

		public override string ToString()
		{
			return LeftOperand.ToString() + DTokens.GetTokenString(OperatorToken) + (RightOperand!=null? RightOperand.ToString():"");
		}
	}

	public class Expression : IExpression, IEnumerable<IExpression>
	{
		public IList<IExpression> Expressions = new List<IExpression>();

		public void Add(IExpression ex)
		{
			Expressions.Add(ex);
		}

		public IEnumerator<IExpression> GetEnumerator()
		{
			return Expressions.GetEnumerator();
		}

		public override string ToString()
		{
			var s = "";
			foreach (var ex in Expressions)
				s += ex.ToString() + ",";
			return s.TrimEnd(',');
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Expressions.GetEnumerator();
		}
	}

	public class AssignExpression : OperatorBasedExpression {
		public AssignExpression(int opToken) { OperatorToken = opToken; }
	}

	public class ConditionalExpression : IExpression
	{
		public IExpression OrOrExpression { get; set; }

		public IExpression TrueCaseExpression { get; set; }
		public IExpression FalseCaseExpression { get; set; }

		public override string ToString()
		{
			return this.OrOrExpression.ToString() + "?" + TrueCaseExpression.ToString() + FalseCaseExpression.ToString();
		}
	}

	public class OrOrExpression : OperatorBasedExpression
	{
		public OrOrExpression() { OperatorToken = DTokens.LogicalOr; }
	}

	public class AndAndExpression : OperatorBasedExpression
	{
		public AndAndExpression() { OperatorToken = DTokens.LogicalAnd; }
	}

	public class XorExpression : OperatorBasedExpression
	{
		public XorExpression() { OperatorToken = DTokens.Xor; }
	}

	public class OrExpression : OperatorBasedExpression
	{
		public OrExpression() { OperatorToken = DTokens.BitwiseOr; }
	}

	public class AndExpression : OperatorBasedExpression
	{
		public AndExpression() { OperatorToken = DTokens.BitwiseAnd; }
	}

	public class EqualExpression : OperatorBasedExpression
	{
		public EqualExpression(bool isUnEqual) { OperatorToken = isUnEqual ? DTokens.NotEqual : DTokens.Equal; }
	}

	public class IdendityExpression : OperatorBasedExpression
	{
		public bool Not;

		public IdendityExpression(bool notIs) { Not=notIs;OperatorToken = DTokens.Is; }

		public override string ToString()
		{
			return LeftOperand.ToString() + (Not?" !":" ")+ "is " + RightOperand.ToString();
		}
	}

	public class RelExpression : OperatorBasedExpression
	{
		public RelExpression(int relationalOperator) { OperatorToken = relationalOperator; }
	}

	public class InExpression : OperatorBasedExpression
	{
		public bool Not;

		public InExpression(bool notIn) { Not=notIn;OperatorToken = DTokens.In; }

		public override string ToString()
		{
			return LeftOperand.ToString() + (Not ? " !" : " ") + "in " + RightOperand.ToString();
		}
	}

	public class ShiftExpression : OperatorBasedExpression
	{
		public ShiftExpression(int shiftOperator) { OperatorToken = shiftOperator; }
	}

	public class AddExpression : OperatorBasedExpression
	{
		public AddExpression(bool isMinus) { OperatorToken = isMinus ? DTokens.Minus : DTokens.Plus; }
	}

	public class MulExpression : OperatorBasedExpression
	{
		public MulExpression(int mulOperator) { OperatorToken = mulOperator; }
	}

	public class CatExpression : OperatorBasedExpression
	{
		public CatExpression() { OperatorToken = DTokens.Tilde; }
	}

	public interface UnaryExpression : IExpression { }

	public class PowExpression : OperatorBasedExpression, UnaryExpression
	{
		public PowExpression() { OperatorToken = DTokens.Pow; }
	}

	public abstract class SimpleUnaryExpression : UnaryExpression
	{
		public abstract int ForeToken { get; }
		public IExpression UnaryExpression { get; set; }

		public override string ToString()
		{
			return DTokens.GetTokenString(ForeToken)+UnaryExpression.ToString();
		}
	}

	public class UnaryExpression_And : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.BitwiseAnd; }
		}
	}

	public class UnaryExpression_Increment : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Increment; }
		}
	}

	public class UnaryExpression_Decrement : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Decrement; }
		}
	}

	public class UnaryExpression_Mul : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Times; }
		}
	}

	public class UnaryExpression_Add : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Plus; }
		}
	}

	public class UnaryExpression_Sub : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Minus; }
		}
	}

	public class UnaryExpression_Not : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Not; }
		}
	}

	public class UnaryExpression_Cat : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Tilde; }
		}
	}

	/* This thing here is simply unusable - it's practically impossible to determine wether an expression is meant as a expression or as a type...
	/// <summary>
	/// (Type).Identifier
	/// </summary>
	public class UnaryExpression_Type : UnaryExpression
	{
		public ITypeDeclaration Type { get; set; }
		public string AccessIdentifier { get; set; }

		public override string ToString()
		{
			return "("+Type.ToString()+")."+AccessIdentifier;
		}
	}*/


	/// <summary>
	/// NewExpression:
	///		NewArguments Type [ AssignExpression ]
	///		NewArguments Type ( ArgumentList )
	///		NewArguments Type
	/// </summary>
	public class NewExpression : UnaryExpression
	{
		public ITypeDeclaration Type { get; set; }
		public IExpression[] NewArguments { get; set; }
		public IExpression[] Arguments{get;set;}

		/// <summary>
		/// true if new myType[10]; instead of new myType(1,"asdf"); has been used
		/// </summary>
		public bool IsArrayArgument { get; set; }

		public override string ToString()
		{
			var ret= "new";

			if (NewArguments != null)
			{
				ret += "(";
				foreach (var e in NewArguments)
					ret += e.ToString()+",";
				ret = ret.TrimEnd(',') +")";
			}

			ret += " "+Type.ToString();

			ret += IsArrayArgument?'[':'(';
			foreach (var e in Arguments)
				ret += e.ToString()+",";

			ret = ret.TrimEnd(',') + (IsArrayArgument ? ']' : ')');

			return ret;
		}
	}

	/// <summary>
	/// NewArguments ClassArguments BaseClasslist { DeclDefs } 
	/// new ParenArgumentList_opt class ParenArgumentList_opt SuperClass_opt InterfaceClasses_opt ClassBody
	/// </summary>
	public class AnonymousClassExpression : UnaryExpression
	{
		public IExpression[] NewArguments { get; set; }
		public DClassLike AnonymousClass { get; set; }

		public IExpression[] ClassArguments { get; set; }

		public override string ToString()
		{
			var ret = "new";

			if (NewArguments != null)
			{
				ret += "(";
				foreach (var e in NewArguments)
					ret += e.ToString() + ",";
				ret = ret.TrimEnd(',') + ")";
			}

			ret += " class";

			if (ClassArguments != null)
			{
				ret += '(';
				foreach (var e in ClassArguments)
					ret += e.ToString() + ",";

				ret = ret.TrimEnd(',') + ")";
			}

			if(AnonymousClass!=null && AnonymousClass.BaseClasses!=null)
			{
				ret += ":";

				foreach (var t in AnonymousClass.BaseClasses)
					ret += t.ToString()+",";

				ret = ret.TrimEnd(',');
			}

			ret += " {...}";

			return ret;
		}
	}

	public class DeleteExpression : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Delete; }
		}
	}

	/// <summary>
	/// CastExpression:
	///		cast ( Type ) UnaryExpression
	///		cast ( CastParam ) UnaryExpression
	/// </summary>
	public class CastExpression : UnaryExpression
	{
		public bool IsTypeCast
		{
			get { return Type != null; }
		}
		public IExpression UnaryExpression;

		public ITypeDeclaration Type { get; set; }
		public int[] CastParamTokens { get; set; }

		public override string ToString()
		{
			var ret="cast(";

			if (IsTypeCast)
				ret += Type.ToString();
			else
			{
				foreach (var tk in CastParamTokens)
					ret += DTokens.GetTokenString(tk)+" ";
				ret = ret.TrimEnd(' ');
			}

			ret += ") "+UnaryExpression.ToString();

			return ret;
		}
	}

	public abstract class PostfixExpression : IExpression
	{
		public IExpression PostfixForeExpression { get; set; }
	}

	/// <summary>
	/// PostfixExpression . Identifier
	/// PostfixExpression . TemplateInstance
	/// PostfixExpression . NewExpression
	/// </summary>
	public class PostfixExpression_Access : PostfixExpression
	{
		public IExpression NewExpression;
		public ITypeDeclaration TemplateOrIdentifier;

		public override string ToString()
		{
			return PostfixForeExpression.ToString()+"."+(TemplateOrIdentifier!=null?TemplateOrIdentifier.ToString():NewExpression.ToString());
		}
	}

	public class PostfixExpression_Increment : PostfixExpression
	{
		public override string ToString()
		{
			return PostfixForeExpression.ToString()+"++";
		}
	}

	public class PostfixExpression_Decrement : PostfixExpression
	{
		public override string ToString()
		{
			return PostfixForeExpression.ToString() + "--";
		}
	}

	/// <summary>
	/// PostfixExpression ( )
	/// PostfixExpression ( ArgumentList )
	/// </summary>
	public class PostfixExpression_MethodCall : PostfixExpression
	{
		public IExpression[] Arguments;

		public override string ToString()
		{
			var ret = PostfixForeExpression.ToString()+"(";

			if (Arguments != null)
				foreach (var a in Arguments)
					ret +=a.ToString()+ ",";

			return ret.TrimEnd(',')+")";
		}
	}

	/// <summary>
	/// IndexExpression:
	///		PostfixExpression [ ArgumentList ]
	/// </summary>
	public class PostfixExpression_Index : PostfixExpression
	{
		public IExpression[] Arguments;

		public override string ToString()
		{
			var ret = PostfixForeExpression.ToString() + "[";

			if (Arguments != null)
				foreach (var a in Arguments)
					ret += a.ToString() + ",";

			return ret.TrimEnd(',') + "]";
		}
	}

	public class PostfixExpression_Slice : PostfixExpression
	{
		public IExpression FromExpression;
		public IExpression ToExpression;

		public override string ToString()
		{
			var ret = PostfixForeExpression.ToString() + "[";

			if (FromExpression != null)
				ret += FromExpression.ToString();

			if (FromExpression != null && ToExpression != null)
				ret += "..";

			if (ToExpression != null)
				ret += ToExpression.ToString();

			return ret + "]";
		}
	}

	public interface PrimaryExpression : IExpression{ }



	/// <summary>
	/// Identifier as well as literal primary expression
	/// </summary>
	public class IdentifierExpression : PrimaryExpression
	{
		public bool IsIdentifier { get { return Value is string; } }

		public object Value = "";

		public IdentifierExpression() { }
		public IdentifierExpression(object Val) { Value = Val; }

		public override string ToString()
		{
			return
				(IsIdentifier ?
					Value as string :
					((Value == null) ?
						string.Empty :
						Value.ToString()));
		}
	}

	public class TokenExpression : PrimaryExpression
	{
		public int Token;

		public TokenExpression() { }
		public TokenExpression(int T) { Token = T; }

		public override string ToString()
		{
			return DParser.GetTokenString(Token);
		}
	}

	/// <summary>
	/// TemplateInstance
	/// BasicType . Identifier
	/// </summary>
	public class TypeDeclarationExpression : PrimaryExpression
	{
		public bool IsTemplateDeclaration
		{
			get { return Declaration is TemplateDecl; }
		}

		public ITypeDeclaration Declaration;

		public TypeDeclarationExpression() { }
		public TypeDeclarationExpression(ITypeDeclaration td) { Declaration = td; }

		public override string ToString()
		{
			return Declaration != null ? Declaration.ToString() : "";
		}
	}

	/// <summary>
	/// auto arr= [1,2,3,4,5,6];
	/// </summary>
	public class ArrayLiteralExpression : PrimaryExpression
	{
		public ArrayLiteralExpression()
		{
			Expressions = new List<IExpression>(); 
		}

		public virtual IEnumerable<IExpression> Expressions { get; set; }

		public override string ToString()
		{
			var s = "[";
			foreach (var expr in Expressions)
				s += expr.ToString() + ", ";
			s = s.TrimEnd(' ', ',')+"]";
			return s;
		}
	}

	public class AssocArrayExpression : PrimaryExpression
	{
		public IDictionary<IExpression, IExpression> KeyValuePairs = new Dictionary<IExpression, IExpression>();

		public override string ToString()
		{
			var s = "[";
			foreach (var expr in KeyValuePairs)
				s += expr.Key.ToString()+":"+expr.Value.ToString() + ", ";
			s = s.TrimEnd(' ', ',') + "]";
			return s;
		}
	}

	public class FunctionLiteral : PrimaryExpression
	{
		public int LiteralToken = DTokens.Delegate;

		public DMethod AnonymousMethod = new DMethod();

		public FunctionLiteral() { }
		public FunctionLiteral(int InitialLiteral) { LiteralToken = InitialLiteral; }

		public override string ToString()
		{
			return DTokens.GetTokenString(LiteralToken) + " " + AnonymousMethod.ToString();
		}
	}

	public class AssertExpression : PrimaryExpression
	{
		public IExpression[] AssignExpressions;

		public override string ToString()
		{
			var ret = "assert(";

			foreach (var e in AssignExpressions)
				ret += e.ToString()+",";

			return ret.TrimEnd(',')+")";
		}
	}

	public class MixinExpression : PrimaryExpression
	{
		public IExpression AssignExpression;

		public override string ToString()
		{
			return "mixin(" + AssignExpression.ToString() + ")";
		}
	}

	public class ImportExpression : PrimaryExpression
	{
		public IExpression AssignExpression;

		public override string ToString()
		{
			return "import(" + AssignExpression.ToString() + ")";
		}
	}

	public class TypeidExpression : PrimaryExpression
	{
		public ITypeDeclaration Type;
		public IExpression Expression;

		public override string ToString()
		{
			return "typeid("+(Type!=null?Type.ToString():Expression.ToString())+")";
		}
	}

	public class IsExpression : PrimaryExpression
	{
		public ITypeDeclaration Type;
		public string Identifier;

		/// <summary>
		/// True if Type == TypeSpecialization instead of Type : TypeSpecialization
		/// </summary>
		public bool EqualityTest;

		public ITypeDeclaration TypeSpecialization;
		public int TypeSpecializationToken;

		public ITemplateParameter[] TemplateParameterList;

		public override string ToString()
		{
			var ret = "is("+Type.ToString();

			ret += Identifier +( EqualityTest?"==":":");

			ret += TypeSpecialization!=null?TypeSpecialization.ToString():DTokens.GetTokenString(TypeSpecializationToken);

			if (TemplateParameterList != null)
			{
				ret += ",";
				foreach (var p in TemplateParameterList)
					ret += p.ToString()+",";
			}

			return ret.TrimEnd(' ',',')+")";
		}
	}

	public class TraitsExpression : PrimaryExpression
	{
		public string Keyword;

		public IEnumerable<TraitsArgument> Arguments;

		public override string ToString()
		{
			var ret="__traits("+Keyword;

			if (Arguments != null)
				foreach (var a in Arguments)
					ret += ","+a.ToString();

			return ret + ")";
		}
	}

	public class TraitsArgument
	{
		public ITypeDeclaration Type;
		public IExpression AssignExpression;

		public override string ToString()
		{
			return Type!=null?Type.ToString():AssignExpression.ToString();
		}
	}

	/// <summary>
	/// ( Expression )
	/// </summary>
	public class SurroundingParenthesesExpression : PrimaryExpression
	{
		public IExpression Expression;

		public override string ToString()
		{
			return "("+Expression.ToString()+")";
		}
	}



#region Template parameters

	public interface ITemplateParameter
	{
		string Name { get; }
	}

	public class TemplateParameterNode : AbstractNode
	{
		public readonly ITemplateParameter TemplateParameter;

		public TemplateParameterNode(ITemplateParameter param)
		{
			TemplateParameter = param;

			Name = param.Name;
		}

		public override string ToString()
		{
			return TemplateParameter.ToString();
		}

		public override string ToString(bool Attributes, bool IncludePath)
		{
			return (GetNodePath(this,false)+"."+ToString()).TrimEnd('.');
		}
	}

	public class TemplateTypeParameter : ITemplateParameter
	{
		public string Name { get; set; }

		public ITypeDeclaration Specialization;
		public ITypeDeclaration Default;

		public override string ToString()
		{
			var ret = Name;

			if (Specialization != null)
				ret += ":"+Specialization.ToString();

			if (Default != null)
				ret += "="+Default.ToString();

			return ret;
		}
	}

	public class TemplateThisParameter : ITemplateParameter
	{
		public string Name { get { return FollowParameter.Name; } }

		public ITemplateParameter FollowParameter;

		public override string ToString()
		{
			return "this"+(FollowParameter!=null?(" "+FollowParameter.ToString()):"");
		}
	}

	public class TemplateValueParameter : ITemplateParameter
	{
		public string Name { get; set; }
		public ITypeDeclaration Type;

		public IExpression SpecializationExpression;
		public IExpression DefaultExpression;
	}

	public class TemplateAliasParameter : TemplateValueParameter
	{
		public ITypeDeclaration SpecializationType;
		public ITypeDeclaration DefaultType;

		public override string ToString()
		{
			return "alias "+base.ToString();
		}
	}

	public class TemplateTupleParameter : ITemplateParameter
	{
		public string Name { get; set; }

		public override string ToString()
		{
			return Name+" ...";
		}
	}

#endregion

#region Initializers

	public interface DInitializer :IExpression { }

	public class VoidInitializer : TokenExpression,DInitializer
	{
		public VoidInitializer():base(DTokens.Void) { }
	}

	public class ArrayInitializer : ArrayLiteralExpression,DInitializer
	{
		public ArrayMemberInitializer[] ArrayMemberInitializations;

		public override IEnumerable<IExpression> Expressions
		{
			get
			{
				foreach (var ami in ArrayMemberInitializations)
					yield return ami.Left;
			}
			set{}
		}

		public override string ToString()
		{
			var ret="[";

			if(ArrayMemberInitializations!=null)
				foreach (var i in ArrayMemberInitializations)
					ret += i.ToString()+",";

			return ret.TrimEnd(',') + "]";
		}
	}

	public class ArrayMemberInitializer
	{
		public IExpression Left;
		public IExpression Specialization;

		public override string ToString()
		{
			return Left.ToString() +(Specialization!=null?(":"+Specialization.ToString()):"");
		}
	}

	public class StructInitializer : DInitializer
	{
		public StructMemberInitializer[] StructMemberInitializers;

		public override string ToString()
		{
			var ret = "{";

			if(StructMemberInitializers!=null)
				foreach (var i in StructMemberInitializers)
					ret += i.ToString() + ",";

			return ret.TrimEnd(',') + "}";
		}
	}

	public class StructMemberInitializer
	{
		public string MemberName=string.Empty;
		public IExpression Specialization;

		public override string ToString()
		{
			return (!string.IsNullOrEmpty(MemberName)? (MemberName+":"):"") + Specialization.ToString();
		}
	}

#endregion
}
