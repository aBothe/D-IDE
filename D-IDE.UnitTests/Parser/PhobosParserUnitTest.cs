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
        public void ParseStdAlgorithm() { SimpleFileTest(phobosDir.FullName + @"\std\algorithm.d", null); }

        [TestMethod]
        public void ParseStdArray() { SimpleFileTest(phobosDir.FullName + @"\std\array.d", null); }

        [TestMethod]
        public void ParseStdAtomics() { SimpleFileTest(phobosDir.FullName + @"\std\atomics.d", null); }

        [TestMethod]
        public void ParseStdBase64() { SimpleFileTest(phobosDir.FullName + @"\std\base64.d", null); }

        [TestMethod]
        public void ParseStdBigint() { SimpleFileTest(phobosDir.FullName + @"\std\bigint.d", null); }

        [TestMethod]
        public void ParseStdBind() { SimpleFileTest(phobosDir.FullName + @"\std\bind.d", null); }

        [TestMethod]
        public void ParseStdBitarray() { SimpleFileTest(phobosDir.FullName + @"\std\bitarray.d", null); }

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
        public void ParseStdCtype() { SimpleFileTest(phobosDir.FullName + @"\std\ctype.d", null); }

        [TestMethod]
        public void ParseStdDate() { SimpleFileTest(phobosDir.FullName + @"\std\date.d", null); }

        [TestMethod]
        public void ParseStdDatebase() { SimpleFileTest(phobosDir.FullName + @"\std\datebase.d", null); }

        [TestMethod]
        public void ParseStdDateparse() { SimpleFileTest(phobosDir.FullName + @"\std\dateparse.d", null); }

        [TestMethod]
        public void ParseStdDatetime() { SimpleFileTest(phobosDir.FullName + @"\std\datetime.d", null); }

        [TestMethod]
        public void ParseStdDemangle() { SimpleFileTest(phobosDir.FullName + @"\std\demangle.d", null); }

        [TestMethod]
        public void ParseStdEncoding() { SimpleFileTest(phobosDir.FullName + @"\std\encoding.d", null); }

        [TestMethod]
        public void ParseStdException() { SimpleFileTest(phobosDir.FullName + @"\std\exception.d", null); }

        [TestMethod]
        public void ParseStdFile() { SimpleFileTest(phobosDir.FullName + @"\std\file.d", null); }

        [TestMethod]
        public void ParseStdFormat() { SimpleFileTest(phobosDir.FullName + @"\std\format.d", null); }

        [TestMethod]
        public void ParseStdFunctional() { SimpleFileTest(phobosDir.FullName + @"\std\functional.d", null); }

        [TestMethod]
        public void ParseStdGetopt() { SimpleFileTest(phobosDir.FullName + @"\std\getopt.d", null); }

        [TestMethod]
        public void ParseStdGregorian() { SimpleFileTest(phobosDir.FullName + @"\std\gregorian.d", null); }

        [TestMethod]
        public void ParseStdInternal() { SimpleFileTest(phobosDir.FullName + @"\std\internal", null); }

        [TestMethod]
        public void ParseStdIntrinsic() { SimpleFileTest(phobosDir.FullName + @"\std\intrinsic.d", null); }

        [TestMethod]
        public void ParseStdIterator() { SimpleFileTest(phobosDir.FullName + @"\std\iterator.d", null); }

        [TestMethod]
        public void ParseStdJson() { SimpleFileTest(phobosDir.FullName + @"\std\json.d", null); }

        [TestMethod]
        public void ParseStdLoader() { SimpleFileTest(phobosDir.FullName + @"\std\loader.d", null); }

        [TestMethod]
        public void ParseStdMath() { SimpleFileTest(phobosDir.FullName + @"\std\math.d", null); }

        [TestMethod]
        public void ParseStdMathspecial() { SimpleFileTest(phobosDir.FullName + @"\std\mathspecial.d", null); }

        [TestMethod]
        public void ParseStdMd5() { SimpleFileTest(phobosDir.FullName + @"\std\md5.d", null); }

        [TestMethod]
        public void ParseStdMetastrings() { SimpleFileTest(phobosDir.FullName + @"\std\metastrings.d", null); }

        [TestMethod]
        public void ParseStdMmfile() { SimpleFileTest(phobosDir.FullName + @"\std\mmfile.d", null); }

        [TestMethod]
        public void ParseStdNumeric() { SimpleFileTest(phobosDir.FullName + @"\std\numeric.d", null); }

        [TestMethod]
        public void ParseStdOpenrj() { SimpleFileTest(phobosDir.FullName + @"\std\openrj.d", null); }

        [TestMethod]
        public void ParseStdOutbuffer() { SimpleFileTest(phobosDir.FullName + @"\std\outbuffer.d", null); }

        [TestMethod]
        public void ParseStdPath() { SimpleFileTest(phobosDir.FullName + @"\std\path.d", null); }

        [TestMethod]
        public void ParseStdPerf() { SimpleFileTest(phobosDir.FullName + @"\std\perf.d", null); }

        [TestMethod]
        public void ParseStdProcess() { SimpleFileTest(phobosDir.FullName + @"\std\process.d", null); }

        [TestMethod]
        public void ParseStdRandom() { SimpleFileTest(phobosDir.FullName + @"\std\random.d", null); }

        [TestMethod]
        public void ParseStdRange() { SimpleFileTest(phobosDir.FullName + @"\std\range.d", null); }

        [TestMethod]
        public void ParseStdRegex() { SimpleFileTest(phobosDir.FullName + @"\std\regex.d", null); }

        [TestMethod]
        public void ParseStdRegexp() { SimpleFileTest(phobosDir.FullName + @"\std\regexp.d", null); }

        [TestMethod]
        public void ParseStdSignals() { SimpleFileTest(phobosDir.FullName + @"\std\signals.d", null); }

        [TestMethod]
        public void ParseStdSocket() { SimpleFileTest(phobosDir.FullName + @"\std\socket.d", null); }

        [TestMethod]
        public void ParseStdSocketstream() { SimpleFileTest(phobosDir.FullName + @"\std\socketstream.d", null); }

        [TestMethod]
        public void ParseStdStdarg() { SimpleFileTest(phobosDir.FullName + @"\std\stdarg.d", null); }

        [TestMethod]
        public void ParseStdStdint() { SimpleFileTest(phobosDir.FullName + @"\std\stdint.d", null); }

        [TestMethod]
        public void ParseStdStdio() { SimpleFileTest(phobosDir.FullName + @"\std\stdio.d", null); }

        [TestMethod]
        public void ParseStdStdiobase() { SimpleFileTest(phobosDir.FullName + @"\std\stdiobase.d", null); }

        [TestMethod]
        public void ParseStdStream() { SimpleFileTest(phobosDir.FullName + @"\std\stream.d", null); }

        [TestMethod]
        public void ParseStdString() { SimpleFileTest(phobosDir.FullName + @"\std\string.d", null); }

        [TestMethod]
        public void ParseStdSyserror() { SimpleFileTest(phobosDir.FullName + @"\std\syserror.d", null); }

        [TestMethod]
        public void ParseStdSystem() { SimpleFileTest(phobosDir.FullName + @"\std\system.d", null); }

        [TestMethod]
        public void ParseStdTraits() { SimpleFileTest(phobosDir.FullName + @"\std\traits.d", null); }

        [TestMethod]
        public void ParseStdTypecons() { SimpleFileTest(phobosDir.FullName + @"\std\typecons.d", null); }

        [TestMethod]
        public void ParseStdTypetuple() { SimpleFileTest(phobosDir.FullName + @"\std\typetuple.d", null); }

        [TestMethod]
        public void ParseStdUni() { SimpleFileTest(phobosDir.FullName + @"\std\uni.d", null); }

        [TestMethod]
        public void ParseStdUri() { SimpleFileTest(phobosDir.FullName + @"\std\uri.d", null); }

        [TestMethod]
        public void ParseStdUtf() { SimpleFileTest(phobosDir.FullName + @"\std\utf.d", null); }

        [TestMethod]
        public void ParseStdVariant() { SimpleFileTest(phobosDir.FullName + @"\std\variant.d", null); }

        [TestMethod]
        public void ParseStdWindows() { SimpleFileTest(phobosDir.FullName + @"\std\windows", null); }

        [TestMethod]
        public void ParseStdXml() { SimpleFileTest(phobosDir.FullName + @"\std\xml.d", null); }

        [TestMethod]
        public void ParseStdZip() { SimpleFileTest(phobosDir.FullName + @"\std\zip.d", null); }

        [TestMethod]
        public void ParseStdZlib() { SimpleFileTest(phobosDir.FullName + @"\std\zlib.d", null); }

        [TestMethod]
        public void ParseStd__Fileinit() { SimpleFileTest(phobosDir.FullName + @"\std\__fileinit.d", null); }

    }
}