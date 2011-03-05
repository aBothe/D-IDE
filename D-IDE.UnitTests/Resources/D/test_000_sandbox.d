import std.stdio;

class C {}

void main(string[] args)
{
	auto c = new C();
	auto d = c;
	
	if (c is d || c == d) {}
	if (c !is d || c == d) {}
}
