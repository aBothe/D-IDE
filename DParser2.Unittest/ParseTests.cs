using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using D_Parser;
using D_Parser.Core;
using D_Parser.Resolver;

namespace ParserTests
{
	public class ParseTests
	{
		public const string dmdDir = "D:\\dmd2";

		public static void TestSingleFile(string file)
		{
			var hp = new HighPrecTimer();

			hp.Start();
			var n = DParser.ParseFile(file);
			hp.Stop();
			Console.WriteLine(hp.Duration + "s");

			printErrors(n);
			Dump(n, "");
		}

		public static void TestSourcePackages()
		{
			var Files = new Dictionary<string, string>();

			foreach (string fn in Directory.GetFiles(dmdDir + "\\src\\phobos", "*.d?", SearchOption.AllDirectories))
			{
				if (fn.EndsWith("phobos\\index.d")) continue;
				Files.Add(fn, File.ReadAllText(fn));
			}
			foreach (string fn in Directory.GetFiles(dmdDir + "\\src\\druntime\\import", "*.d?", SearchOption.AllDirectories))
			{
				Files.Add(fn, File.ReadAllText(fn));
			}

			var hp = new HighPrecTimer();

			hp.Start();
			int i = 0;
			foreach (string file in Files.Keys)
			{
				i++;
				var n = DParser.ParseString(Files[file], false);

				printErrors(n);
			}
			hp.Stop();
			Console.WriteLine(hp.Duration + "s");
			Console.WriteLine("~" + (hp.Duration / Files.Count).ToString() + "s per file");
		}

		public static void TestExpression(string e)
		{
			var ex = DParser.ParseExpression(e);

			Console.WriteLine(e+"\t>>>\t"+ ex);
		}

		public static void TestExpressionStartFinder(string code_untilCaretOffset)
		{
			var start = ReverseParsing.SearchExpressionStart(code_untilCaretOffset, code_untilCaretOffset.Length);

			var expressionCode = code_untilCaretOffset.Substring(start, code_untilCaretOffset.Length- start);

			Console.WriteLine("unfiltered:\t"+code_untilCaretOffset+"\nfiltered:\t" + expressionCode);

			if (string.IsNullOrWhiteSpace(expressionCode))
			{
				Console.WriteLine("No code to parse!");
				return;
			}

			var parser = DParser.Create(new StringReader(expressionCode));
			parser.Lexer.NextToken();

			if (parser.IsAssignExpression())
			{
				var expr = parser.AssignExpression();

				Console.WriteLine("expression:\t" + expr.ToString());
			}
			else
			{
				var type = parser.Type();

				Console.WriteLine("type:\t\t" + type.ToString());
			}
		}



		static void printErrors(IAbstractSyntaxTree mod)
		{
			foreach (var e in mod.ParseErrors)
				Console.WriteLine("Line " + e.Location.Line.ToString() + " Col " + e.Location.Column.ToString() + ": " + e.Message);
		}

		static void Dump(INode n, string lev)
		{
			Console.WriteLine(lev + n.ToString());
			if (n is IBlockNode)
			{
				Console.WriteLine(lev + "{");
				foreach (var ch in n as IBlockNode)
				{
					Dump(ch, lev + "  ");
				}
				Console.WriteLine(lev + "}");
			}
		}
	}
}
