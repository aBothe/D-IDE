using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using D_Parser;
using D_Parser.Core;

namespace ParserTests
{
	public class ParseTests
	{
		public const string dmdDir = "D:\\dmd2";

		public void TestSingleFile(string file)
		{
			var hp = new HighPrecTimer();

			hp.Start();
			var n = DParser.ParseFile(file);
			hp.Stop();
			Console.WriteLine(hp.Duration + "s");

			printErrors(n);
			Dump(n, "");
		}

		public void TestSourcePackages()
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

		public void TestExpression(string e)
		{
			var ex = DParser.ParseExpression(e);

			Console.WriteLine(ex);
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
