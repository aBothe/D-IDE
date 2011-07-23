using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using D_Parser;
using D_Parser.Dom;
using D_Parser.Resolver;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;

namespace ParserTests
{
	public class ParseTests
	{
		public const string dmdDir = "A:\\D\\dmd2";

		public static void TestSingleFile(string file)
		{
			var hp = new HighPrecTimer();

			hp.Start();
			var n = DParser.ParseFile(file);
			hp.Stop();
			Console.WriteLine((int)(hp.Duration * 1000) + " ms");

			printErrors(n);
			Dump(n, "");
		}

		public static IAbstractSyntaxTree TestCode(string code)
		{
			var hp = new HighPrecTimer();

			hp.Start();
			var n = DParser.ParseString(code);
			hp.Stop();
			Console.WriteLine((int)( hp.Duration*1000) + " ms");

			printErrors(n);
			Dump(n, "");
			return n;
		}

		public static void TestSourcePackages(bool parseOuterStructureOnly=false)
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

			for (int j = 10; j >= 1; j--)
			{
				var hp = new HighPrecTimer();

				hp.Start();
				int i = 0;
				foreach (string file in Files.Keys)
				{
					i++;
					var n = DParser.ParseString(Files[file], parseOuterStructureOnly);

					printErrors(n);
				}
				hp.Stop();
				Console.WriteLine(Math.Round(hp.Duration,3) + "s | ~" + Math.Round(hp.Duration * 1000 / Files.Count, 1).ToString() + "ms per file");
			}
		}

		public static IExpression TestExpression(string e)
		{
			var ex = DParser.ParseExpression(e);

			Console.WriteLine(e+"\t>>>\t"+ ex);
			return ex;
		}

		public static decimal TestMathExpression(string mathExpression)
		{
			var ex = DParser.ParseExpression(mathExpression);

			if (ex == null || !ex.IsConstant)
			{
				Console.WriteLine("\""+mathExpression+"\" not a mathematical expression!");
				return 0;
			}

			Console.WriteLine(ex.ToString()+" = "+ex.DecValue);
			return ex.DecValue;
		}

		public static void TestTypeEval(string e)
		{
			var ex = DParser.ParseExpression(e);

			Console.WriteLine("Code:\t\t"+e);
			Console.WriteLine("Expression:\t"+ex.ToString());

			var tr = ex.ExpressionTypeRepresentation;

			Console.WriteLine("Type representation:\t"+tr.ToString());
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

		public static void IsTypeIdentifierTest(string code)
		{
			bool ShowCCPopup = !DResolver.IsTypeIdentifier(code, code.Length - 1);

			Console.WriteLine("Code before caret:\t\t" + code);
			Console.WriteLine("Show the completion popup:\t" + ShowCCPopup.ToString());
		}



		static void printErrors(IAbstractSyntaxTree mod)
		{
			if(mod.ParseErrors.Count>0)
				Console.WriteLine(mod.ModuleName);

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
