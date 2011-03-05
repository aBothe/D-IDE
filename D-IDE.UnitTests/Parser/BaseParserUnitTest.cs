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
    public abstract class BaseParserUnitTest
    {
        protected static bool currentResourceIsFile;
        protected static string currentResource;
        protected static Dictionary<int, string> currentErrors = new Dictionary<int,string>();
        protected static DirectoryInfo phobosDir;
        protected static DirectoryInfo dmdDir;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public virtual TestContext TestContext
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

        protected static void _Initialize(TestContext testContextInstance)
        {
            DParser.OnError += delegate(IAbstractSyntaxTree syntaxTree, int line, int col, int kindOf, string message)
            {
                StringBuilder msg = new StringBuilder();
                if (currentErrors.ContainsKey(line)) msg.AppendLine(currentErrors[line]);
                msg.Append("File \"").Append(currentResource ?? "<NULL>").Append("\" - (Ln ").Append(line)
                    .Append(", Col ").Append(col).Append(") [").Append(kindOf).Append("] ").Append(message);

                currentErrors[line] = msg.ToString();
            };

            dmdDir = new DirectoryInfo(D.DSettings.Instance.dmd2.BaseDirectory);
            if (!dmdDir.Exists)
            {
                dmdDir = new DirectoryInfo(@"c:\d\dmd2");
                D.DSettings.Instance.dmd2.BaseDirectory = dmdDir.FullName;
            }
            phobosDir = new DirectoryInfo(D.DSettings.Instance.dmd2.BaseDirectory + @"\src\phobos");
        }

        protected static void _Cleanup() { }

        protected void SimpleResourceTest(string dFile, string tokenFile)
        {
            currentResourceIsFile = false;
            currentErrors.Clear();
            string resource = "D_IDE.UnitTests.Resources.D." + dFile + ".d";
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(resource))
            {
                currentResource = resource;
                StreamReader reader = new StreamReader(stream);
                DModule syntaxTree = DParser.ParseString(reader.ReadToEnd()) as DModule;
                testComplete(syntaxTree);
            }
        }

        protected void SimpleFileTest(string dFile, string tokenFile)
        {
            currentResourceIsFile = true;
            currentErrors.Clear();
            currentResource = dFile;
            using (StreamReader reader = new StreamReader(File.OpenRead(dFile)))
            {
                string l = reader.ReadLine();
                if (l.Equals("DDoc", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception("Cannot parse a DDoc file!");
                }
            }

            DModule syntaxTree = DParser.ParseFile(currentResource) as DModule;
            testComplete(syntaxTree);
        }

        protected void testComplete(DModule syntaxTree)
        {
            bool isSuccessful = false;
            if (currentResource != null)
            {
                isSuccessful = (currentErrors.Count == 0);

                if (!isSuccessful)
                {
                    StringBuilder errors = new StringBuilder();
                    foreach (int lineNumber in currentErrors.Keys)
                    {
                        errors.AppendLine(currentErrors[lineNumber]);

                        StringBuilder line = new StringBuilder();
                        line.Append(lineNumber.ToString().PadLeft(6, ' ')).Append(": ")
                            .Append(currentResourceIsFile ? fetchLineFile(currentResource, lineNumber) : fetchLineResource(currentResource, lineNumber));

                        TestContext.WriteLine(line.ToString().Replace("{", "[").Replace("}", "]"));
                    }
                    throw new AssertFailedException(errors.ToString());
                }

                Assert.IsNotNull(syntaxTree, "The syntax tree was not instantiated or not of the right type!");
                TestContext.WriteLine(SerializeDNode(syntaxTree).Replace("{", "[").Replace("}", "]"));
            }
        }
 

        private string fetchLineResource(string resourceName, int lineNumber)
        {
            return fetchLine(this.GetType().Assembly.GetManifestResourceStream(resourceName), lineNumber);
        }

        private string fetchLineFile(string fileName, int lineNumber)
        {
            return fetchLine(File.OpenRead(fileName), lineNumber);
        }

        private string fetchLine(Stream s, int lineNumber) 
        {
            int i = 0;
            string line = null;
            using (StreamReader reader = new StreamReader(s))
            {
                do
                {
                    line = reader.ReadLine();
                    if (++i == lineNumber) break;

                } while (line != null);
            }
            return line;
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

            if (node is DModule)
            {
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
                    SerializeDNode(paramNode, sb, level + 1);
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
                sb.Append(indent).Append("VAR:").Append((variable.Type == null) ? "<NULL>" : variable.Type.ToString()).Append("~").Append(variable.Name);
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
                    SerializeDNode(childNode, sb, level + 1);
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
    }
}
