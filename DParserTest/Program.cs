using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace D_Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            Scanner sc = new Scanner("E:\\test.d");

            DParser parser = new DParser(sc);

            parser.Parse();

            return;
        }
    }
}
