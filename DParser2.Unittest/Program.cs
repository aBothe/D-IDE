using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;
using D_Parser.Core;

namespace ParserTests
{
    class Program
    {
		public const string dmdDir = "D:\\dmd2";

        public static string curFile = "";
        public static void Main(string[] args)
        {
            var Files = new Dictionary<string, string>();
            int a = 0; // 0 - Read 1 file; 1 - Read all files; 2 - no action
            int b = a>1?1:0; // 0 - Parse file(s); 1 - Parse specific text only

            if (a == 0)
                Files.Add("abc", File.ReadAllText(
					//dmdDir+"\\src\\phobos\\std\\path.d"
					@"D:\dmd2\src\phobos\std\array.d"
					));
            else if(a==1)
            {
                foreach (string fn in Directory.GetFiles(dmdDir+"\\src\\phobos", "*.d?", SearchOption.AllDirectories))
                {
                    if (fn.EndsWith("phobos\\index.d")) continue;
                    Files.Add(fn, File.ReadAllText(fn));
                }
                foreach (string fn in Directory.GetFiles(dmdDir+"\\src\\druntime\\import", "*.d?", SearchOption.AllDirectories))
                {
                    Files.Add(fn, File.ReadAllText(fn));
                }
            }

            
            if (b == 0)
            {
                var hp = new HighPrecTimer();

                hp.Start();
                int i = 0;
                foreach (string file in Files.Keys)
                {
                    curFile = file;
                    
                    i++;
                    var n = DParser.ParseString(Files[file], false);

					printErrors(n);
                }
                hp.Stop();
                Console.WriteLine(hp.Duration + "s");
            }
            else if(b==1)
            {
                var n = DParser.ParseExpression(
@"
(cast(void[])a).length;
");
				//printErrors(n);Dump(n,"");

				Console.WriteLine(n.ToString());
				
            }
            Console.Read();
            return;
        }

        static void printErrors(IAbstractSyntaxTree mod)
        {
			foreach(var e in mod.ParseErrors)
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
