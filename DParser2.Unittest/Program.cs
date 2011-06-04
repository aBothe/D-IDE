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
			string input="";

			//ParseTests.TestExpression(code);
			//ParseTests.TestExpressionStartFinder(code);

			while (true)
			{
				input = Console.ReadLine();

				if (input == "q")
					return;

				var code = input;

				bool ShowCCPopup = !DResolver.IsTypeIdentifier(code, code.Length-1);

				Console.WriteLine("Code before caret:\t\t" + code);
				Console.WriteLine("Show the completion popup:\t" + ShowCCPopup.ToString());
			}
        }
    }
}
