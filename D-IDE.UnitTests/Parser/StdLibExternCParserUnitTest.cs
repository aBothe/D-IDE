using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using D_Parser;
using Parser.Core;
using System.IO;

namespace D_IDE.UnitTests.Parser
{
    /// <summary>
    /// A spot for parser unit tests.
    /// </summary>
    [TestClass]
    public class StdLibExternCParserUnitTes : BaseParserUnitTest
    {

        [ClassInitialize]
        public static void Initialize(TestContext testContextInstance)
        {
            _Initialize(testContextInstance);
        }

        [ClassCleanup()]
        public static void Cleanup()
        {
            _Cleanup();
        }

        [TestMethod]
        public void ParseEtcCZlib()
        {
            SimpleFileTest(phobosDir.FullName + @"\etc\c\zlib.d", null);
        }

        [TestMethod]
        public void ParseStdCWindowsStat()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\windows\stat.d", null);
        }

        [TestMethod]
        public void ParseStdCWindowsWindows()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\windows\windows.d", null);
        }

        [TestMethod]
        public void ParseStdCWindowsWinsock()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\windows\winsock.d", null);
        }

        [TestMethod]
        public void ParseStdCFenv()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\fenv.d", null);
        }

        [TestMethod]
        public void ParseStdCLocale()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\locale.d", null);
        }

        [TestMethod]
        public void ParseStdCMath()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\math.d", null);
        }

        [TestMethod]
        public void ParseStdCProcess()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\process.d", null);
        }

        [TestMethod]
        public void ParseStdCStdarg()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\stdarg.d", null);
        }

        [TestMethod]
        public void ParseStdCStddef()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\stddef.d", null);
        }

        [TestMethod]
        public void ParseStdCStdio()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\stdio.d", null);
        }

        [TestMethod]
        public void ParseStdCStdlib()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\stdlib.d", null);
        }

        [TestMethod]
        public void ParseStdCString()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\string.d", null);
        }

        [TestMethod]
        public void ParseStdCTime()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\time.d", null);
        }

        [TestMethod]
        public void ParseStdCWcharh()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\c\wcharh.d", null);
        }
    }
}
