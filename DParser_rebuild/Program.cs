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
            string src = File.ReadAllText("E:\\dmd2\\src\\phobos\\std\\algorithm.d"/*"E:\\test.d"*/);

            HiPerfTimer hp = new HiPerfTimer();
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            hp.Start();

            DParser dp = DParser.Create(new StringReader(src));
            DModule n = dp.Parse();

            hp.Stop();
            n.Name = n.ModuleName;
            Console.WriteLine(hp.Duration + "s");
            //Dump(n,"");

            return;
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
