using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Resolver;
using D_Parser.Formatting;
using D_Parser;

namespace ParserTests
{
	class FormatterTest
	{
		public static void RunTests()
		{
			Console.WriteLine("Indentation tests...");

			TestLastLine(@"import
std", 1);
			TestLastLine(@"import std;
",0);
			TestLastLine(@"import
",1);
			TestLastLine(@"import std;
import std;
",0);
			TestLastLine(@"import std;
import 
	lol;",1);
			TestLastLine(@"import std;
import
",1);
			TestLine(@"import std;
import
	std;

class A{}",3,1);
			TestLastLine(@"class A{
	void foo()
	{
	}
	",1);

			TestLastLine(@"foo();",0);

			TestLastLine(@"foo();
",0);

			TestLastLine(@"foo(
);",1);
			TestLastLine(@"foo(
	a.lol",1);
			TestLastLine(@"foo(
	a,
	b",1);
			TestLastLine(@"foo(a,
	b);",1);

			TestLastLine(@"foo(
	a)",1);
			TestLastLine(@"foo(
	b())",1);
			TestLastLine(@"foo(
",1);
			TestLastLine(@"foo(
)",1);

			TestLastLine(@"foo(asdf())
{",0);
			TestLastLine(@"foo(asdf())
", 1);
			TestLastLine(@"foo(asdf()=b)
",1);
			TestLastLine(@"foo(asdf)
",1);

			TestLastLine(@"foo()
{
	asdf;
}
",0);
			TestLastLine(@"foo()
{
	asdf;
}",0);
			TestLastLine(@"foo()
{
	asdf;
	asdf;}
",0);
			TestLastLine(@"foo()
{
	asdf;
	asdf;}",1);
			TestLastLine(@"foo(){
	a;
	bar();}",1);
			TestLastLine(@"foo()
{
	b(
		{
			nestedFoo();", 3);

			TestLastLine(@"foo()
{
	b({
		nestedFoo();
	});
",1);

			TestLastLine(@"foo()
{
	bar({asdfCall();});",1);

			TestLastLine(@"class A:B
{",0);
			TestLastLine(@"class A:
",1);
			TestLastLine(@"class A:B
",1);
			

			TestLastLine(@"enum A
{",0);
			TestLastLine(@"enum A
{
",1);
			TestLastLine(@"enum A
{
	a,
	",1);
			TestLastLine(@"enum A
{
a= A+
B",2);
			TestLastLine(@"enum A
{
a,
b=A+B,
c",1);
			TestLastLine(@"enum A
{
a,
b=A+B,
c,
",1);
			TestLastLine(@"enum A
{
a,
b=A*B,
c,
d,",1);
			TestLastLine(@"enum A
{
a,
b
,c",1);
			TestLastLine(@"enum A
{
a
b,
c,
}",0);


			TestLastLine(@"if(a)
{",0);

			TestLastLine(@"if(a)
	a;",1);
			TestLastLine(@"if(asdf)
", 1);
			TestLastLine(@"if(asdf())
", 1);
			TestLastLine(@"if(asdf()==b)
",1);
			TestLastLine(@"if(
",1);


			TestLastLine(@"switch(a)
{
	case:",1);
			TestLastLine(@"switch(a)
{
	case 3:
		lol;",2);
			TestLastLine(@"switch(a)
{
	case 3:
		lol;
		", 2);

			TestLastLine(@"switch(a)
{
	case 3:
		lol;
}", 0);

			TestLastLine(@"switch(a)
{
	case 3:
		lol;
}
", 0);
			TestLastLine(@"switch(a)
{
	case 3:
		lol;
		asdf;", 2);
			TestLastLine(@"switch(a)
{
	case 3:
		lol;
	case 4:
		asdf;", 2);
			TestLastLine(@"switch(a)
{
	case 3:
		lol;
	default:
	case 4:
		asdf;", 2);


			TestLastLine(@"private:
	",1);

			TestLastLine(@"version(Windows):
	",1);
			TestLastLine(@"version(D):",0);

			TestLastLine(@"
private foo()
{
	a;
}",0);

			TestLastLine(@"
private:
	foo()
{",0);

			TestLastLine(@"
void main(string[] args)
{
    if(true)
    {
		for(int i=0; i<5;i++)
		{
			i = i % 4;
			if(i == 3)
			{
				i++;
				", 4);

			TestLastLine(@"
void main(string[] args)
{
    if(true)
    {
		for(int i=0; i<5;i++)
		{
			i = i % 4;
			if(i == 3)
			{
				i++;
			}", 3);

			TestLine(@"
void main(string[] args)
{
    if(true)
    {
		for(int i=0; i<5;i++)
		{
			i = i % 4;
			if(i == 3)
			{
				i++;
			}
		}
	}
}", 12, 3);

		}

		static void TestLastLine(string code, int targetIndent)
		{
			var newInd=GetLastLineIndent(code);
			if (newInd != targetIndent)
				OutputError(code, targetIndent, newInd);
		}

		static void TestLine(string code, int line, int targetIndent)
		{
			var newInd = GetLineIndent(code,line);
			if (newInd != targetIndent)
				OutputError(code,targetIndent,newInd);
		}

		static void OutputError(string code,int expected, int calculated)
		{
			Console.WriteLine();
			Console.WriteLine("---------------------------------------------------");
			Console.WriteLine("\"" + code + "\"");
			Console.WriteLine("Expected:\t" + expected);
			Console.WriteLine("Calculated:\t" + calculated + " tabs");
		}

		static int GetLineIndent(string code,int line)
		{
			var fmt = new DFormatter();

			var cb = fmt.CalculateIndentation(code, line);

			return cb != null ? cb.GetLineIndentation(line) : 0;
		}

		static int GetLastLineIndent(string code)
		{
			var caret = DocumentHelper.OffsetToLocation(code, code.Length);

			return GetLineIndent(code, caret.Line);
		}
	}
}
