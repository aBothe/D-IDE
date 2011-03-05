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
    public class PhobosParserUnitTest : BaseParserUnitTest
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
        public void ParseStdAlgorithm()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\algorithm.d", null);
        }

        [TestMethod]
        public void ParseStdArray()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\array.d", null);
        }

        [TestMethod]
        public void ParseStdAtomics()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\atomics.d", null);
        }

        [TestMethod]
        public void ParseStdBase64()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\base64.d", null);
        }

        [TestMethod]
        public void ParseStdBigint()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\bigint.d", null);
        }

        [TestMethod]
        public void ParseStdBind()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\bind.d", null);
        }

        [TestMethod]
        public void ParseStdBitarray()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\bitarray.d", null);
        }

        [TestMethod]
        public void ParseStdBitmanip() { SimpleFileTest(phobosDir.FullName + @"\std\bitmanip.d", null); }

        [TestMethod]
        public void ParseStdBoxer() { SimpleFileTest(phobosDir.FullName + @"\std\boxer.d", null); }

        [TestMethod]
        public void ParseStdCompiler() { SimpleFileTest(phobosDir.FullName + @"\std\compiler.d", null); }

        [TestMethod]
        public void ParseStdComplex() { SimpleFileTest(phobosDir.FullName + @"\std\complex.d", null); }

        [TestMethod]
        public void ParseStdConcurrency() { SimpleFileTest(phobosDir.FullName + @"\std\concurrency.d", null); }

        [TestMethod]
        public void ParseStdContainer() { SimpleFileTest(phobosDir.FullName + @"\std\container.d", null); }

        [TestMethod]
        public void ParseStdContracts() { SimpleFileTest(phobosDir.FullName + @"\std\contracts.d", null); }

        [TestMethod]
        public void ParseStdConv() { SimpleFileTest(phobosDir.FullName + @"\std\conv.d", null); }

        [TestMethod]
        public void ParseStdCpuid() { SimpleFileTest(phobosDir.FullName + @"\std\cpuid.d", null); }

        [TestMethod]
        public void ParseStdCstream() { SimpleFileTest(phobosDir.FullName + @"\std\cstream.d", null); }

        [TestMethod]
        public void ParseStdCtype()
        {
            SimpleFileTest(phobosDir.FullName + @"\std\ctype.d", null);

            /*
    date.d
    datebase.d
    dateparse.d
    datetime.d
    demangle.d
    encoding.d
    exception.d
    file.d
    format.d
    functional.d
    getopt.d
    gregorian.d
    internal
    intrinsic.d
    iterator.d
    json.d
    loader.d
    math.d
    mathspecial.d
    md5.d
    metastrings.d
    mmfile.d
    numeric.d
    openrj.d
    outbuffer.d
    path.d
    perf.d
    process.d
    random.d
    range.d
    regex.d
    regexp.d
    signals.d
    socket.d
    socketstream.d
    stdarg.d
    stdint.d
    stdio.d
    stdiobase.d
    stream.d
    string.d
    syserror.d
    system.d
    traits.d
    typecons.d
    typetuple.d
    uni.d
    uri.d
    utf.d
    variant.d
    windows
    xml.d
    zip.d
    zlib.d
    __fileinit.d
            }*/
        }
    }
}