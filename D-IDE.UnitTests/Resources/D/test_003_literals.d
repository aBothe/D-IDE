import std.stdio;

void main(string[] args)
{
	// Strings
	auto s01 = r"c:\foo.exe";
	auto s02 = r"ab\n";
	auto s03 = `"Hello, World", Jennifer said
\quietly\.`;
	auto s04 = "c:\\foo.exe";
	auto s05 = "c:\\foo.exe"c;
	auto s06 = "c:\\foo.exe"w;
	auto s07 = "c:\\foo.exe"d;
	auto s08 = x"00 FBCD 32FD 0A";
	auto s10 = q"EOS
This
is a multi-line
heredoc string
EOS";
	auto s11 = q"(foo(xxx))";
	auto s12 = q"/foo]/";
	//auto s13 = q{/*}*/ };
	auto s14 = q{ foo(q{hello}); };
	auto s15 = q{ __TIME__ };
	
	// Integers
	auto i1 = 4;
	auto i2 = 4L;
	auto i3 = 4u;
	auto i4 = 4U;
	auto i5 = 4Lu;
	auto i6 = 4uL;
	auto i7 = 4UL;
	auto i8 = 23_420_345_261;
	
	// Floating Points
	auto f1 = 4.4;
	auto f2 = 123_456.567_8;
	auto f3 = 1.2314e-12;
	auto f4 = 1.175494351e-38F;
	
	// Binary Numbers
	auto b1 = 0b01001101;
	auto b2 = 0B01110101;
	
	// Octal Numbers
	auto o1 = 034;
	
	// Hex Numbers
	auto h1 = 0xEf;
	auto h2 = 0XcC;
	auto h3 = 0x1.FFFFFFFFFFFFFp1023;
	auto h4 = 0x1p-52;

	// Imaginary Numbers
	auto im1 = 6.3i;
	auto im2 = 6.3fi;
	auto im3 = 6.3Li;
	
	// Special Tokens
	auto st1 = __DATE__;
	auto st3 = __TIME__;
	auto st4 = __TIMESTAMP__;
	auto st5 = __VENDOR__;
	auto st6 = __VERSION__;
	
	//int #line 6 "foo\bar
}
__EOF__