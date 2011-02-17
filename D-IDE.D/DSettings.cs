using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Xml;

namespace D_IDE.D
{
	public class DSettings
	{
		public static DSettings Instance=new DSettings();

		public DMDConfig dmd1 = new DMDConfig() { Version=DVersion.D1};
		public DMDConfig dmd2 = new DMDConfig() { Version=DVersion.D2};

		public DMDConfig DMDConfig(DVersion v)
		{
			if (v == DVersion.D1)
				return dmd1;
			return dmd2;
		}

		public string cv2pdb_exe = "cv2pdb.exe";

		#region Saving&Loading
		public void Save(XmlWriter x)
		{
			x.WriteStartDocument();

			x.WriteStartElement("dsettings");

			x.WriteStartElement("cv2pdb");
			x.WriteCData(cv2pdb_exe);
			x.WriteEndElement();

			dmd1.Save(x);
			dmd2.Save(x);

			x.WriteEndElement();
		}

		public void Load(XmlReader x)
		{
			while (x.Read())
			{
				switch (x.LocalName)
				{
					case "cv2pdb":
						cv2pdb_exe = x.ReadString();
						break;

					case "dmd":
						var config = new DMDConfig();
						config.Load(x);

						if (config.Version == DVersion.D1)
							dmd1 = config;
						else
							dmd2 = config;
						break;
				}
			}
		}
		#endregion
	}

	public enum DVersion:int
	{
		D1=1,
		D2=2
	}

	public class DMDConfig
	{
		public class DBuildArguments
		{
			public bool IsDebug = false;

			public string SoureCompiler;
			public string Win32ExeLinker;
			public string ExeLinker;
			public string DllLinker;
			public string LibLinker;

			public void Load(XmlReader x)
			{
				if (x.LocalName != "buildarguments")
					return;

				if (x.MoveToAttribute("IsDebug"))
					bool.TryParse(x.GetAttribute("IsDebug"),out IsDebug);
				x.MoveToElement();

				var x2 = x.ReadSubtree();
				while (x2.Read())
				{
					switch (x2.LocalName)
					{
						case "sourcecompiler":
							SoureCompiler = x2.ReadString();
							break;
						case "win32linker":
							Win32ExeLinker = x2.ReadString();
							break;
						case "exelinker":
							ExeLinker = x2.ReadString();
							break;
						case "dlllinker":
							DllLinker = x2.ReadString();
							break;
						case "liblinker":
							LibLinker = x2.ReadString();
							break;
					}
				}
			}

			public void Save(XmlWriter x)
			{
				x.WriteStartElement("buildarguments");
				x.WriteAttributeString("IsDebug",IsDebug.ToString());

				x.WriteStartElement("sourcecompiler");
				x.WriteCData(SoureCompiler);
				x.WriteEndElement();

				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();

				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();

				x.WriteEndElement();
			}

			public void ApplyFrom(DBuildArguments other)
			{
				IsDebug = other.IsDebug;
				SoureCompiler = other.SoureCompiler;
				Win32ExeLinker = other.Win32ExeLinker;
				ExeLinker = other.ExeLinker;
				DllLinker = other.DllLinker;
				LibLinker = other.LibLinker;
			}
		}

		public DVersion Version = DVersion.D2;

		/// <summary>
		/// TODO: Change default to C:\dmd2\windows\bin
		/// </summary>
		public string BaseDirectory = @"D:\dmd2\windows\bin";

		public string SoureCompiler = "dmd.exe";
		public string ExeLinker = "dmd.exe";
		public string Win32ExeLinker = "dmd.exe";
		public string DllLinker = "dmd.exe";
		public string LibLinker = "lib.exe";

		public DBuildArguments BuildArguments(bool IsDebug)
		{
			if (IsDebug)
				return DebugArgs;
			return ReleaseArgs;
		}

		public DBuildArguments DebugArgs=new DBuildArguments(){
		IsDebug=true,
		SoureCompiler = "-c \"$src\" -of\"$obj\" -gc -debug",
		Win32ExeLinker = "$objs -L/su:windows -L/exet:nt -of\"$exe\" -gc -debug",
		ExeLinker = "$objs -of\"$exe\" -gc -debug",
		DllLinker = "$objs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc -debug",
		LibLinker = "-c -n \"$lib\" $objs"};

		public DBuildArguments ReleaseArgs=new DBuildArguments(){
		SoureCompiler = "-c \"$src\" -of\"$obj\" -release -O -inline",
		Win32ExeLinker = "$objs -L/su:windows -L/exet:nt -of\"$exe\" -release -O -inline",
		ExeLinker = "$objs -of\"$exe\" -release -O -inline",
		DllLinker = "$objs -L/IMPLIB:\"$lib\" -of\"$dll\" -release -O -inline",
		LibLinker = "-c -n \"$lib\" $objs"};

		public void Load(XmlReader x)
		{
			if (x.LocalName != "dmd")
				return;

			if(x.MoveToAttribute("version"))
				Version = (DVersion)Convert.ToInt32( x.GetAttribute("version"));
			x.MoveToElement();

			var x2 = x.ReadSubtree();
			while (x2.Read())
			{
				switch (x2.LocalName)
				{
					case "basedirectory":
						BaseDirectory = x2.ReadString();
						break;
					case "sourcecompiler":
						SoureCompiler = x2.ReadString();
						break;
					case "exelinker":
						ExeLinker = x2.ReadString();
						break;
					case "win32linker":
						Win32ExeLinker = x2.ReadString();
						break;
					case "dlllinker":
						DllLinker = x2.ReadString();
						break;
					case "liblinker":
						LibLinker = x2.ReadString();
						break;

					case "buildarguments":
						var args = new DBuildArguments();
						args.Load(x2);
						if (args.IsDebug)
							DebugArgs = args;
						else 
							ReleaseArgs = args;
						break;
				}
			}
		}

		public void Save(XmlWriter x)
		{
			x.WriteStartElement("dmd");
			x.WriteAttributeString("version",((int)Version).ToString());

			if (!string.IsNullOrEmpty(BaseDirectory)){
				x.WriteStartElement("basedirectory");
				x.WriteCData(BaseDirectory);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(SoureCompiler)){
			x.WriteStartElement("sourcecompiler");
			x.WriteCData(SoureCompiler);
			x.WriteEndElement();
			} 
			if (!string.IsNullOrEmpty(ExeLinker)){
				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(Win32ExeLinker))
			{
				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(DllLinker))
			{
				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(LibLinker))
			{
				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();
			}
			DebugArgs.Save(x);
			ReleaseArgs.Save(x);

			x.WriteEndElement();
		}
	}
}
