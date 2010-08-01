using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace D_Parser
{
    class Program
    {
        public static void Main(string[] args)
        {
            string src = /*
                "module abc.test;"+
                "import Test=astd.myImport;"+
                "import std.stdio;"+
                "import core.memory, core.gc;"+*/
                /*
                "int x;" +
                "int* y;"+
                "int (*myFct);"+*/
                "foo();"+
                "void[]**[] foo(int a, bool b) {}";
            List<string> imps;
            DParser dp = DParser.Create(new StringReader(src));
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            DNode n = dp.Parse("", out imps);
            n.name = n.module;

            Dump(n,"");

            return;
        }

        static void DParser_OnError(string file, string module, int line, int col, int kindOf, string message)
        {
            Console.WriteLine("Line "+line.ToString()+" Col "+col.ToString()+": "+message);
        }

        static void Dump(DNode n,string lev)
        {
            Console.WriteLine(lev+n.ToString());
            if (n.Count > 0)
            {
                Console.WriteLine(lev+"{");
                foreach (DNode ch in n)
                {
                    Dump(ch,lev+"  ");
                }
                Console.WriteLine(lev+"}");
            }
        }
    }
}
