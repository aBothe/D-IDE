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
            Dictionary<string, string> Files = new Dictionary<string, string>();
            //Files.Add("bind",File.ReadAllText("E:\\dmd2\\src\\phobos\\std\\bind.d"));
            foreach (string fn in Directory.GetFiles("E:\\dmd2\\src\\phobos", "*.d", SearchOption.AllDirectories))
            {
                if (fn.EndsWith("phobos.d")) continue;
                Files.Add(fn, File.ReadAllText(fn));
            }

            HiPerfTimer hp = new HiPerfTimer();
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            hp.Start();
            int i = 0;
            foreach (string file in Files.Keys)
            {
                i++;
                DParser dp = DParser.Create(new StringReader(Files[file]));
                DModule n = dp.Parse(false);
            }
            hp.Stop();
            Console.WriteLine(hp.Duration + "s");

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
