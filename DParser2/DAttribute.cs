using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    /// <summary>
    /// Represents an attrribute a declaration may have or consists of
    /// </summary>
    public class DAttribute
    {
        public int Token;
        public object LiteralContent;
        public static readonly DAttribute Empty = new DAttribute(-1);

        public DAttribute(int Token)
        {
            this.Token = Token;
            LiteralContent = null;
        }

        public DAttribute(int Token, object Content)
        {
            this.Token = Token;
            this.LiteralContent = Content;
        }

        public new string ToString()
        {
            if (LiteralContent != null)
                return DTokens.GetTokenString(Token) + "(" + LiteralContent.ToString() + ")";
            else
                return DTokens.GetTokenString(Token);
        }

        public static bool ContainsAttribute(DAttribute[] HayStack,params int[] NeedleToken)
        {
            var l = new List<int>(NeedleToken);
            foreach (var attr in HayStack)
                if (l.Contains(attr.Token))
                    return true;
            return false;
        }
        public static bool ContainsAttribute(List<DAttribute> HayStack,params int[] NeedleToken)
        {
            var l = new List<int>(NeedleToken);
            foreach (var attr in HayStack)
                if (l.Contains(attr.Token))
                    return true;
            return false;
        }
        public static bool ContainsAttribute(Stack<DAttribute> HayStack,params int[] NeedleToken)
        {
            var l = new List<int>(NeedleToken);
            foreach (var attr in HayStack)
                if (l.Contains(attr.Token))
                    return true;
            return false;
        }


        public bool IsStorageClass
        {
            get
            {
                return DTokens.StorageClass[Token];
            }
        }
    }
}