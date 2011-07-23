﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;
using D_Parser.Dom;
using D_Parser.Resolver;

namespace ParserTests
{
    class Program
    {
        public static string curFile = "";
        public static void Main(string[] args)
        {
			string input =
@"toUpper!S(s)";
			
			var mod=ParseTests.TestExpression(input);

			//ParseTests.TestSourcePackages();
			
			//ParseTests.TestExpression(code);
			//ParseTests.TestExpressionStartFinder(code);

			Console.ReadKey();

			return;
			while (true)
			{
				input = Console.ReadLine();

				if (input == "q")
					return;

				var code = input;

				ParseTests.TestMathExpression(code);
			}
        }
    }
}
