using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using D_Parser;
using Parser.Core;
using System.IO;

namespace D_IDE.UnitTests
{
    /// <summary>
    /// A spot for parser unit tests.
    /// </summary>
    [TestClass]
    public class ParserUnitTest1
    {
        protected static DirectoryInfo phobosDir;
        protected static DirectoryInfo dmdDir;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContextInstance) 
        {
            DParser.OnError += delegate(IAbstractSyntaxTree syntaxTree, int line, int col, int kindOf, string message) 
                {
                    Assert.Fail("File \"" + syntaxTree.FileName + "\" - (Ln "+line+", Col " + col + ") [" + kindOf + "] " + message);
                };

            dmdDir = new DirectoryInfo(D.DSettings.Instance.dmd2.BaseDirectory);
            if (!dmdDir.Exists)
            {
                dmdDir = new DirectoryInfo(@"c:\d\dmd2");
                D.DSettings.Instance.dmd2.BaseDirectory = dmdDir.FullName;
            }
            phobosDir = new DirectoryInfo(D.DSettings.Instance.dmd2.BaseDirectory + @"\src\phobos");
        }

        [ClassCleanup()]
        public static void Cleanup() { }
        
        [TestMethod]
        public void ParsePhobos()
        {
            FileInfo[] files = phobosDir.GetFiles("*.d", SearchOption.AllDirectories);

            foreach (FileInfo f in files)
            {
                IAbstractSyntaxTree syntaxTree = DParser.ParseFile(f.FullName);
                Assert.IsNotNull(syntaxTree, "The syntax tree was not instantiated!");
            }
        }
    }
}
