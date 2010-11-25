using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using D_Parser;
using D_Parser.NodeStorage;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace D_IDE
{
    class D_IDENodeStorage:BinaryNodeStorage
    {
        private D_IDENodeStorage(string file, bool WriteAccess)
            : base(file, WriteAccess)
        { }

        private D_IDENodeStorage(Stream stream, bool WriteAccess) : base(stream, WriteAccess) { }

        public static void WriteModules(string[] ParsedDirectories,List<CodeModule> Modules, string file)
        {
            var ns = new D_IDENodeStorage(file, true);

            ns.WriteModules(ParsedDirectories,Modules);
        }

        public static void WriteModules(string[] ParsedDirectories, List<CodeModule> Modules, Stream stream)
        {
            var ns = new D_IDENodeStorage(stream, true);

            ns.WriteModules(ParsedDirectories, Modules);
        }

        public static List<CodeModule> ReadModules(string file, ref List<string> ParsedDirectories)
        {
            var ns = new D_IDENodeStorage(file, false);

            return ns.ReadModules(ref ParsedDirectories);
        }

        public static List<CodeModule> ReadModules(Stream stream, ref List<string> ParsedDirectories)
        {
            var ns = new D_IDENodeStorage(stream, false);

            return ns.ReadModules(ref ParsedDirectories);
        }

        public void WriteModules(string[] ParsedDirectories, List<CodeModule> Modules)
        {
            var bs = BinWriter;

            bs.Write(Modules.Count); // To know how many modules we've saved

            if (ParsedDirectories != null)
            {
                bs.Write((uint)ParsedDirectories.Length);
                foreach (string dir in ParsedDirectories)
                    WriteString(dir,true);
            }
            else bs.Write((uint)0);

            foreach (var mod in Modules)
            {
                bs.Write(ModuleInitializer);
                WriteString(mod.ModuleName,true);
                WriteString(mod.ModuleFileName,true);
                WriteNodes(mod.Children);
                bs.Flush();
            }
        }

        public List<CodeModule> ReadModules(ref List<string> ParsedDirectories)
        {
            var bs = BinReader;

            // Module count
            int ModuleCount = bs.ReadInt32();

            var ret = new List<CodeModule>();

            // Parsed directories
            uint DirCount = bs.ReadUInt32();

            for (int i = 0; i < DirCount; i++)
            {
                string d = ReadString(true);
                if (!ParsedDirectories.Contains(d))
                    ParsedDirectories.Add(d);
            }

            for (int i = 0; i < ModuleCount; i++)
            {
                if (bs.ReadInt32() != ModuleInitializer)
                    throw new Exception("Wrong data format");

                string mod_name = ReadString(true);
                string mod_fn = ReadString(true);

                var cm = new CodeModule();
                //cm.Project = Project;
                cm.ModuleFileName = mod_fn;
                cm.ModuleName = mod_name;

                var bl = cm as DBlockStatement;
                ReadNodes(ref bl);

                ret.Add(cm);
            }

            return ret;
        }
    }
}
