import std.stdio;

class C {}

void main(string[] args)
{
	auto c = new C();
	int aa[char[]];
	aa["a"] = 1;
	aa["b"] = 2;
	
	auto o1 = (c is null || c !is null);
	auto o2 = (c is C || c !is C);
	auto o3 = ("a" in aa || "a" !in aa);
}
