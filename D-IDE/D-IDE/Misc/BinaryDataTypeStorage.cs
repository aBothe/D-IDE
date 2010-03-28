using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using D_Parser;
using ICSharpCode.NRefactory.Ast;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using ICSharpCode.NRefactory;

namespace D_IDE
{
	class BinaryDataTypeStorageWriter
	{
		#region General
		public BinaryWriter BinStream;
		public const uint ModuleInitializer=(uint)('D')|('M'<<8)|('o'<<16)|('d'<<24);
		public const uint NodeInitializer = (uint)('N') | ('o' << 8) | ('d' << 16) | ('e'<<24);
		public BinaryDataTypeStorageWriter(string file)
		{
			FileStream fs=new FileStream(file,FileMode.Create,FileAccess.Write);
			BinStream = new BinaryWriter(fs);
		}

		public void Close()
		{
			if (BinStream != null)
			{
				BinStream.Flush();
				BinStream.Close();
			}
		}

		/// <summary>
		/// This special method is needed because Stream.Write("testString") limits the string length to 128 (7-bit ASCII) although we want to have at least 255
		/// </summary>
		/// <param name="s"></param>
		void WriteString(string s)
		{
			if (String.IsNullOrEmpty(s))
			{
				BinStream.Write((ushort)0);
				return;
			}
			if (s.Length >= ushort.MaxValue) s = s.Remove(ushort.MaxValue-1);
			BinStream.Write((ushort)s.Length); // short = 2 bytes; byte = 1 byte
			BinStream.Write(Encoding.UTF8.GetBytes(s));
		}
		#endregion

		#region Modules
		public void WriteModules(DModule[] Modules) { WriteModules(new List<DModule>(Modules)); }

		public void WriteModules(List<DModule> Modules)
		{
			BinaryWriter bs = BinStream;

			bs.Write(Modules.Count); // To know how many modules we've saved

			foreach (DModule mod in Modules)
			{
				bs.Write(ModuleInitializer);
				WriteString(mod.ModuleName);
				WriteString(mod.mod_file);
				WriteNodes(mod.Children);
				bs.Flush();
			}
		}
		#endregion

		#region Nodes
		void WriteNodes(List<INode> Nodes)
		{
			BinaryWriter bs = BinStream;

			if (Nodes == null || Nodes.Count < 1)
			{
				bs.Write((int)0);
				bs.Flush();
				return;
			}

			bs.Write(Nodes.Count);

			foreach (INode n in Nodes)
			{
				DataType dt = n as DataType;
				bs.Write(NodeInitializer);

				bs.Write((int)dt.fieldtype);
				WriteString(dt.name);
				bs.Write((int)dt.TypeToken);
				WriteString(dt.type);
				bs.Write(dt.StartLocation.X);
				bs.Write(dt.StartLocation.Y);
				bs.Write(dt.EndLocation.X);
				bs.Write(dt.EndLocation.Y);

				bs.Write(dt.modifiers.Count);
				foreach (int mod in dt.modifiers)
					bs.Write(mod);

				WriteString(dt.module);
				WriteString(dt.value);
				WriteString(dt.superClass);
				WriteString(dt.implementedInterface);

				WriteNodes(dt.param);
				WriteNodes(dt.Children);
			}

			bs.Flush();
		}
		#endregion
	}



	class BinaryDataTypeStorageReader
	{
		#region General
		public BinaryReader BinStream;
		public BinaryDataTypeStorageReader(string file)
		{
			FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
			BinStream = new BinaryReader(fs);
		}

		string ReadString()
		{
			int len = (int) BinStream.ReadUInt16();
			if (len < 1) return String.Empty;
			byte[] t = BinStream.ReadBytes(len);
			return Encoding.UTF8.GetString(t);
		}

		public void Close()
		{
			if (BinStream != null)
			{
				BinStream.Close();
			}
		}
		#endregion

		#region Modules
		public List<DModule> ReadModules()
		{
			BinaryReader bs = BinStream;

			int Count = bs.ReadInt32();
			List<DModule> ret = new List<DModule>(Count); // Speed improvement caused by given number of modules

			for (int i = 0; i < Count;i++ )
			{
				uint mi=bs.ReadUInt32();
				if (mi != BinaryDataTypeStorageWriter.ModuleInitializer)
				{
					throw new Exception("Wrong format!");
				}

				DModule mod = new DModule();
				mod.ModuleName = ReadString();
				mod.mod_file = ReadString();
				ReadNodes(ref mod.dom.children);
				ret.Add(mod);
			}
			return ret;
		}
		#endregion

		#region Nodes
		void ReadNodes(ref List<INode> Nodes)
		{
			BinaryReader bs = BinStream;

			int Count = bs.ReadInt32();
			Nodes.Capacity = Count;
			Nodes.Clear();

			for (int i = 0; i < Count;i++ )
			{
				uint ni = bs.ReadUInt32();
				if (ni != BinaryDataTypeStorageWriter.NodeInitializer)
				{
					throw new Exception("Wrong format!");
				}

				DataType dt = new DataType();

				dt.fieldtype = (FieldType)bs.ReadInt32();
				dt.name = ReadString();
				dt.TypeToken = bs.ReadInt32();
				dt.type = ReadString();
				Location startLoc = new Location();
				startLoc.X = bs.ReadInt32();
				startLoc.Y = bs.ReadInt32();
				dt.StartLocation = startLoc;
				Location endLoc = new Location();
				endLoc.X = bs.ReadInt32();
				endLoc.Y = bs.ReadInt32();
				dt.EndLocation = endLoc;

				int modCount = bs.ReadInt32();
				for (int j = 0; j < modCount; j++)
					dt.modifiers.Add(bs.ReadInt32());

				dt.module = ReadString();
				dt.value = ReadString();
				dt.superClass = ReadString();
				dt.implementedInterface = ReadString();

				ReadNodes(ref dt.param);
				ReadNodes(ref dt.children);

				Nodes.Add(dt);
			}
		}
		#endregion
	}
}
