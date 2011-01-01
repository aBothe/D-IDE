using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
using System.IO;

namespace D_Parser
{
	public class DLanguage:ILanguage
	{
		public string DefaultFileExtension { get { return ".d"; } }

		public IParser CreateParser()
		{
			return new DParserWrapper();
		}

		public ICodeResolver CreateCodeResolver()
		{
			return new DCodeResolver();
		}

		public void SaveModuleCache(IAbstractSyntaxTree[] Modules, string[] ImportDirectories, string FileName)
		{
			// Take a memory stream due to its speed
			// -- A file stream would take to much time
			var ms = new MemoryStream();

			var bns = new BinaryNodeStorage(ms, true);
			var bs = bns.BinWriter;
			bs.Write(Modules.Length); // To know how many modules we've saved

			if (ImportDirectories != null)
			{
				bs.Write((uint)ImportDirectories.Length);
				foreach (string dir in ImportDirectories)
					bns.WriteString(dir, true);
			}
			else bs.Write((uint)0);

			foreach (var mod in Modules)
			{
				bs.Write(BinaryNodeStorage.ModuleInitializer);
				bns.WriteString(mod.ModuleName, true);
				bns.WriteString(mod.FileName, true);
				bns.WriteNodes(mod.Children);
				bs.Flush();
			}

			File.WriteAllBytes(FileName, ms.ToArray());
			ms.Close();
		}

		public void SaveModuleCache(IAbstractSyntaxTree[] Modules, string FileName)
		{
			SaveModuleCache(Modules, null, FileName);
		}

		public IAbstractSyntaxTree[] LoadModuleCache(string FileName, out string[] ImportDirectories)
		{
			var bns = new BinaryNodeStorage(FileName, false);
			var bs = bns.BinReader;

			// Module count
			int ModuleCount = bs.ReadInt32();

			var ret = new List<IAbstractSyntaxTree>();
			var imps = new List<string>();

			// Parsed directories
			uint DirCount = bs.ReadUInt32();

			for (int i = 0; i < DirCount; i++)
			{
				string d = bns.ReadString(true);
				if (!imps.Contains(d))
					imps.Add(d);
			}
			ImportDirectories = imps.ToArray();

			for (int i = 0; i < ModuleCount; i++)
			{
				if (bs.ReadInt32() != BinaryNodeStorage.ModuleInitializer)
					throw new Exception("Wrong data format");

				string mod_name = bns.ReadString(true);
				string mod_fn = bns.ReadString(true);

				var cm = new DModule();
				//cm.Project = Project;
				cm.FileName = mod_fn;
				cm.ModuleName = mod_name;

				var bl = cm as IBlockNode;
				bns.ReadNodes(ref bl);

				ret.Add(cm);
			}

			return ret.ToArray();
		}

		public IAbstractSyntaxTree[] LoadModuleCache(string FileName)
		{
			string[] imps = null;
			return LoadModuleCache(FileName, out imps);
		}
	}
}
