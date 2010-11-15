using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    /// <summary>
    /// Encapsules an entire document and represents the root node
    /// </summary>
    public class DModule : DBlockStatement
    {
        public string ModuleName="";
        public string ModuleFileName="";

        /// <summary>
        /// Contains all done imports whereas the key contains the module path and the value equals the public state of the import
        /// </summary>
        public Dictionary<string,bool> Imports = new Dictionary<string,bool>();

        /// <summary>
        /// Applies file name, children and imports from an other module instance
         /// </summary>
        /// <param name="Other"></param>
        public void ApplyFrom(DModule Other)
        {
            ModuleFileName = Other.ModuleFileName;
            children = new List<DNode>(Other.Children);
            foreach (var ch in Children)
                ch.Parent = this;
            Imports=new Dictionary<string,bool>(Other.Imports);
        }

        /// <summary>
        /// Returns the module name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ModuleName;
        }

        public string ToString(bool IncludeFileName)
        {
            return ModuleName+(IncludeFileName?(" ("+ModuleFileName+")"):"");
        }
    }

    public class DVariable : DNode
    {
        public DExpression Initializer; // Variable
        public bool IsAlias = false;

        public override string ToString()
        {
            return (IsAlias?"alias ":"")+base.ToString()+(Initializer!=null?(" = "+Initializer.ToString()):"");
        }
    }

    public abstract class DBlockStatement : DNode, IEnumerable<DNode>
    {
        public CodeLocation BlockStartLocation=new CodeLocation();

        public DNode Assign(DBlockStatement block)
        {
            children = block.children;
            BlockStartLocation = block.BlockStartLocation;
            return base.Assign(block as DNode);
        }

        protected List<DNode> children = new List<DNode>();

        public int Count
        {
            get { return children.Count; }
        }

        public DNode this[int i]
        {
            get { if (children.Count > i)return (DNode)children[i]; else return null; }
            set { if (children.Count > i) children[i] = value; }
        }

        public DNode this[string name]
        {
            get
            {
                if (children.Count > 1)
                {
                    foreach (DNode n in Children)
                    {
                        if ((n as DNode).Name == name) return (n as DNode);
                    }
                }
                return null;
            }
            set
            {
                if (children.Count > 1)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        if (this[i].Name == name) this[i] = value;
                    }
                }
            }
        }

        public void Add(DNode v)
        {
            children.Add(v);
            v.Parent = this;
        }
        /// <summary>
        /// Adds children of <para>ItemOwner</para> to the node's children
        /// </summary>
        /// <param name="ItemOwner"></param>
        public void AddRange(DBlockStatement ItemOwner)
        {
            foreach (var n in ItemOwner)
                Add(n);
        }

        public List<DNode> Children
        {
            get { return children; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return children.GetEnumerator();
        }

        IEnumerator<DNode> IEnumerable<DNode>.GetEnumerator()
        {
            return children.GetEnumerator();
        }
    }

    public class DMethod : DBlockStatement
    {
        public List<DNode> Parameters=new List<DNode>();
        public MethodType SpecialType = MethodType.Normal;

        public enum MethodType
        {
            Normal=0,
            Delegate,
            Constructor,
            Destructor
        }

        public override string ToString()
        {
            var s= base.ToString()+"(";
            foreach (var p in Parameters)
                s += p.ToString()+",";
            return s.Trim(',')+")";
        }
    }

    public class DStatementBlock : DBlockStatement
    {
        public int Token;
        public DExpression Expression;

        public DStatementBlock(int Token)
        {
            this.Token = Token;
        }

        public DStatementBlock() { }

        public override string ToString()
        {
            return DTokens.GetTokenString(Token)+(Expression!=null?("("+Expression.ToString()+")"):"");
        }
    }

    public class DClassLike : DBlockStatement
    {
        public List<TypeDeclaration> BaseClasses=new List<TypeDeclaration>();
        public int ClassType=DTokens.Class;

        public DClassLike() { }
        public DClassLike(int ClassType)
        {
            this.ClassType = ClassType;
        }

        public override string ToString()
        {
            string ret = AttributeString + " " + DTokens.GetTokenString(ClassType) + " " + ToDeclarationString(false);
            if (BaseClasses.Count > 0)
                ret += ":";
            foreach (var c in BaseClasses)
                ret += c.ToString()+", ";

            return ret.Trim().TrimEnd(',');
        }
    }

    public class DEnum : DBlockStatement
    {
        public TypeDeclaration EnumBaseType;

        public override string ToString()
        {
            return "enum "+ToDeclarationString(false)+(EnumBaseType!=null?(":"+EnumBaseType.ToString()):"");
        }
    }

    public class DEnumValue : DVariable
    {
    }
}
