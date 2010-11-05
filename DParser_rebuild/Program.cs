using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PAB;
using System.Threading;

namespace D_Parser
{
    class Program
    {
        public static string curFile = "";
        public static void Main(string[] args)
        {
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            var Files = new Dictionary<string, string>();
            int a = 1;
            if (a == 0)
                Files.Add("abc", File.ReadAllText("D:\\dmd2\\src\\phobos\\std\\bigint.d"));
            else
            {
                foreach (string fn in Directory.GetFiles("D:\\dmd2\\src\\phobos", "*.d", SearchOption.AllDirectories))
                {
                    if (fn.EndsWith("phobos.d")) continue;
                    Files.Add(fn, File.ReadAllText(fn));
                }
                foreach (string fn in Directory.GetFiles("D:\\dmd2\\src\\druntime\\import", "*.d?", SearchOption.AllDirectories))
                {
                    Files.Add(fn, File.ReadAllText(fn));
                }
            }

            int b = 0;
            if (b == 0)
            {
                var hp = new HiPerfTimer();

                hp.Start();
                int i = 0;
                foreach (string file in Files.Keys)
                {
                    curFile = file;
                    if (curFile.Contains("zlib.d")) {}
                    // if(la.line==827) {}
                    i++;
                    var dp = DParser.Create(new StringReader(Files[file]));
                    var n = dp.Parse(false);
                }
                hp.Stop();
                Console.WriteLine(hp.Duration + "s");
            }
            else
            {
                var lex = new DLexer(new StringReader("fdsa... fgh .. . asdf[0..2] 0.578 .125 1024.125 345.11 0b11 01234"));
                lex.NextToken();
                Console.WriteLine(lex.LookAhead.ToString());

                while (lex.LookAhead.Kind != DTokens.EOF)
                {
                    lex.NextToken();
                    Console.WriteLine(lex.LookAhead.ToString());
                }
            }
            
            Console.Read();
            return;
        }

        static void DParser_OnError(DModule tempModule, int line, int col, int kindOf, string message)
        {
            string f = curFile;
            throw new Exception(message);
            Console.WriteLine("Line " + line.ToString() + " Col " + col.ToString() + ": " + message);
        }

        static void Dump(DNode n, string lev)
        {
            Console.WriteLine(lev + n.ToString());
            if (n is DBlockStatement)
            {
                Console.WriteLine(lev + "{");
                foreach (var ch in n as DBlockStatement)
                {
                    Dump(ch, lev + "  ");
                }
                Console.WriteLine(lev + "}");
            }
        }
    }
}
