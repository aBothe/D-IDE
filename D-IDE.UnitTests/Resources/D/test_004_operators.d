import std.stdio;

class C {}

void main(string[] args)
{
	auto c = new C();
	
	int aa[char[]];
	aa["a"] = 1;
	aa["b"] = 2;
	
	int[] a1 = [1, 2];
	int[] a2 = [3, 4];
	
	auto bit1 = 3 | 12;
	auto bit2 = 6 & 4;
	auto bit3 = 6 ^ 4;
	auto bit4 = 6 | 4;
	
	auto bl1 = true || false;
	auto bl2 = true && false;
	
	auto cmp1 = true == false;
	auto cmp2 = true != false;
	auto cmp3 = 12 > 14;
	auto cmp4 = 12 >= 14;
	auto cmp5 = 12 < 14;
	auto cmp6 = 12 <= 14;
	auto cmp7 = 12 <> 14;
	auto cmp8 = 12 <>= 14;
	auto cmp9 = 12 !> 14;
	auto cmp10 = 12 !>= 14;
	auto cmp11 = 12 !< 14;
	auto cmp12 = 12 !<= 14;
	auto cmp13 = 12 !<> 14;
	auto cmp14 = 12 !<>= 14;
	
	auto o1 = c is null;
	auto o2 = c !is null;
	
	auto aa1 = "a" in aa;
	auto aa2 = "a" !in aa;
	
	auto ac1 = a1 ~ a2;
	
	auto op1 = 12 - 14;
	auto op2 = 12 + 14;
	auto op3 = 12 * 14;
	auto op4 = 12 / 14;
	
	auto u1 = -12u;
	auto u2 = !12u;
	auto u3 = ++u1;
	auto u4 = --u1;
	auto u5 = &u1;
	auto u6 = *u5;
	auto u7 = +12u;
	auto u8 = ~12u;

	while(c !is c && c is null)
    { break;}
}