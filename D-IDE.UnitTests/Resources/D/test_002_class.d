import std.stdio;

class ClassTest
{
    private int _i;
    protected string _s;
	float _f;
	static long _l;

public:
    this(int i)
    {
		_i = i;
    }
	
    ~this()
    {
		_i = -1;
    }

    bool isEven()
    {
        return (_i%2) == 0;
    }
}
