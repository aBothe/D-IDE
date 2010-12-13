using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using Parser.Core;

namespace D_IDE
{
    public class IDEUtil:Util
    {
        public static CodeLocation ToCodeLocation(ICSharpCode.AvalonEdit.Document.TextLocation Caret)
        {
            return new CodeLocation(Caret.Column + 1, Caret.Line + 1);
        }
        public static CodeLocation ToCodeLocation(CodeLocation loc)
        {
            return loc;
        }
    }
}
