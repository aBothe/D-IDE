using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Util;
using System.Globalization;

namespace D_Parser
{
	class myLookupTable
	{
		Node root = new Node(-1, null);
		bool casesensitive;
		int length;

		/// <value>
		/// The number of elements in the table
		/// </value>
		public int Count
		{
			get
			{
				return length;
			}
		}

		/// <summary>
		/// Inserts an int in the tree, under keyword
		/// </summary>
		public int this[string keyword]
		{
			get
			{
				Node next = root;

				if(!casesensitive)
				{
					keyword = keyword.ToUpper(CultureInfo.InvariantCulture);
				}

				for(int i = 0; i < keyword.Length; ++i)
				{
					int index = ((int)keyword[i]) % 256;
					next = next.leaf[index];

					if(next == null)
					{
						return -1;
					}

					if(keyword == next.word)
					{
						return next.val;
					}
				}
				return -1;
			}
			set
			{
				Node node = root;
				Node next = root;

				if(!casesensitive)
				{
					keyword = keyword.ToUpper(CultureInfo.InvariantCulture);
				}

				++length;

				// insert word into the tree
				for(int i = 0; i < keyword.Length; ++i)
				{
					int index = ((int)keyword[i]) % 256; // index of curchar
					bool d = keyword[i] == '\\';

					next = next.leaf[index];             // get node to this index

					if(next == null)
					{ // no node created -> insert word here
						node.leaf[index] = new Node(value, keyword);
						break;
					}

					if(next.word != null && next.word.Length != i)
					{ // node there, take node content and insert them again
						string tmpword = next.word;                  // this word will be inserted 1 level deeper (better, don't need too much 
						int tmpval = next.val;                 // string comparisons for finding.)
						next.val = -1;
						next.word = null;
						this[tmpword] = tmpval;
					}

					if(i == keyword.Length - 1)
					{ // end of keyword reached, insert node there, if a node was here it was
						next.word = keyword;       // reinserted, if it has the same length (keyword EQUALS this word) it will be overwritten
						next.val = value;
						break;
					}

					node = next;
				}
			}
		}

		/// <summary>
		/// Creates a new instance of <see cref="LookupTable"/>
		/// </summary>
		public myLookupTable(bool casesensitive)
		{
			this.casesensitive = casesensitive;
		}

		class Node
		{
			public Node(int val, string word)
			{
				this.word = word;
				this.val = val;
			}

			public string word;
			public int val;

			public Node[] leaf = new Node[256];
		}
	}

	public static class DKeywords
	{
		public static readonly string[] keywordList = {
	"align",
	"asm",
	"assert",
	"auto",

	"body",
	"bool",
	"break",
	"byte",

	"case",
	"cast",
	"catch",
	"cdouble",
	"cent",
	"cfloat",
	"char",
	"class",
	"const",
	"continue",
	"creal",

	"dchar",
	"debug",
	"default",
	"delegate",
	"delete",
	"deprecated",
	"do",
	"double",

	"else",
	"enum",
	"export",
	"extern",

	"false",
	"final",
	"finally",
	"float",
	"for",
	"foreach",
	"foreach_reverse",
	"function",

	"goto",

	"idouble",
	"if",
	"ifloat",
	"import",
	"immutable",
	"in",
	"inout",
	"int",
	"interface",
	"invariant",
	"ireal",
	"is",

	"lazy",
	"long",

	"macro",
	"mixin",
	"module",

	"new",
	"nothrow",
	"null",

	"out",
	"override",

	"package",
	"pragma",
	"private",
	"protected",
	"public",
	"pure",

	"real",
	"ref",
	"return",

	"scope",
	"shared",
	"short",
	"static",
	"struct",
	"super",
	"switch",
	"synchronized",

	"template",
	"this",
	"throw",
	"true",
	"try",
	"typedef",
	"typeid",
	"typeof",

	"ubyte",
	"ucent",
	"uint",
	"ulong",
	"union",
	"unittest",
	"ushort",

	"version",
	"void",
	"volatile",

	"wchar",
	"while",
	"with",

	"__gshared",
	"__thread",
	"__traits",

	"abstract",
	"alias",

	"@property",
	"@disabled",
		};

		static myLookupTable keywords = new myLookupTable(true);

		static DKeywords()
		{
			for(int i = 0; i < keywordList.Length; ++i)
			{
				keywords[keywordList[i]] = i + DTokens.Align;
			}
		}

		public static int GetToken(string keyword)
		{
			return (int)keywords[keyword];
		}
	}
}
