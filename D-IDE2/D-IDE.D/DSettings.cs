using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.D
{
	public class DSettings
	{
		public static DSettings Instance=new DSettings();

		public readonly DMDConfig DMD1=new DMDConfig();
		public readonly DMDConfig DMD2=new DMDConfig();

		public DMDConfig DMDConfig(DVersion v)
		{
			if (v == DVersion.D1)
				return DMD1;
			return DMD2;
		}
	}

	public enum DVersion
	{
		D1=1,
		D2=2
	}

	public class DMDConfig
	{
		public class DBuildArguments
		{
			public string SoureCompiler;
			public string Win32ExeLinker;
			public string ExeLinker;
			public string DllLinker;
			public string LibLinker;
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

		DBuildArguments BuildArguments(bool IsDebug)
		{
			if (IsDebug)
				return DebugArgs;
			return ReleaseArgs;
		}

		DBuildArguments DebugArgs=new DBuildArguments(){
		SoureCompiler = "-c \"$src\" -of\"$obj\" -gc -debug",
		Win32ExeLinker = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -gc -debug",
		ExeLinker = "$objs $libs -of\"$exe\" -gc -debug",
		DllLinker = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc -debug",
		LibLinker = "-c -n \"$lib\" $objs"};

		DBuildArguments ReleaseArgs=new DBuildArguments(){
		SoureCompiler = "-c \"$src\" -of\"$obj\" -release -O -inline",
		Win32ExeLinker = "$objs $libs -L/su:windows -L/exet:nt -of\"$exe\" -release -O -inline",
		ExeLinker = "$objs $libs -of\"$exe\" -release -O -inline",
		DllLinker = "$objs $libs -L/IMPLIB:\"$lib\" -of\"$dll\" -release -O -inline",
		LibLinker = "-c -n \"$lib\" $objs"};
	}
}
