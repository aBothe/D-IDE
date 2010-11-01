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
        public object LiteralContent=null;

        public DAttribute(int Token)
        {
            this.Token = Token;
        }

        public DAttribute(int Token, object Content)
        {
            this.Token = Token;
            this.LiteralContent = Content;
        }

        public string ToString()
        {
            if (LiteralContent != null)
                return DTokens.GetTokenString(Token) + "(" + LiteralContent.ToString() + ")";
            else
                return DTokens.GetTokenString(Token);
        }

        public static bool ContainsAttribute(DAttribute[] HayStack, int NeedleToken)
        {
            foreach (var attr in HayStack)
                if (attr.Token == NeedleToken)
                    return true;
            return false;
        }
        public static bool ContainsAttribute(List<DAttribute> HayStack, int NeedleToken)
        {
            foreach (var attr in HayStack)
                if (attr.Token == NeedleToken)
                    return true;
            return false;
        }
        public static bool ContainsAttribute(Stack<DAttribute> HayStack, int NeedleToken)
        {
            foreach (var attr in HayStack)
                if (attr.Token == NeedleToken)
                    return true;
            return false;
        }
    }
}