using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Dom.Statements;
using D_Parser.Parser;

namespace D_Parser.Completion
{
	internal class AttributeCompletionProvider : AbstractCompletionProvider
	{
		public DAttribute Attribute;

		public AttributeCompletionProvider(ICompletionDataGenerator gen) : base(gen) { }

		protected override void BuildCompletionDataInternal(IEditorData Editor, string EnteredText)
		{
			if (Attribute is DeclarationCondition)
			{
				var c = Attribute as DeclarationCondition;

				if (c.IsVersionCondition)
				{
					foreach (var kv in new Dictionary<string, string>{
						{"DigitalMars","DMD (Digital Mars D) is the compiler"},
						{"GNU","GDC (GNU D Compiler) is the compiler"},
						{"LDC","LDC (LLVM D Compiler) is the compiler"},
						{"SDC","SDC (Stupid D Compiler) is the compiler"},
						{"D_NET","D.NET is the compiler"},
						{"Windows","Microsoft Windows systems"},
						{"Win32","Microsoft 32-bit Windows systems"},
						{"Win64","Microsoft 64-bit Windows systems"},
						{"linux","All Linux systems"},
						{"OSX","Mac OS X"},
						{"FreeBSD","FreeBSD"},
						{"OpenBSD","OpenBSD"},
						{"BSD","All other BSDs"},
						{"Solaris","Solaris"},
						{"Posix","All POSIX systems (includes Linux, FreeBSD, OS X, Solaris, etc.)"},
						{"AIX","IBM Advanced Interactive eXecutive OS"},
						{"SkyOS","The SkyOS operating system"},
						{"SysV3","System V Release 3"},
						{"SysV4","System V Release 4"},
						{"Hurd","GNU Hurd"},
						{"Cygwin","The Cygwin environment"},
						{"MinGW","The MinGW environment"},
						{"X86","Intel and AMD 32-bit processors"},
						{"X86_64","AMD and Intel 64-bit processors"},
						{"ARM","The Advanced RISC Machine architecture (32-bit)"},
						{"PPC","The PowerPC architecture, 32-bit"},
						{"PPC64","The PowerPC architecture, 64-bit"},
						{"IA64","The Itanium architecture (64-bit)"},
						{"MIPS","The MIPS architecture, 32-bit"},
						{"MIPS64","The MIPS architecture, 64-bit"},
						{"SPARC","The SPARC architecture, 32-bit"},
						{"SPARC64","The SPARC architecture, 64-bit"},
						{"S390","The System/390 architecture, 32-bit"},
						{"S390X","The System/390X architecture, 64-bit"},
						{"HPPA","The HP PA-RISC architecture, 32-bit"},
						{"HPPA64","The HP PA-RISC architecture, 64-bit"},
						{"SH","The SuperH architecture, 32-bit"},
						{"SH64","The SuperH architecture, 64-bit"},
						{"Alpha","The Alpha architecture"},
						{"LittleEndian","Byte order, least significant first"},
						{"BigEndian","Byte order, most significant first"},
						{"D_Coverage","Code coverage analysis instrumentation (command line switch -cov) is being generated"},
						{"D_Ddoc","Ddoc documentation (command line switch -D) is being generated"},
						{"D_InlineAsm_X86","Inline assembler for X86 is implemented"},
						{"D_InlineAsm_X86_64","Inline assembler for X86-64 is implemented"},
						{"D_LP64","Pointers are 64 bits (command line switch -m64)"},
						{"D_PIC","Position Independent Code (command line switch -fPIC) is being generated"},
						{"D_SIMD","Vector Extensions are supported"},
						{"unittest","Unit tests are enabled (command line switch -unittest)"},
						{"D_Version2","This is a D version 2 compiler"},
						{"none","Never defined; used to just disable a section of code"},
						{"all","Always defined; used as the opposite of none"}
					})
						CompletionDataGenerator.AddTextItem(kv.Key,kv.Value);
				}
			}
			else if (Attribute.Token == DTokens.Extern)
			{
				foreach (var kv in new Dictionary<string, string>{
					{"C",""},
					{"C++","C++ is reserved for future use"},
					{"D",""},
					{"Windows","Implementation Note: for Win32 platforms, Windows and Pascal should exist"},
					{"Pascal","Implementation Note: for Win32 platforms, Windows and Pascal should exist"},
					{"System","System is the same as Windows on Windows platforms, and C on other platforms"}
				})
					CompletionDataGenerator.AddTextItem(kv.Key, kv.Value);
			}
			else if (Attribute is PragmaAttribute)
			{
				var p = Attribute as PragmaAttribute;
				if (string.IsNullOrEmpty(p.Identifier))
					foreach (var kv in new Dictionary<string, string>{
					{"lib","Inserts a directive in the object file to link in"}, 
					{"msg","Prints a message while compiling"}, 
					{"startaddress","Puts a directive into the object file saying that the function specified in the first argument will be the start address for the program"}})
						CompletionDataGenerator.AddTextItem(kv.Key, kv.Value);
			}
		}
	}

	internal class ScopeAttributeCompletionProvider : AbstractCompletionProvider
	{
		public ScopeGuardStatement ScopeStmt;

		public ScopeAttributeCompletionProvider(ICompletionDataGenerator gen) : base(gen) { }

		protected override void BuildCompletionDataInternal(IEditorData Editor, string EnteredText)
		{
			if (ScopeStmt!=null)
			{
				foreach(var s in new[]{
					"exit", 
					"success", 
					"failure"})
					CompletionDataGenerator.AddTextItem(s,null);
			}
		}
	}
}
