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
    public class ParserUnitTest1 : BaseParserUnitTest
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
        public void ParseSandbox()
        {
            SimpleResourceTest("test_000_sandbox", null);
        }

        [TestMethod]
        public void ParseFunction()
        {
            SimpleResourceTest("test_001_function", null);
        }

        [TestMethod]
        public void ParseClass()
        {
            SimpleResourceTest("test_002_class", null);
        }

        [TestMethod]
        public void ParsePrimitiveLiterals()
        {
            SimpleResourceTest("test_003_literals", null);
        }

        [TestMethod]
        public void ParsePrimitiveOperators()
        {
            SimpleResourceTest("test_004_operators", null);
        }
    }
}
