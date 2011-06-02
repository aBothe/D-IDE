using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;
using D_Parser.Core;
using D_Parser.Resolver;

namespace ParserTests
{
    class Program
    {
        public static string curFile = "";
        public static void Main(string[] args)
        {
			var code = @"
if(myObj.foo().myprop) a=56+asd.
";

			int caret = code.Length - 1;

			var start = ReverseParsing.SearchExpressionStart(code, caret);

			var expressionCode = code.Substring(start,caret-start);

			Console.WriteLine("Parsed Code:\t"+expressionCode);

			var parser = DParser.Create(new StringReader(expressionCode));
			parser.Lexer.NextToken();

			if (parser.IsAssignExpression())
			{
				var expr = parser.AssignExpression();

				Console.WriteLine("Expression:\t" + expr.ToString());
			}else
			{
				var type = parser.Type();

				Console.WriteLine("Type:\t\t" + type.ToString());
			}

            Console.Read();
            return;
        }
    }
}
