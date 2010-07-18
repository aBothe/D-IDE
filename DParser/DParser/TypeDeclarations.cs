using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public abstract class TypeDeclaration
    {
        public abstract uint GetDeclarationClassTypeId { get; }

        public TypeDeclaration Base=null;
        public TypeDeclaration ParentDecl { get { return Base; } set { Base = value; } }

        public TypeDeclaration HasSpecificTypeDeclaration<DeclarationType>()
        {
            if (Base == null) return null;

            return Base.HasSpecificTypeDeclaration<DeclarationType>();
        }

        /// <summary>
        /// Returns a string which represents the current type
        /// </summary>
        /// <returns></returns>
        public new abstract string ToString();
    }

    /// <summary>
    /// Basic type, e.g. &gt;int&lt;
    /// </summary>
    public class NormalDeclaration : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 1; } }
        public string Name;

        public NormalDeclaration() { }
        public NormalDeclaration(string Identifier)
        { Name = Identifier.Trim('(',')'); }

        public override string  ToString()
        {
            return Name + (Base != null ? (" " + Base.ToString()) : "");
        }
    }

    public class DTokenDeclaration : NormalDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 2; } }
        public int Token;

        public DTokenDeclaration() { }
        public DTokenDeclaration(int Token)
        { this.Token = Token; }

        public new string Name
        {
            get { return DTokens.GetTokenString(Token); }
            set { Token = DTokens.GetTokenID(value); }
        }

        public override string ToString()
        {
            return Name + (Base != null ? (" "+Base.ToString()) : "");
        }
    }

    /// <summary>
    /// Array decl, e.g. &gt;int[string]&lt; myArray;
    /// </summary>
    public class ArrayDecl : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 3; } }
        public TypeDeclaration ValueType
        {
            get { return Base; }
            set { Base = value; }
        }
        public TypeDeclaration KeyType;

        public ArrayDecl() { }
        public ArrayDecl(TypeDeclaration ValueType) { this.ValueType = ValueType; }

        public override string ToString()
        {
            return ValueType.ToString()+"["+(KeyType!=null? KeyType.ToString():"")+"]";
        }
    }

    public class DelegateDeclaration : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 4; } }
        public TypeDeclaration ReturnType
        {
            get { return Base; }
            set { Base = value; }
        }
        /// <summary>
        /// Is it a function(), not a delegate() ?
        /// </summary>
        public bool IsFunction = false;

        public List<DVariable> Parameters = new List<DVariable>();

        public override string ToString()
        {
            string ret=ReturnType.ToString()+(IsFunction?" function":" delegate")+"(";

            foreach (DVariable n in Parameters)
            {
                ret += n.Type.ToString()+" "+n.name+ (String.IsNullOrEmpty(n.Value)?"":("= "+n.Value))+", ";
            }
            ret=ret.TrimEnd(',',' ')+")";
            return ret;
        }
    }

    /// <summary>
    /// int* ptr;
    /// </summary>
    public class PointerDecl : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 5; } }
        public PointerDecl() { }
        public PointerDecl(TypeDeclaration BaseType) { Base = BaseType; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "*";
        }
    }

    /// <summary>
    /// const(char)
    /// </summary>
    public class MemberFunctionAttributeDecl : DTokenDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 6; } }
        public int Modifier
        {
            get { return Token; }
            set { Token = value; }
        }

        public MemberFunctionAttributeDecl() { }
        public MemberFunctionAttributeDecl(int ModifierToken) { this.Modifier = ModifierToken; }

        public override string ToString()
        {
            return Name+"("+(Base!=null? Base.ToString():"")+")";
        }
    }

    public class VarArgDecl : NormalDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 7; } }
        public VarArgDecl() { }
        public VarArgDecl(TypeDeclaration BaseIdentifier) { Base = BaseIdentifier; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "...";
        }
    }

    // Secondary importance
    /// <summary>
    /// class ABC: &gt;A, C&lt;
    /// </summary>
    public class InheritanceDecl : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 8; } }
        public TypeDeclaration InheritedClass;
        public TypeDeclaration InheritedInterface;

        public InheritanceDecl() { }
        public InheritanceDecl(TypeDeclaration Base) { this.Base = Base; }

        public override string ToString()
        {
            return Base.ToString()+": "+InheritedClass.ToString()+(InheritedInterface!=null?(", "+InheritedInterface.ToString()):"");
        }
    }

    /// <summary>
    /// List&lt;T:base&gt; myList;
    /// </summary>
    public class TemplateDecl : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 9; } }
        public TypeDeclaration Template;

        public TemplateDecl() { }
        public TemplateDecl(TypeDeclaration Base)
        {
            this.Base = Base;
        }

        public override string ToString()
        {
            return (Base!=null?Base.ToString():"").ToString()+"!("+(Template!=null?Template.ToString():"")+")";
        }
    }

    /// <summary>
    /// A.B
    /// </summary>
    public class DotCombinedDeclaration : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 10; } }
        public TypeDeclaration AccessedMember;

        public DotCombinedDeclaration() { }
        public DotCombinedDeclaration(TypeDeclaration Base) { this.Base = Base; }

        public override string ToString()
        {
            return (Base!=null?Base.ToString():"")+"."+(AccessedMember!=null? AccessedMember.ToString():"");
        }
    }


#region Expressions
    public class BooleanExpression : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId  { get { return 11; } }
        public int OperatorToken;
        public TypeDeclaration LeftValue
        {
            get { return Base; }
            set { Base = value; }
        }
        public TypeDeclaration RightValue;

        public BooleanExpression() { }
        public BooleanExpression(int Operator) { OperatorToken = Operator; }
        public BooleanExpression(int Operator, TypeDeclaration Left) { OperatorToken = Operator; LeftValue = Left; }

        public override string ToString()
        {
            return LeftValue.ToString()+" "+DTokens.GetTokenString(OperatorToken)+" "+RightValue.ToString();
        }
    }

    public class DecisiveBooleanExpression : TypeDeclaration
    {
        public override uint GetDeclarationClassTypeId { get { return 12; } }
        public BooleanExpression TriggerExpression;
        public TypeDeclaration TrueExpression, FalseExpression;

        public DecisiveBooleanExpression() { }

        public override string ToString()
        {
            return TriggerExpression.ToString()+"?"+TrueExpression.ToString()+":"+FalseExpression.ToString();
        }
    }
#endregion


}