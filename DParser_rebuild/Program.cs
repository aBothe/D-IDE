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
            DParser dp = DParser.Create(new StringReader(File.ReadAllText("E:\\test.d")));
            DParser.OnError += new DParser.ErrorHandler(DParser_OnError);

            DModule n = dp.Parse();
            n.Name = n.ModuleName;

            Dump(n,"");

            return;
        }

        static void DParser_OnError(DModule tempModule, int line, int col, int kindOf, string message)
        {
            Console.WriteLine("Line "+line.ToString()+" Col "+col.ToString()+": "+message);
        }

        static void Dump(DNode n,string lev)
        {
            Console.WriteLine(lev+n.ToString());
            if (n is DBlockStatement)
            {
                Console.WriteLine(lev+"{");
                foreach (DNode ch in n as DBlockStatement)
                {
                    Dump(ch,lev+"  ");
                }
                Console.WriteLine(lev+"}");
            }
        }
    }
}
