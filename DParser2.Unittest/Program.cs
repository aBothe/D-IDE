using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;
using D_Parser.Dom;
using D_Parser.Resolver;
using D_Parser.Parser;

namespace ParserTests
{
	class Program
	{
		public static string curFile = @"A:\D\dmd2\src\phobos\std\datetime.d";
		public static void Main(string[] args)
		{
			a();
			Console.ReadKey();
		}

		static void a()
		{
			var fcon=File.ReadAllText(curFile);
			var lx = new Lexer(new StringReader(fcon));

			var hp = new HighPrecTimer();

			hp.Start();

			lx.NextToken();

			while (lx.LookAhead.Kind != DTokens.EOF)
			{
				lx.NextToken();
			}

			hp.Stop();

			Console.WriteLine(Math.Round(hp.Duration, 3) + "s");
		}

		static void b()
		{
			string input =
@"void main(){pragma(msg, asdf, 23, ""ho"");}";

			if (false)
			{
				var mod = ParseTests.TestCode(input);
			}
			else
				ParseTests.TestSourcePackages(true);

			//ParseTests.TestExpression(code);
			//ParseTests.TestExpressionStartFinder(code);
		}

		static void c()
		{
			string input = "";
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
