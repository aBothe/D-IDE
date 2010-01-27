using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.NRefactory.Parser;

namespace D_IDE
{
    
    class Parser
    {
	
	public static DDoc Parse(string fn)
	{
	    return Parse(File.ReadAllText(fn));
	}

	public static Log lw;

	public static void Error(string msg)
	{
	    if (lw != null)
	    {
		lw.Add(msg);
	    }
	    else throw new Exception(msg);
	    //MessageBox.Show(msg);
	}



	public static _class ParseClass(string v, int rdepth, VisbilityType vis)
	{
	    _class ret = new _class();
	    ret.level = rdepth;
	    ret.vis = vis;
	    
	    string ts = v.Trim('\r','\n','\t',' ');

	    if (ts.StartsWith("class "))
	    {
		ts = ts.Remove(0, 6);
	    }
	    else
	    {
		Error("error at ParseClass(" + v + "): no class declaration");
	    }

	    int j = ts.IndexOf('{');

	    ret.name = ts.Substring(0,j);

	    ts = ts.Substring(j + 1);

	    VisbilityType tvis = VisbilityType.Public;

	    ParseMode mode=ParseMode.None; // What is parser waiting for
	    bool isStringing = false;

	    int last = 0;

	    int tdep = rdepth+1;
	    int depth=rdepth+1; // {

	    int bdepth = 0; // (
	    int tbdep = 0;

	    bool commenting = false;
	    bool multicomm = false;
	    
	    //string ts="", ts2="";
	    //DataType tdt;

	    for (int i = 0; i < ts.Length; i++)
	    {
		char c = ts[i];

		#region Comments & Strings
		if (i >= 1)
		{
		    if (ts[i - 1] == '/' && (c == '/' || c == '+')) { commenting = true; }
		    if (c == '*' && ts[i - 1] == '/') { multicomm = true; }


		    if (multicomm && (ts[i - 1] == '*' && c == '/'))
		    {
			last = i + 1;
			multicomm = false; continue;
		    }

		    if (commenting)
			if ((ts[i - 1] == '+' && c == '/') || c == '\n')
			{
			    last = i + 1;
			    commenting = false; continue;
			}
		}
		if (c == '\"') isStringing = !isStringing;
		if (commenting || multicomm || isStringing) continue;



		if (c == '(') { bdepth++; }
		if (c == ')') { bdepth--; }

		if (c == '{') { depth++; continue; }
		#endregion

		if (c == '}')
		{
		    depth--;
		    if (mode == ParseMode._class && depth == tdep)
		    {
			ret.Classes.Add(ParseClass(ts.Substring(last, i - last), depth, tvis));
			//last = i + 1;
			mode = ParseMode.None;
		    }
		    last = i + 1;

		    continue;
		}

		if (c == ';' && !isStringing)
		{
		    if (mode == ParseMode._var)
		    {
			_var tv = Parser.ParseVar(ts.Substring(last, i - last), depth, tvis);
			tv.startOffset = ret.startOffset+ last;
			tv.endOffset = ret.endOffset+ i - last;
			ret.Prop.Add(tv);

			last = i + 1;
			mode = ParseMode.None;
			//if (mode == ParseMode._var) MessageBox.Show("aaa");
			continue;
		    }
		}

		if (c == ')' && !isStringing)
		{
		    if (mode == ParseMode._var)
		    {
			_func tf = Parser.ParseFunc(ts.Substring(last, i - last), depth, tvis);
			tf.startOffset = ret.startOffset + last;
			tf.endOffset = ret.endOffset + i - last;
			ret.Mem.Add(tf);

			last = i + 1;
			mode = ParseMode.None;
			continue;
		    }

		    if (mode == ParseMode._ctor || mode == ParseMode._dtor)
		    {
			_func tf = Parser.ParseFunc(ts.Substring(last, i - last), depth, tvis);
			tf.startOffset = ret.startOffset + last;
			tf.endOffset = ret.endOffset + i - last;
			ret.Mem.Add(tf);

			last = i + 1;
			mode = ParseMode.None;
			continue;
		    }
		}

		string tok = ts.Replace('\t', ' ').Substring(last, i - last).Trim('\r', '\n', ' ');
		//MessageBox.Show("'"+tok+"'");


		if (_var.Type.Contains(tok) && depth == rdepth+1 && !isStringing)
		{
		    mode = ParseMode._var;
		}

		switch (tok)
		{
		    case "class":
			mode = ParseMode._class;
			tbdep = bdepth;
			break;
		    case "this":
			mode = ParseMode._ctor;
			break;
		    case "delete":
			mode = ParseMode._dtor;
			break;
		    case "public:":
		    case "public":
			tvis = VisbilityType.Public;
			break;
		    case "private:":
		    case "private":
			tvis = VisbilityType.Private;
			break;
		    case "protected:":
		    case "protected":
			tvis = VisbilityType.Protected;
			break;
		    default: break;
		}
	    }
	    return ret;
	}



	// void main(string[] arg, int a, byte b)
	public static _func ParseFunc(string v,int depth, VisbilityType vis)
	{
	    _func ret = new _func();
	    ret.level = depth;
	    ret.vis = vis;

	    string ts=v.TrimEnd(')');

	    int j = ts.LastIndexOf('(');

	    ret.param = ParseArgList(ts.Substring(j+1));

	    string[] ts2 = ts.Substring(0, j).Trim().Split(' ');

	    if(ts2.Length<=0)
		Error("at ParseFunc(" + v + ") occured an error");
	    if (ts2.Length == 1)
		if(ts2[0]=="this")
		{
		    ret.type="(Constructor)";
		    ret.name = ts2[0];
		    return ret;
		}else
		if(ts2[0]=="delete")
		{
		    ret.type="(Destructor)";
		    ret.name = ts2[0];
		    return ret;
		}else
		    Error("at ParseFunc(" + v + ") occured an error");
	    ret.type = ts2[0];
	    ret.name = ts2[1];
	    return ret;
	}

	// int a, int b, string c
	private static List<_var> ParseArgList(string v)
	{
	    List<_var> ret = new List<_var>();

	    foreach (string var in v.TrimStart('(').TrimEnd(')').Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries))
	    {
		ret.Add(ParseVar(var.Trim(), 0, VisbilityType.Public));
	    }

	    return ret;
	}

	// import a,b,c
	private static string[] ParseImport(string v)
	{
	    List<string> ret = new List<string>();

	    int j = v.IndexOf("import");
	    
	    if (j < 0 || v.LastIndexOf(';')>0) { Error("at ParseImport(" + v + ") occured an error"); return new string[] { }; }

	    string ts = v.Substring(j + 6).Replace(" ", "");
	    foreach (string var in ts.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
	    {
		ret.Add(var);
	    }

	    return ret.ToArray();
	}

	// int a=123
	public static _var ParseVar(string v, int depth, VisbilityType vis)
	{
	    _var ret = new _var();
	    ret.level = depth;
	    ret.vis = vis;

	    string[] ts;

	    int j = v.LastIndexOf('=');
	    if (j >= 0) ts = v.Substring(0, j).Trim().Split(' ');
	    else	   ts = v.Trim().Split(' ');

	    if (j > 0) ret.def = v.Substring(j+1).Trim();

	    if (ts.Length < 2) Error("at ParseVar(" + v + ") occured an error");
	    ret.type = ts[0];
	    ret.name = ts[1];

	    return ret;
	}
    }
}
