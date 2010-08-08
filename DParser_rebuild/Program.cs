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
        public static void Main(string[] args)
        {
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            Dictionary<string, string> Files = new Dictionary<string, string>();
            //Files.Add("bind",File.ReadAllText("E:\\dmd2\\src\\phobos\\std\\bind.d"));
            /*foreach (string fn in Directory.GetFiles("E:\\dmd2\\src\\phobos", "*.d", SearchOption.AllDirectories))
            {
                if (fn.EndsWith("phobos.d")) continue;
                Files.Add(fn, File.ReadAllText(fn));
            }

            HiPerfTimer hp = new HiPerfTimer();
            

            hp.Start();
            int i = 0;
            foreach (string file in Files.Keys)
            {
                i++;
                DParser dp = DParser.Create(new StringReader(Files[file]));
                DModule n = dp.Parse(false);
            }
            hp.Stop();
            Console.WriteLine(hp.Duration + "s");*/


            DLexer lex = new DLexer(new StringReader("345.11 .125 0b11 01234"));
            lex.NextToken();
            Console.WriteLine(lex.LookAhead.LiteralValue);
            while (lex.LookAhead.Kind != DTokens.EOF)
            {
                lex.NextToken();
                Console.WriteLine(lex.LookAhead.literalValue);
            }
            return;

            //Dump(n,"");
            //Console.Read();
        }

        static void DParser_OnError(DModule tempModule, int line, int col, int kindOf, string message)
        {
            throw new Exception(message);
            Console.WriteLine("Line " + line.ToString() + " Col " + col.ToString() + ": " + message);
        }

        static void Dump(DNode n, string lev)
        {
            Console.WriteLine(lev + n.ToString());
            if (n is DBlockStatement)
            {
                Console.WriteLine(lev + "{");
                foreach (DNode ch in n as DBlockStatement)
                {
                    Dump(ch, lev + "  ");
                }
                Console.WriteLine(lev + "}");
            }
        }
    }
}
