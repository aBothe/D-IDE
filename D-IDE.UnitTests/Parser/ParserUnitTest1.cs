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
        public void ParseFunction()
        {
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("D_IDE.UnitTests.Resources.D.test_001_function.d"))
            {
                StreamReader reader = new StreamReader(stream);
                DModule syntaxTree = DParser.ParseString(reader.ReadToEnd()) as DModule;
                Assert.IsNotNull(syntaxTree, "The syntax tree was not instantiated or not of the right type!");
                string s = SerializeDNode(syntaxTree);
                TestContext.WriteLine(s);
            }
        }

        [TestMethod]
        public void ParseClass()
        {
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("D_IDE.UnitTests.Resources.D.test_002_class.d"))
            {
                StreamReader reader = new StreamReader(stream);
                DModule syntaxTree = DParser.ParseString(reader.ReadToEnd()) as DModule;
                Assert.IsNotNull(syntaxTree, "The syntax tree was not instantiated or not of the right type!");
                string s = SerializeDNode(syntaxTree);
                TestContext.WriteLine(s);
            }
        }

        protected string SerializeDNode(DNode node)
        {
            StringBuilder sb = new StringBuilder();
            SerializeDNode(node, sb, 0);
            return sb.ToString();
        }

        protected void SerializeDNode(DNode node, StringBuilder sb, int level)
        {
            StringBuilder indent = new StringBuilder();
            for (int i = 0; i < level; i++) indent.Append('\t');
            SerializeCodeLocation(node.StartLocation, sb);
            sb.Append("-");
            SerializeCodeLocation(node.EndLocation, sb);
            sb.Append(" ");

            if (node is DModule) { 
                DModule module = node as DModule;
                sb.Append(indent).Append("ML:").Append(module.ModuleName);
                foreach (ITypeDeclaration td in module.Imports.Keys)
                {
                    sb.Append(indent).Append("~IM:").Append(td.ToString());
                }
                sb.AppendLine();
            }
            else if (node is DMethod)
            {
                DMethod method = node as DMethod;
                sb.Append(indent).Append("MD:").Append(method.Type != null ? method.Type.ToString() : "<NULL>").Append("~").AppendLine(method.Name);
                foreach (DNode paramNode in method.Parameters)
                {
                    SerializeDNode(paramNode, sb, level+1);
                }
            }
            else if (node is DClassLike)
            {
                DClassLike clss = node as DClassLike;
                sb.Append(indent).Append("CLS:").Append(clss.ClassType).Append("~").AppendLine(clss.Name);
            }
            else if (node is DVariable)
            {
                DVariable variable = node as DVariable;
                sb.Append(indent).Append("VAR:").Append(variable.Type.ToString()).Append("~").Append(variable.Name);
                if (variable.Initializer != null)
                    sb.Append(indent).Append("~INIT:").Append(variable.Initializer.ToString());
                sb.AppendLine();
            }
            else
            {
                object o = node;
            }

            if (node is DStatementBlock)
            {
                DStatementBlock block = node as DStatementBlock;
                //if (block.Expression != null)
                //    sb.Append(indent).Append("EXP:").Append(block.Expression.ToString());
                foreach (DNode childNode in block)
                {
                    SerializeDNode(childNode, sb, level+1);
                }
            }
            else if (node is DBlockStatement)
            {
                DBlockStatement block = node as DBlockStatement;
                foreach (DNode childNode in block.Children)
                {
                    SerializeDNode(childNode, sb, level + 1);
                }
            }
        }

        protected void SerializeCodeLocation(CodeLocation loc, StringBuilder sb)
        {
            sb.Append("[").Append(loc.Column.ToString().PadLeft(5, '0')).Append(",")
                .Append(loc.Line.ToString().PadLeft(6, '0')).Append("]");
        }
        
        /*[TestMethod]
        public void ParsePhobos()
        {
            FileInfo[] files = phobosDir.GetFiles("*.d", SearchOption.AllDirectories);

            foreach (FileInfo f in files)
            {
                IAbstractSyntaxTree syntaxTree = DParser.ParseFile(f.FullName);
                Assert.IsNotNull(syntaxTree, "The syntax tree was not instantiated!");
            }
        }*/
    }
}
