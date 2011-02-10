using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;

namespace ParserTests
{
    class Program
    {
        public static string curFile = "";
        public static void Main(string[] args)
        {
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            var Files = new Dictionary<string, string>();
            int a = 1; // 0 - Read 1 file; 1 - Read all files; 2 - no action
            int b = a>1?1:0; // 0 - Parse file(s); 1 - Parse specific text only

            if (a == 0)
                Files.Add("abc", File.ReadAllText("D:\\dmd2\\src\\phobos\\std\\path.d"));
            else if(a==1)
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
                }
                hp.Stop();
                Console.WriteLine(hp.Duration + "s");
            }
            else if(b==1)
            {
                var n = DParser.ParseString(
@"

unittest
{
    assert(dirname() == );
{

asdasd
}
asdasdasd asd
}
",true);
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
