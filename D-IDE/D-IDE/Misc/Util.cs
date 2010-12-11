using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.TextEditor;
using D_Parser;
using Parser.Core;

namespace D_IDE.Misc
{
    public class Util
    {
        public static TextLocation ToTextLocation(CodeLocation loc)
        {
            return new TextLocation(loc.Column, loc.Line);
        }

        public static CodeLocation ToCodeLocation(TextLocation loc)
        {
            return new CodeLocation(loc.Column, loc.Line);
        }
    }

    class ErrorLogger
    {
        public static void Log(Exception ex)
        {

        }
    }
}
