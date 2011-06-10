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
			//  ) 
			string input = "cast(int) (IOC_IN | (UINT.sizeof & IOCPARM_MASK) << 16 | (102 << 8) | 126)";

			var e=ParseTests.TestExpression(input);
			Console.ReadKey();
			//ParseTests.TestExpression(code);
			//ParseTests.TestExpressionStartFinder(code);
			return;
			while (true)
			{
				input = Console.ReadLine();

				if (input == "q")
					return;

				var code = input;

				ParseTests.TestTypeEval(code);
			}
        }
    }
}
