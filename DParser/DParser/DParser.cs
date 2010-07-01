using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace D_Parser
{
	/// <summary>
	/// Parser for D Code
	/// </summary>
	public partial class DParser
	{
		public string PhysFileName;

		public static CodeLocation GetCodeLocation(DToken t) { return new CodeLocation(t.Location.Column, t.Location.Line); }
		public static CodeLocation ToCodeEndLocation(DToken t) { return new CodeLocation(t.EndLocation.Column, t.EndLocation.Line); }

		/// <summary>
		/// Parses D source file
		/// </summary>
		/// <param name="fn"></param>
		/// <param name="imports"></param>
		/// <param name="folds"></param>
		/// <returns>Module structure</returns>
		public static DNode ParseFile(string moduleName, string fn, out List<string> imports)
		{
			if (!File.Exists(fn)) { imports = new List<string>(); return null; }
			DNode ret = new DNode(FieldType.Root);

			FileStream fs;
			try
			{
				fs = new FileStream(fn, FileMode.Open);
			}
			catch (IOException iox) { imports = new List<string>(); return null; }
			TextReader tr = new StreamReader(fs);

			DLexer dl = new DLexer(tr);

			DParser p = new DParser(dl);
			//dl.Errors.SemErr = p.SemErr;
			//dl.Errors.SynErr = p.SynErr;
			p.PhysFileName = fn;
			//p.SemErr(DTokens.Short);
			if (fs.Length > (1024 * 1024 * 2))
			{
				OnError(fn, moduleName, 0, 0, DTokens.EOF, "DParser only parses files that are smaller than 2 MBytes!");
				imports = new List<string>();
				return ret;
			}

			ret = p.Parse(moduleName, out imports);

			fs.Close();


			return ret;
		}

		/// <summary>
		/// Parses D source text.
		/// See also <seealso cref="ParseFile"/>
		/// </summary>
		/// <param name="cont"></param>
		/// <param name="imports"></param>
		/// <param name="folds"></param>
		/// <returns>Module structure</returns>
		public static DNode ParseText(string file, string moduleName, string cont, out List<string> imports)
		{
			if (cont == null || cont.Length < 1) { imports = null; return null; }
			DNode ret = new DNode(FieldType.Root);

			TextReader tr = new StringReader(cont);

			DParser p = new DParser(new DLexer(tr));
			p.PhysFileName = file;
			if (cont.Length > (1024 * 1024 * 2))
			{
				p.SemErr(DTokens.EOF, 0, 0, "DParser only parses files that are smaller than 2 MBytes!");
				imports = new List<string>();
				return ret;
			}
			ret = p.Parse(moduleName, out imports);
			tr.Close();

			return ret;
		}

		public static DParser Create(TextReader tr)
		{
			DLexer dl = new DLexer(tr);
			return new DParser(dl);
		}

		/// <summary>
		/// Encapsules whole document structure
		/// </summary>
		DNode doc;

		public List<string> import;

		/// <summary>
		/// Modifiers for entire block
		/// </summary>
		List<int> BlockModifiers;
		/// <summary>
		/// Modifiers for current expression only
		/// </summary>
		List<int> ExpressionModifiers;

		public DNode Document
		{
			get { return doc; }
		}

		public string SkipToSemicolon()
		{
			string ret = "";

			int mbrace = 0, par = 0;
			while (la.Kind != DTokens.EOF)
			{
				if (la.Kind == DTokens.OpenCurlyBrace) mbrace++;
				if (la.Kind == DTokens.CloseCurlyBrace) mbrace--;

				if (la.Kind == DTokens.OpenParenthesis) par++;
				if (la.Kind == DTokens.CloseParenthesis) par--;

				if (ThrowIfEOF(DTokens.Semicolon)) break;
				if (mbrace < 1 && par < 1 && la.Kind != DTokens.Semicolon && Peek(1).Kind == DTokens.CloseCurlyBrace)
				{
					ret += strVal;
					SynErr(la.Kind, "Check for missing semicolon!");
					break;
				}
				if (mbrace < 1 && par < 1 && la.Kind == DTokens.Semicolon)
				{
					break;
				}
				if (ret.Length < 2000) ret += strVal;
				lexer.NextToken();
			}
			return ret;
		}

		public void SkipToClosingBrace()
		{
			int mbrace = 0;
			while (la.Kind != DTokens.EOF)
			{
				if (ThrowIfEOF(DTokens.CloseCurlyBrace)) return;
				if (la.Kind == DTokens.OpenCurlyBrace)
				{
					mbrace++;
				}
				if (la.Kind == DTokens.CloseCurlyBrace)
				{
					mbrace--;
					if (mbrace <= 0) { break; }
				}
				lexer.NextToken();
			}
		}

		public string SkipToClosingParenthesis()
		{
			string ret = "";
			int mbrace = 0, round = 0;
			while (!EOF)
			{
				if (la.Kind == DTokens.OpenCurlyBrace) mbrace++;
				if (la.Kind == DTokens.CloseCurlyBrace) mbrace--;

				if (ThrowIfEOF(DTokens.CloseParenthesis)) break;
				if (la.Kind == DTokens.OpenParenthesis)
				{
					round++;
					lexer.NextToken(); continue;
				}
				if (la.Kind == DTokens.CloseParenthesis)
				{
					round--;
					if (mbrace < 1 && round < 1) { break; }
				}
				if (ret.Length < 2000) ret += strVal;
				lexer.NextToken();
			}
			return ret;
		}

		public string SkipToClosingSquares()
		{
			string ret = "";
			int mbrace = 0, round = 0;
			while (!EOF)
			{
				if (la.Kind == DTokens.OpenCurlyBrace) mbrace++;
				if (la.Kind == DTokens.CloseCurlyBrace) mbrace--;

				if (ThrowIfEOF(DTokens.CloseSquareBracket)) break;
				if (la.Kind == DTokens.OpenSquareBracket)
				{
					round++;
					lexer.NextToken(); continue;
				}
				if (la.Kind == DTokens.CloseSquareBracket)
				{
					round--;
					if (mbrace < 1 && round < 1) { break; }
				}
				if (ret.Length < 2000) ret += strVal;

				lexer.NextToken();
			}
			return ret;
		}

		public DLexer lexer;
		//public Errors errors;
		public DParser(DLexer lexer)
		{
			this.lexer = lexer;
			//errors = lexer.Errors;
			//errors.SynErr = new ErrorCodeProc(SynErr);
		}

		StringBuilder qualidentBuilder = new StringBuilder();

		DToken t
		{
			[System.Diagnostics.DebuggerStepThrough]
			get
			{
				return (DToken)lexer.CurrentToken;
			}
		}

		/// <summary>
		/// lookAhead token
		/// </summary>
		DToken la
		{
			[System.Diagnostics.DebuggerStepThrough]
			get
			{
				return (DToken)lexer.LookAhead;
			}
		}

		int _lastcommentindex = 0;
		public string CheckForExpressionComments()
		{
			string ret = "";
			for (int i = _lastcommentindex; ret.Length<512&&i>=_lastcommentindex&& i < lexer.Comments.Count; i++)
			{
				Comment tc = lexer.Comments[i];
				if (tc.CommentType != Comment.Type.Documentation)
					continue;

                string t = tc.CommentText.Trim();
                for (int j = i; t == "ditto" && j > 0;j-- )
                {
                    tc = lexer.Comments[j];
                    if (tc.CommentType != Comment.Type.Documentation)
                        continue;
                    t = tc.CommentText;
                }
				if (ret == "") ret = t;
				else ret += "\n" + t;
			}
			_lastcommentindex = lexer.Comments.Count;
			return ret;
		}

		/// <summary>
		/// Check if current lookAhead DToken equals to n and skip that token.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		protected bool Expect(int n, string reason)
		{
			if (la.Kind == n)
			{
				lexer.NextToken();
				return true;
			}
			else
			{
				SynErr(n, reason);
				return false;
			}
		}

		/// <summary>
		/// Retrieve string value of current lookAhead token
		/// </summary>
		protected string strVal
		{
			get
			{
				if (la.Kind == DTokens.Identifier || la.Kind == DTokens.Literal)
					return la.Value;
				return DTokens.GetTokenString(la.Kind);
			}
		}

		protected bool ThrowIfEOF(int n)
		{
			if (la.Kind == DTokens.EOF)
			{
				SynErr(n, "End of file reached!");
				return true;
			}
			return false;
		}

		protected bool PeekMustBe(int n, string reason)
		{
			if (Peek(1).Kind == n)
			{
				lexer.NextToken();
			}
			else
			{
				SynErr(n, reason);
				return false;
			}
			return true;
		}

		/* Return the n-th token after the current lookahead token */
		void StartPeek()
		{
			lexer.StartPeek();
		}

		DToken Peek()
		{
			return lexer.Peek();
		}

		DToken Peek(int n)
		{
			lexer.StartPeek();
			DToken x = la;
			while (n > 0)
			{
				x = lexer.Peek();
				n--;
			}
			return x;
		}

		/*-----------------------------------------------------------------*
		 * Resolver routines to resolve LL(1) conflicts:                   *
		 * These resolution routine return a boolean value that indicates  *
		 * whether the alternative at hand shall be choosen or not.        *
		 * They are used in IF ( ... ) expressions.                        *
		 *-----------------------------------------------------------------*/

		/* True, if ident is followed by "=" */
		bool IdentAndAsgn()
		{
			return la.Kind == DTokens.Identifier && Peek(1).Kind == DTokens.Assign;
		}

		bool IsAssignment() { return IdentAndAsgn(); }

		/* True, if ident is followed by ",", "=", "[" or ";"*/
		bool IsVarDecl()
		{
			int peek = Peek(1).Kind;
			return la.Kind == DTokens.Identifier &&
				(peek == DTokens.Comma || peek == DTokens.Assign || peek == DTokens.Semicolon || peek == DTokens.OpenSquareBracket);
		}

		bool EOF
		{
			get { return la == null || la.Kind == DTokens.EOF; }
		}



		/// <summary>
		/// Initializes and proceed parse procedure
		/// </summary>
		/// <param name="imports">List of imports in the module</param>
		/// <param name="fl">TODO: Folding marks</param>
		/// <returns>Completely parsed module structure</returns>
		public DNode Parse(string moduleName, out List<string> imports)
		{
			import = new List<string>();
			imports = import;

			BlockModifiers = new List<int>();
			BlockModifiers.Add(DTokens.Public);
			ExpressionModifiers = new List<int>();

			doc = new DNode(FieldType.Root);
			doc.name = moduleName;
			doc.startLoc = CodeLocation.Empty;
			doc.module = moduleName;
			ParseBlock(ref doc, false);

			doc.endLoc = GetCodeLocation(la);
			return doc;
		}

		/// <summary>
		/// Parses complete block from current lookahead DToken "{" until the last "}" on the same depth
		/// </summary>
		/// <param name="ret">Parent node</param>
		void ParseBlock(ref DNode ret, bool isFunctionBody)
		{
			int curbrace = 0;
			if(String.IsNullOrEmpty( ret.desc))ret.desc = CheckForExpressionComments();
			List<int> prevBlockModifiers = new List<int>(BlockModifiers);
			ExpressionModifiers.Clear();
			BlockModifiers.Clear();
			BlockModifiers.Add(DTokens.Public);

			//Debug.Print("ParseBlock started ("+ret.name+")");

			if (la != null) ret.startLoc= ret.BlockStartLocation = GetCodeLocation(la);

			while (la == null || la.Kind != DTokens.EOF)
			{
				lexer.NextToken();
			blockcont:
				if (la.Kind == DTokens.EOF) { if (curbrace > 1) SynErr(DTokens.CloseCurlyBrace); break; }
				BlockModifiers = prevBlockModifiers;

				if (la.Kind == DTokens.Scope)
				{
					if (Peek(1).Kind == DTokens.OpenParenthesis)
					{
						SkipToClosingParenthesis();
						continue;
					}
				}

				#region Modifiers
				if (DTokens.Modifiers[la.Kind])
				{
					DToken pt = Peek(1);
					int mod = la.Kind;

					if (pt.Kind == DTokens.OpenParenthesis) // const>(<char)[]
					{
						if (Peek(2).Kind == DTokens.CloseParenthesis) // invariant() {...} - whatever this shall mean...something like that is possible in D!
						{
							lexer.NextToken(); // Skip modifier ID
							lexer.NextToken(); // Skip "("
							// assert(la.Kind==DTokens.CloseParenthesis)
							if (Peek(1).Kind == DTokens.OpenCurlyBrace)
							{
								SkipToClosingBrace();
							}

							continue;
						}
					}
					else if (pt.Kind == DTokens.Colon)// private>:<
					{
						if (!BlockModifiers.Contains(mod))
						{
							if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(BlockModifiers);
							BlockModifiers.Add(mod);
						}
						continue;
					}
					else if (pt.Kind == DTokens.OpenCurlyBrace) // public >{<...}
					{
						lexer.NextToken(); // Skip modifier
						DNode tblock = new DNode(ret.fieldtype);
						ParseBlock(ref tblock, isFunctionBody);

						foreach (DNode dt in tblock) // Apply modifier to parsed children
						{
							if (!dt.modifiers.Contains(mod)) // static package int a;
							{
								if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(dt.modifiers);
								dt.modifiers.Add(mod);
							}
						}

						ret.Children.AddRange(tblock.Children);
						continue;
					}
					else
					{
						DToken pt2 = pt;
						pt = lexer.Peek();
						bool hasFollowingMods = false;
						while (pt.Kind != DTokens.EOF)
						{
							if (DTokens.Modifiers[pt.Kind]) // static >const<
							{
								pt = lexer.Peek();
								if (pt.Kind == DTokens.OpenCurlyBrace) // static const >{<...}
								{
									hasFollowingMods = true;
									break;
								}
							}
							else
								break;
							pt = lexer.Peek();
						}

						if (!hasFollowingMods && la.Kind == DTokens.Const && pt2.Kind == DTokens.Identifier && pt.Kind == DTokens.Assign) // const >MyCnst2< = 2; // similar to enum MyCnst = 1;
						{
							DNode cdt = ParseEnum();
							cdt.type = "int";
							cdt.modifiers.Add(DTokens.Const);
							cdt.TypeToken = DTokens.Int;
							cdt.Parent = ret;
							ret.Children.Add(cdt);
						}

						if (!ExpressionModifiers.Contains(mod) && !hasFollowingMods) // static package int a;
						{
							if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(ExpressionModifiers);
							ExpressionModifiers.Add(mod);
						}
						continue;
					}

				}
				#endregion

				#region Normal Expressions
				if (DTokens.BasicTypes[la.Kind] || la.Kind == DTokens.Identifier ||
					la.Kind == DTokens.Typeof || DTokens.Modifiers[la.Kind] ||
					la.Kind == DTokens.OpenParenthesis) // (*MyPointer)(...);
				{
					if (!isFunctionBody && Peek(1).Kind == DTokens.OpenParenthesis)
					{
						if (Peek().Kind == DTokens.Times) // void(*foo)(....);
						{
							SemErr(la.Kind, "Skip that kind of syntax...");
							SkipToSemicolon();
							continue;
						}
					}
					bool isTypeOf = la.Kind == DTokens.Typeof;
					bool hasInititalClamps = la.Kind == DTokens.OpenParenthesis; // (*MyPointer)(...);

					if (hasInititalClamps && !isFunctionBody)
					{
						SynErr(la.Kind, "Declaration expected, not \"" + strVal + "\"!");
						SkipToClosingBrace();
						return;
					}

					#region Within Function Body
					if (isFunctionBody && !isTypeOf)
					{
						DToken pk = Peek(1);
						switch (pk.Kind)
						{
							case DTokens.Dot: // Package.foo();
								continue;

							case DTokens.Not: // Type!(int,b)();
								int mbrace = 0;
								bool isCall = false;
								while ((pk = Peek()).Kind != DTokens.EOF)
								{
									if (pk.Kind == DTokens.OpenCurlyBrace) mbrace++;
									if (pk.Kind == DTokens.CloseCurlyBrace) mbrace--;

									if (mbrace < 1 && pk.Kind == DTokens.Semicolon) { isCall = true; break; }
								}
								if (!isCall) break;
								SkipToSemicolon();
								continue;

							case DTokens.Colon: // part:
								lexer.NextToken();
								continue;

							case DTokens.OpenSquareBracket: // array[0]+=5; char[] buf;
								#region Check if Var Decl is done
								int mbrace2 = 0;
								bool isDecl = false;
								while ((pk = Peek()).Kind != DTokens.EOF)
								{
									switch (pk.Kind)
									{
										case DTokens.OpenSquareBracket:
											mbrace2++;
											break;
										case DTokens.CloseSquareBracket:
											mbrace2--;
											break;
										case DTokens.Dot:
											if (mbrace2 > 0) continue;
											pk = Peek();
											if (pk.Kind == DTokens.Identifier) // array[i].foo(); array[i].x=2;
											{
												continue;
											}
											break;
									}

									if (mbrace2 < 1)
									{
										if (DTokens.AssignOps[pk.Kind])
										{
											isDecl = pk.Kind == DTokens.Assign;
											break;
										}

										if (pk.Kind == DTokens.Identifier)
										{
											pk = Peek();
											if (pk.Kind == DTokens.Comma || // string[] a,b;
												pk.Kind == DTokens.Assign || // string[] stringArray=...;
												pk.Kind == DTokens.Semicolon || // string[] stringArray;
												pk.Kind == DTokens.OpenSquareBracket) // char[] ID=value;
											{ isDecl = true; goto parseexpr; }
										}
									}
								}
								#endregion
								if (isDecl) goto default;
							sqbracket:
								if (la.Kind == DTokens.OpenSquareBracket)
									SkipToClosingSquares();
							if (la.Kind == DTokens.CloseSquareBracket)
							{
								pk = Peek(1);
								if (pk.Kind == DTokens.OpenSquareBracket) // matrix[0][4]=1;
								{
									lexer.NextToken(); // Skip last "]"
									goto sqbracket;
								}
							}
							goto default;

							case DTokens.OpenParenthesis: // Must be function call 
							lexer.NextToken(); // Skip ID
							SkipToClosingParenthesis(); // foo(a,b,c,d);
							continue;

							default:
							if (pk.Kind == DTokens.Increment ||  // a++;
								pk.Kind == DTokens.Decrement)
								lexer.NextToken();
							if (DTokens.AssignOps[pk.Kind])// b<<=4;
							{
								ParseAssignIdent(ref ret, true);
								continue;
							}
							if (DTokens.Conditions[pk.Kind]) // p!= null || p<1
							{
								lexer.NextToken(); // Skip ID before Comparison Expression
							}
							break;
						}
					}
					#endregion
				parseexpr:
					if (DTokens.ClassLike[Peek(1).Kind]) break;


					#region Modifier assessment
					bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
					foreach (int m in BlockModifiers)
					{
						if (!ExpressionModifiers.Contains(m))
						{
							if (cvm && DTokens.VisModifiers[m]) continue;
							ExpressionModifiers.Add(m);
						}
					}
					List<int> TExprMods = new List<int>(ExpressionModifiers);
					ExpressionModifiers.Clear();

					#endregion

					DNode tv = ParseExpression();
					if (tv != null)
					{
						tv.modifiers.AddRange(TExprMods);
						tv.module = ret.module;
						tv.Parent = ret;
						ret.Add(tv);

						if (la.Kind == DTokens.Comma) goto blockcont;
					}
					continue;
				}
				#endregion

				#region Special and other Tokens
				switch (la.Kind)
				{
					#region Custom Allocators
					case DTokens.New:
						// This is for handling custom allocators (new(uint size){...})
						if (!isFunctionBody)
						{
							if (!PeekMustBe(DTokens.OpenParenthesis, "Expected \"(\" for declaring a custom allocator!"))
							{
								SkipToClosingBrace();
								break;
							}
							DNode custAlloc = new DNode(FieldType.Function);
							custAlloc.name = "new";
							custAlloc.type = "void*";
							custAlloc.TypeToken = DTokens.New;
							lexer.NextToken();
							ParseFunctionArguments(ref custAlloc);
							if (!Expect(DTokens.CloseParenthesis, "Expected \")\" for declaring a custom allocator!"))
							{
								SkipToClosingBrace();
								break;
							}
							custAlloc.modifiers.Add(DTokens.Private);
							ParseBlock(ref custAlloc, true);

							custAlloc.module = ret.module;
							custAlloc.Parent = ret;
							ret.Add(custAlloc);
						}
						break;
					case DTokens.Delete:
						// This is for handling custom deallocators (delete(void* p){...})
						if (!isFunctionBody)
						{
							if (!PeekMustBe(DTokens.OpenParenthesis, "Expected \"(\" for declaring a custom deallocator!"))
							{
								SkipToClosingBrace();
								break;
							}
							DNode custAlloc = new DNode(FieldType.Function);
							custAlloc.name = "delete";
							custAlloc.type = "void";
							custAlloc.TypeToken = DTokens.Delete;
							lexer.NextToken();
							ParseFunctionArguments(ref custAlloc);
							if (!Expect(DTokens.CloseParenthesis, "Expected \")\" for declaring a custom deallocator!"))
							{
								SkipToClosingBrace();
								break;
							}
							custAlloc.modifiers.Add(DTokens.Private);
							ParseBlock(ref custAlloc, true);

							custAlloc.module = ret.module;
							custAlloc.Parent = ret;
							ret.Add(custAlloc);
						}
						break;
					#endregion
					case DTokens.Cast:
						if (PeekMustBe(DTokens.OpenParenthesis, "Error parsing \"cast\" Expression: \"(\" expected!"))
						{
							SkipToClosingParenthesis();
							if (la.Kind != DTokens.CloseParenthesis) SynErr(DTokens.CloseParenthesis, "Error parsing \"cast\" Expression: \")\" expected!");
						}
						break;
					case DTokens.With:
						if (PeekMustBe(DTokens.OpenParenthesis, "Error parsing \"with()\" Expression: \"(\" expected!"))
						{
							SkipToClosingParenthesis();
							if (la.Kind != DTokens.CloseParenthesis) SynErr(DTokens.CloseParenthesis, "Error parsing \"with()\" Expression: \")\" expected!");
						}
						break;
					case DTokens.Asm: // Inline Assembler
						SkipToClosingBrace();
						break;
					case DTokens.Case:
						while (!EOF)
						{
							lexer.NextToken();
							if (la.Kind == DTokens.Colon) break;
						}
						break;
					case DTokens.Catch:
						if (Peek(1).Kind == DTokens.OpenParenthesis)
							SkipToClosingParenthesis();
						break;
					case DTokens.Debug:
						if (Peek(1).Kind == DTokens.OpenParenthesis)
							SkipToClosingParenthesis();
						break;
					case DTokens.Goto:
						SkipToSemicolon();
						break;
					case DTokens.Throw:
					case DTokens.Return:
						SkipToSemicolon();
						break;
					case DTokens.Unittest:
						if (Peek(1).Kind == DTokens.OpenCurlyBrace)
						{
							SkipToClosingBrace();
						}
						break;
					case DTokens.Do: // do {...} while(...);
						if (Peek(1).Kind == DTokens.OpenCurlyBrace)
						{
							SkipToClosingBrace();
							Expect(DTokens.CloseCurlyBrace, "Error parsing do Expression: \"}\" after \"do\" block expected!");
						}
						else
						{
							SkipToSemicolon();
							Expect(DTokens.Semicolon, "Semicolon after while() expected!");
						}

						if (Expect(DTokens.While, "Check correcness of do() while(); expression!"))
						{
							SkipToSemicolon();
						}
						break;
					case DTokens.For:
					case DTokens.Foreach:
					case DTokens.Foreach_Reverse:
					case DTokens.Switch:
					case DTokens.While:
					case DTokens.If: // static if(...) {}else if(...) {} else{}
						if (Peek(1).Kind == DTokens.OpenParenthesis)
						{
							SkipToClosingParenthesis();
							break;
						}
						break;
					case DTokens.Else:
						break;
					case DTokens.Comma:
						if (ret.Count < 1) break;
						lexer.NextToken(); // Skip ","
						// MyType a,b,c,d;
						DNode prevExpr = (DNode)ret.Children[ret.Count - 1];
						if (prevExpr.fieldtype == FieldType.Variable)
						{
							DNode tv = new DNode(FieldType.Variable);
							if (tv == null) continue;
							tv.modifiers = prevExpr.modifiers;
							tv.startLoc = prevExpr.startLoc;
							tv.type = prevExpr.type;
							tv.TypeToken = prevExpr.TypeToken;
							tv.name = ParseTypeIdent();
							tv.endLoc = GetCodeLocation(la);

							if (la.Kind == DTokens.Assign) tv.value = ParseAssignIdent(ref ret);
							else if (Peek(1).Kind == DTokens.Assign)
							{
								lexer.NextToken();
								tv.value = ParseAssignIdent(ref ret);
							}

							ret.Add(tv);

							if (la.Kind == DTokens.Comma) goto blockcont; // Another declaration is directly following
						}
						break;
					case DTokens.EOF:
						if (t.Kind != DTokens.CloseCurlyBrace) SynErr(DTokens.CloseCurlyBrace);
						ret.endLoc = GetCodeLocation(la);
						BlockModifiers = prevBlockModifiers;
						return;
					case DTokens.Align:
					case DTokens.Version:
						lexer.NextToken(); // Skip "version"

						if (la.Kind == DTokens.Assign) // version=xxx
						{
							SkipToSemicolon();
							break;
						}

						Expect(DTokens.OpenParenthesis, "Version checks only like version(...) - missing \"(\"");
						string version = strVal; // version(xxx)
						if (version == "Posix" && Peek(1).Kind == DTokens.OpenCurlyBrace) SkipToClosingBrace();
						break;
					case DTokens.Extern:
						if (Peek(1).Kind == DTokens.OpenParenthesis)
							SkipToClosingParenthesis();
						break;
					case DTokens.CloseCurlyBrace: // }
						curbrace--;
						if (curbrace < 0)
						{
							ret.endLoc = GetCodeLocation(la);
							BlockModifiers = prevBlockModifiers;
							ExpressionModifiers.Clear();
							return;
						}
						break;
					case DTokens.OpenCurlyBrace: // {
						curbrace++;
						break;
					case DTokens.Enum:
						DNode mye = ParseEnum();
						if (mye != null)
						{
							mye.Parent = ret;
							mye.module = ret.module;

							if (mye.name != "")
								ret.Add(mye);
							else
							{
								foreach (DNode ch in mye)
								{
									ch.Parent = ret;
									ch.module = ret.module;
								}
								ret.Children.AddRange(mye.Children);
							}
						}
						break;
					case DTokens.Super:
						if (isFunctionBody) // Every "super" in a function body can only be a call....
						{
							SkipToSemicolon();
							break;
						}
						break;
					case DTokens.This:
						if (isFunctionBody) // Every "this" in a function body can only be a call....
						{
							SkipToSemicolon();
							break;
						}

						#region Modifier assessment
						bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
						foreach (int m in BlockModifiers)
						{
							if (!ExpressionModifiers.Contains(m))
							{
								if (cvm && DTokens.VisModifiers[m]) continue;
								ExpressionModifiers.Add(m);
							}
						}
						List<int> TExprMods = new List<int>(ExpressionModifiers);
						ExpressionModifiers.Clear();

						#endregion

						string cname = "";
						if (t.Kind == DTokens.Tilde) // "~"
							cname = "~" + ret.name;
						else
							cname = ret.name;

						DNode ctor = ParseExpression();
						if (ctor != null)
						{
							if (ret.fieldtype == FieldType.Root && !TExprMods.Contains(DTokens.Static))
							{
								SemErr(DTokens.This, ctor.startLoc.Column, ctor.startLoc.Line, "Module Constructors must be static!");
							}

							ctor.modifiers.AddRange(TExprMods);
							ctor.name = cname;
							ctor.fieldtype = FieldType.Constructor;
							ctor.endLoc = GetCodeLocation(la);

							ctor.Parent = ret;
							ctor.module = ret.module;

							ret.Add(ctor);
						}
						break;
					case DTokens.Class:
					case DTokens.Template:
					case DTokens.Struct:
					case DTokens.Union:
					case DTokens.Interface:

						if (Peek(1).Kind == DTokens.OpenCurlyBrace) // struct {...}
						{
							break;
						}

						DNode myc = ParseClass();
						if (myc != null)
						{
							myc.module = ret.module;
							myc.Parent = ret;
							ret.Add(myc);
						}
						continue;
					case DTokens.Module:
						lexer.NextToken();
						ret.module = SkipToSemicolon();
						break;
					case DTokens.Typedef:
					case DTokens.Alias:
						// typedef void* function(int a) foo;
						DNode tt = new DNode();
						tt.startLoc = GetCodeLocation(la);

						int tbrace = 0;
						while (la.Kind != DTokens.EOF)
						{
							lexer.NextToken();
							if (la.Kind == DTokens.OpenCurlyBrace) tbrace++;
							if (la.Kind == DTokens.CloseCurlyBrace) tbrace--;
							if (tbrace < 1 && la.Kind == DTokens.Identifier && Peek(1).Kind == DTokens.Semicolon)
							{
								tt.name = strVal;
								break;
							}
							if (tbrace < 1 && la.Kind == DTokens.Semicolon)
							{
								break;
							}
							if (la.Kind == DTokens.EOF) { SynErr(DTokens.CloseParenthesis); return; }
							tt.type += strVal;
						}

						tt.fieldtype = FieldType.AliasDecl;
						tt.endLoc = GetCodeLocation(la);
						ret.Add(tt);
						break;
					case DTokens.Import:
						ParseImport();
						continue;
					case DTokens.Mixin:
					case DTokens.Assert:
					case DTokens.Pragma:
						SkipToSemicolon();
						break;
					default:
						break;
				}
				#endregion
			}
			// Debug.Print("ParseBlock ended (" + ret.name + ")");
		}

		/// <summary>
		/// import abc.def, efg.hij, xyz;
		/// </summary>
		void ParseImport()
		{
			if (la.Kind != DTokens.Import)
				SynErr(DTokens.Import);
			string ts = "";

			List<string> tl = new List<string>();
			while (!EOF && la.Kind != DTokens.Semicolon)
			{
				lexer.NextToken();
				if (ThrowIfEOF(DTokens.Semicolon)) return;
				switch (la.Kind)
				{
					default:
						ts += strVal;
						break;
					case DTokens.Comma:
					case DTokens.Semicolon:
						tl.Add(ts);
						ts = "";
						break;
					case DTokens.Colon:
					case DTokens.Assign:
						ts = "";
						break;
				}
			}

			if (la.Kind == DTokens.Semicolon) import.AddRange(tl);
		}


		void ParseTemplateArguments(ref DNode v)
		{
			int psb = 0;// ()
			DNode targ = null;

			if (la.Kind == DTokens.OpenParenthesis) psb = -1;
			while (la.Kind != DTokens.EOF)
			{
				if (ThrowIfEOF(DTokens.CloseParenthesis))
					return;

				switch (la.Kind)
				{
					case DTokens.OpenParenthesis:
						psb++;
						if (targ != null) targ.type += "(";
						break;
					case DTokens.CloseParenthesis:
						psb--;
						if (psb < 0)
						{
							if (targ != null)
							{
								targ.endLoc = ToCodeEndLocation(t);
								targ.name = targ.type;
								v.param.Add(targ);
							}
							return;
						}
						if (targ != null) targ.type += ")";
						break;
					case DTokens.Comma:
						if (psb > 1) break;
						if (targ == null) { SkipToClosingBrace(); break; }
						targ.endLoc = GetCodeLocation(la);
						targ.name = targ.type;
						v.param.Add(targ);
						targ = null;
						break;
					case DTokens.Dot:
						if (Peek(1).Kind == DTokens.Dot && Peek(2).Kind == DTokens.Dot) // "..."
						{
							if (targ == null) targ = new DNode(FieldType.Variable);

							targ.type = "...";
							targ.name = "...";

							targ.startLoc = GetCodeLocation(la);
							targ.endLoc = GetCodeLocation(la);
							targ.endLoc.Column += 3; // three dots (...)

							v.param.Add(targ);
							targ = null;
						}
						break;
					case DTokens.Alias:
						if (targ == null) targ = new DNode(FieldType.Variable);
						targ.startLoc = GetCodeLocation(la);
						targ.modifiers.Add(la.Kind);
						break;
					default:
						if (targ == null) { targ = new DNode(FieldType.Variable); targ.startLoc = ToCodeEndLocation(la); }

						if (DTokens.Modifiers[la.Kind] && Peek(1).Kind != DTokens.OpenParenthesis) // const int a
						{
							targ.modifiers.Add(la.Kind);
							break;
						}

						targ.type += strVal;
						break;
				}
				lexer.NextToken();
			}
		}

		/// <summary>
		/// Parses all variable declarations when "(" is the lookahead DToken and retrieves them into v.param. 
		/// Thereafter ")" will be lookahead
		/// </summary>
		/// <param name="v"></param>
		void ParseFunctionArguments(ref DNode v)
		{
			int psb = 0;// ()
			DNode targ = null;
			// int[]* MyFunction(in string[]* arg, uint function(aa[]) funcarg, ref MyType b)
			while (la.Kind != DTokens.EOF)
			{
				if (ThrowIfEOF(DTokens.CloseParenthesis))
					return;

				switch (la.Kind)
				{
					case DTokens.OpenParenthesis:
						psb++;
						break;
					case DTokens.CloseParenthesis:
						psb--;
						if (psb < 1)
						{
							if (targ != null)
							{
								targ.endLoc = ToCodeEndLocation(t);
								v.param.Add(targ);
							}
							return;
						}
						break;
					case DTokens.Comma:
						if (targ == null) { SkipToClosingBrace(); break; }
						targ.endLoc = GetCodeLocation(la);
						v.param.Add(targ);
						targ = null;
						break;
					case DTokens.Dot:
						if (Peek(1).Kind == DTokens.Dot && Peek(2).Kind == DTokens.Dot) // "..."
						{
							if (targ == null) targ = new DNode(FieldType.Variable);

							targ.type = "...";
							targ.name = "...";

							targ.startLoc = GetCodeLocation(la);
							targ.endLoc = GetCodeLocation(la);
							targ.endLoc.Column += 3; // three dots (...)

							v.param.Add(targ);
							targ = null;
						}
						break;
					case DTokens.Alias:
						if (targ == null) targ = new DNode(FieldType.Variable);
						targ.modifiers.Add(la.Kind);
						break;
					default:
						if (DTokens.Modifiers[la.Kind] && Peek(1).Kind != DTokens.OpenParenthesis) // const int a
						{
							if (targ == null) targ = new DNode(FieldType.Variable);
							targ.modifiers.Add(la.Kind);
							break;
						}
						if (DTokens.BasicTypes[la.Kind] || la.Kind == DTokens.Identifier || la.Kind == DTokens.Typeof)
						{
							if (targ == null) targ = new DNode(FieldType.Variable);
							if (Peek(1).Kind == DTokens.Dot) break;

							targ.startLoc = GetCodeLocation(la);
							targ.TypeToken = la.Kind;
							bool hasTypeMod = false;
							targ.type = ParseTypeIdent(false, out hasTypeMod);
							if (hasTypeMod || (la.Kind != DTokens.CloseParenthesis && Peek(1).Kind != DTokens.CloseParenthesis))
								lexer.NextToken(); // Skip last ID
							if (la.Kind == DTokens.CloseParenthesis) continue;

							if (la.Kind == DTokens.Delegate || la.Kind == DTokens.Function)
							{
								targ.fieldtype = FieldType.Delegate;
								targ.type += " " + strVal;
								lexer.NextToken(); // Skip "delegate" or "function"
								if (Expect(DTokens.OpenParenthesis, "Delegate parameters expected!"))
								{
									ParseFunctionArguments(ref targ);
									Expect(DTokens.CloseParenthesis, "Missing \")\" after delegate argument parsing!");
								}
							}

							if (la.Kind == DTokens.Comma || (la.Kind == DTokens.CloseParenthesis && Peek(1).Kind == DTokens.Semicolon))// size_t wcslen(in wchar *>);<
							{
								continue;
							}

							if (la.Kind == DTokens.Colon) // void foo(T>:<Object[],S[],U,V) {...}
							{
								int tpsb = 0;
								while (la.Kind != DTokens.EOF)
								{
									if (la.Kind == DTokens.OpenParenthesis) tpsb++;
									if (la.Kind == DTokens.CloseParenthesis) tpsb--;
									targ.type += strVal;
									DToken pk2 = Peek(1);
									if (tpsb < 1 && (pk2.Kind == DTokens.Comma || pk2.Kind == DTokens.CloseParenthesis))
									{
										lexer.NextToken();
										break;
									}
									lexer.NextToken();
								}
							}

							if (la.Kind == DTokens.Identifier) targ.name = strVal;

							if (Peek(1).Kind == DTokens.Assign) // Argument has default argument
							{
								targ.value = ParseAssignIdent(ref v, true);
								if (la.Kind == DTokens.CloseParenthesis && Peek(1).Kind == DTokens.Comma) lexer.NextToken(); // void foo(int a=bar(>),<bool b)
								continue;
							}
						}
						break;
				}
				lexer.NextToken();
			}
		}

		void ValBox()
		{
			//MessageBox.Show(GetCodeLocation(la).ToString() + ": \"" + strVal + "\"");
		}


		/// <summary><![CDATA[
		/// enum{
		/// a=1,
		/// b=2,
		/// c
		/// }
		/// =1;
		/// =-1234;
		/// =&a;
		/// =*b;
		/// =cast(uint) -1
		/// =cast(char*) "",
		/// =*(cast(int[]*)b);
		/// =delegate void(int i) {...};
		/// =MyType.ConstVal
		/// =1+5;
		/// =(EnumVal1 + EnumVal2);
		/// =EnumVal1 | EnumVal2;
		/// ]]></summary>
		/// <returns></returns>
		string ParseAssignIdent(ref DNode parent) { return ParseAssignIdent(ref parent, false); }
		string ParseAssignIdent(ref DNode parent, bool isFuncParam)
		{
			string ret = "";

			int psb = 0;// ()
			bool hadQuestion = false;
			while (la.Kind != DTokens.EOF)
			{
				lexer.NextToken();
				DToken pk = Peek(1);

				if (ThrowIfEOF(DTokens.Semicolon)) { break; }

				if (la.Kind == DTokens.OpenCurlyBrace) { SkipToClosingBrace(); }

				if (la.Kind == DTokens.Question) // a<b?a:b;
				{
					hadQuestion = true;
				}

				if (la.Kind == DTokens.OpenParenthesis)
				{
					psb++;
					if (t.Kind == DTokens.Cast /* cast(...) */ || DTokens.Modifiers[t.Kind] /* const(char) */)
					{
						if (t.Kind == DTokens.Cast) ret += "(";
						ret += SkipToClosingParenthesis();
					}
					else if (t.Kind != DTokens.Identifier) // =new Thread(...);
					{
						StartPeek();
						bool isDelegate = false;
						// Seek for a delegate decl
						int tpsb = 1;
						while (pk.Kind != DTokens.EOF)
						{
							pk = Peek();
							if (pk.Kind == DTokens.CloseParenthesis)
							{
								tpsb--;
								if (tpsb < 0) break;
								if ((pk = Peek()).Kind == DTokens.OpenCurlyBrace)
									isDelegate = true;
							}
						}
						if (isDelegate)
						{
							DNode delegatefun = new DNode(FieldType.Delegate);
							delegatefun.startLoc = GetCodeLocation(la);
							delegatefun.type = "void";
							delegatefun.TypeToken = DTokens.Void;
							delegatefun.name = ""; // It's anonymous...
							delegatefun.modifiers.Add(DTokens.Private);
							ParseFunctionArguments(ref delegatefun);

							if (Peek(1).Kind == DTokens.OpenCurlyBrace)
							{
								psb--;
								lexer.NextToken(); // Skip ")"
								ParseBlock(ref delegatefun, true);
								delegatefun.endLoc = GetCodeLocation(la);
								Expect(DTokens.CloseCurlyBrace, "Error parsing anonymous delegate: ending \"}\" missing!");
								parent.Add(delegatefun);
							}
						}
						else
						{
							ret += SkipToClosingParenthesis();
						}
					}
				}
				if (la.Kind == DTokens.CloseParenthesis)
				{
					psb--;
					if (psb < 0)
					{
						if (!isFuncParam) ret += ")";
						else if (Peek(1).Kind != DTokens.OpenSquareBracket) break;
					}
				}

				if (la.Kind == DTokens.OpenSquareBracket) { ret += SkipToClosingSquares(); }

				if (psb < 1 &&
					(la.Kind == DTokens.Semicolon || la.Kind == DTokens.Comma || (la.Kind == DTokens.Colon && !hadQuestion) || la.Kind == DTokens.CloseCurlyBrace)
					) break;

				if (la.Kind == DTokens.Colon && hadQuestion) hadQuestion = false;

				ret += ((la.Kind == DTokens.Identifier || DTokens.BasicTypes[la.Kind]) ? " " : "") + strVal;

				if (la.Kind == (int)DTokens.Function || la.Kind == DTokens.Delegate)
				{
					if (Peek(1).Kind != DTokens.OpenParenthesis) // =delegate bool(...) {....}
					{
						DNode delegatefun = new DNode(FieldType.Delegate);
						delegatefun.startLoc = GetCodeLocation(la);
						delegatefun.TypeToken = la.Kind;
						ret += strVal;
						delegatefun.name = ""; // It's anonymous...
						delegatefun.modifiers.Add(DTokens.Private);
						lexer.NextToken(); // Skip "delegate" or "function"

						delegatefun.type = ParseTypeIdent();
						if (la.Kind != DTokens.OpenParenthesis) lexer.NextToken();

						ParseFunctionArguments(ref delegatefun);

						if (Peek(1).Kind == DTokens.OpenCurlyBrace)
						{
							psb--;
							lexer.NextToken(); // Skip ")"
							ParseBlock(ref delegatefun, true);
							delegatefun.endLoc = GetCodeLocation(la);
							Expect(DTokens.CloseCurlyBrace, "Error parsing anonymous delegate: ending \"}\" missing!");
							parent.Add(delegatefun);
						}

						SkipToSemicolon();
						return ret.Trim();
					}
					ret += "(";
				}
			}
			return ret.Trim();
		}

		/// <summary>
		/// MyType
		/// uint[]*
		/// const(char)*
		/// invariant(char)[]
		/// int[] function(char[int[]], int function() mf, ref string y)[]
		/// immutable(char)[] 
		/// </summary>
		/// <returns></returns>
		string ParseTypeIdent() { bool a; return ParseTypeIdent(false, out a); }
		private string ParseTypeIdent(bool identifierOnly)
		{
			bool a;
			return ParseTypeIdent(identifierOnly, out a);
		}
		string ParseTypeIdent(bool identifierOnly, out bool hasClampMod)
		{
			bool isDelegate = t != null && (t.Kind == DTokens.Delegate || t.Kind == DTokens.Function);

			hasClampMod = false; // const()

			if (DTokens.Modifiers[la.Kind] || la.Kind == DTokens.Typeof) hasClampMod = true;// const()

			bool t_hasClampMod = hasClampMod;

			string ret = strVal;

			if (la.Kind == DTokens.OpenParenthesis) // (*MyPointer)(...);
			{
				ret += SkipToClosingParenthesis();
				return ret;
			}

			DToken pk = null;
			switch (Peek(1).Kind)
			{
				default: // Parse things like MyType123[string[]]

					while (la.Kind != DTokens.EOF)
					{
						lexer.NextToken();
						if (ThrowIfEOF(DTokens.CloseParenthesis)) { break; }

						if (identifierOnly && la.Kind != DTokens.Identifier && la.Kind != DTokens.Dot && la.Kind != DTokens.Times)
							break;

						if (la.Kind == DTokens.Not && Peek(1).Kind == DTokens.OpenParenthesis)
						{
							lexer.NextToken(); // Skip "!"
							ret += "!(" + SkipToClosingParenthesis() + ")";
							t_hasClampMod = hasClampMod = true;
						}

						if (la.Kind == DTokens.OpenParenthesis)
						{
							if (!t_hasClampMod) break;
							ret += "(" + SkipToClosingParenthesis() + ")";
							t_hasClampMod = false;
						}
						if (la.Kind == DTokens.CloseParenthesis || la.Kind == DTokens.Times)
						{
							if (la.Kind == DTokens.Times) ret += "*";

							pk = Peek(1);
							if (pk.Kind != DTokens.OpenSquareBracket &&
								pk.Kind != DTokens.OpenParenthesis &&
								pk.Kind != DTokens.OpenSquareBracket &&
								pk.Kind != DTokens.Times)
							{
								break;
							}
						}

						if (la.Kind == DTokens.OpenSquareBracket)
						{
							ret += "[" + SkipToClosingSquares() + "]";
							//SynErr(la.Kind,"outgoing from "+ret);
							pk = Peek(1);
							if (pk.Kind != DTokens.OpenSquareBracket && pk.Kind != DTokens.OpenParenthesis && pk.Kind != DTokens.Times)
							{
								break;
							}
						}

						pk = Peek(1);

						if (pk.Kind == DTokens.OpenParenthesis && isDelegate) break; // =delegate List!(string)(int a,int b) {...};

						if (pk.Kind == DTokens.Function || pk.Kind == DTokens.Delegate) // List!(string) delegate() myDLG;
						{
							break;
						}

						if (t.Kind != DTokens.Not &&
							(la.Kind == DTokens.Semicolon || la.Kind == DTokens.Comma || la.Kind == DTokens.Colon || la.Kind == DTokens.CloseCurlyBrace)
							) break;
						if (t.Kind != DTokens.Minus && !t_hasClampMod && t.Kind != DTokens.Not &&
							(pk.Kind == DTokens.Semicolon || DTokens.AssignOps[pk.Kind] || pk.Kind == DTokens.Comma || pk.Kind == DTokens.Colon || pk.Kind == DTokens.CloseCurlyBrace)
							) break;


						if (la.Kind == DTokens.Identifier || DTokens.BasicTypes[la.Kind]) ret += (t.Kind != DTokens.Dot ? " " : "") + strVal;
						if (la.Kind == DTokens.Not || la.Kind == DTokens.Dot) ret += strVal;

						if ((la.Kind != DTokens.Not && la.Kind != DTokens.Dot) && pk.Kind == DTokens.Identifier) { break; } // const(char)
					}
					break;
				case DTokens.OpenParenthesis:
					// void MyFunc >(< ) {...}
					// const>(<char)[]
					if (la.Kind != DTokens.Identifier) { goto default; }
					break;
				case DTokens.Identifier:        // MyType >MyInst<
					if (la.Kind == DTokens.Minus)// int i=>-<1;
						goto default;
					break;
				case DTokens.Delegate:
				case DTokens.Function:
				case DTokens.Semicolon:         // int i>;<
				case DTokens.Colon:             // class MyClass >:< MyBase {...}
				case DTokens.OpenCurlyBrace:    // class MyClass : MyBase >{<...}
				case DTokens.Assign:            // int i >=< 0;
					// -> return directly things like MyType (ID string only)
					break;
			}

			return ret.Trim();
		}

		/// <summary>
		/// void main(string[] args) {}
		/// void expFunc();
		/// void delegate(int a,bool b) myDelegate;
		/// int i=45;
		/// MyType[] a;
		/// const(char)[] foo();
		/// this() {}
		/// </summary>
		/// <returns></returns>
		DNode ParseExpression()
		{
			DNode tv = new DNode();
			tv.desc = CheckForExpressionComments();
			tv.startLoc = GetCodeLocation(la);
			bool isCTor = la.Kind == DTokens.This;
			tv.TypeToken = la.Kind;
			if (!isCTor)
			{
				tv.type = ParseTypeIdent();

				if (DTokens.Conditions[la.Kind] || DTokens.Conditions[Peek(1).Kind])// b?foo(): bar();
				{
					SkipToSemicolon();
					return null;
				}

				// avoid things like std.stdio.writeln
				if (tv.type == "" || tv.type.EndsWith("."))
				{
					return null;
				}

				if (Peek(1).Kind == DTokens.Delegate || Peek(1).Kind == DTokens.Function)
				{
					lexer.NextToken(); // Skip last ID parsed by ParseTypeIdent();
					lexer.NextToken(); // Skip "delegate" or "function"
                    if (Expect(DTokens.OpenParenthesis, "Failed to parse delegate declaration - \"(\" missing!"))
                    {
                        tv.fieldtype = FieldType.Delegate;
                        ParseFunctionArguments(ref tv);
                        if (Expect(DTokens.CloseParenthesis, "Failed to parse delegate declaration - \")\" missing!"))
                        {
                            if (la.Kind != DTokens.Identifier)
                            {
                                SynErr(DTokens.Identifier, "Missing Identifier!");
                                return null;
                            }
                            tv.name = strVal; // Assign delegate name

                            // New: Also parse possible arguments that are written after the delegate name
                            // @property void function() ctor>()<;
                            if (Peek(1).Kind == DTokens.OpenParenthesis)
                            {
                                lexer.NextToken(); // Skip delegate id
                                lexer.NextToken(); // Skip opening parenthesis
                                ParseFunctionArguments(ref tv);
                                Expect(DTokens.CloseParenthesis, "Failed to parse delegate declaration - \")\" missing!");
                            }

                            tv.endLoc = GetCodeLocation(la);

                            if (Peek(1).Kind == DTokens.Assign) // int delegate(bool b) foo = (bool b) {...};
                            {
                                lexer.NextToken(); // Skip last ID
                                ParseAssignIdent(ref tv);
                            }

                            return tv;
                        }
                    }
				}
			}

			if (!isCTor) lexer.NextToken(); // Skip last ID parsed by ParseIdentifier();

			if (IsVarDecl())// int foo; TypeTuple!(a,T)[] a;
			{
				tv.fieldtype = FieldType.Variable;
				tv.name = ParseTypeIdent();

				if (Peek(1).Kind == DTokens.Assign)
					lexer.NextToken();

				DToken dt = la;

				if (la.Kind == DTokens.Assign)
					tv.value = ParseAssignIdent(ref tv);

				if (Peek(1).Kind == DTokens.Semicolon || Peek(1).Kind == DTokens.Comma)
					lexer.NextToken();

				if (la.Kind != DTokens.Comma)
				{
					if (la.Kind != DTokens.Semicolon)
					{
						CodeLocation cl = GetCodeLocation(dt);
						SynErr(dt.Kind, cl.Column, cl.Line, "Missing semicolon near declaration!");
						return tv;
					}
					SkipToSemicolon();
				}
			}
			else if (la.Kind == DTokens.Identifier || isCTor) // MyType myfunc() {...}; this()() {...}
			{
				tv.fieldtype = FieldType.Function;
				tv.name = strVal;
				lexer.NextToken(); // Skip function name

				if (!Expect(DTokens.OpenParenthesis, "Failure during Paramter parsing - \"(\" missing!")) { SkipToClosingBrace(); return null; }

				bool HasTemplateArgs = false;
				#region Scan for template arguments
				int psb = 0;
				DToken pk = la;
				lexer.StartPeek();
				if (pk.Kind == DTokens.OpenParenthesis) psb = -1;
				for (int i = 0; pk != null && pk.Kind != DTokens.EOF; i++)
				{
					if (pk.Kind == DTokens.OpenParenthesis) psb++;
					if (pk.Kind == DTokens.CloseParenthesis)
					{
						psb--;
						if (psb < 0)
						{
							if (lexer.Peek().Kind == DTokens.OpenParenthesis) HasTemplateArgs = true;
							break;
						}
					}
					pk = lexer.Peek();
				}
				#endregion

				if (la.Kind != DTokens.CloseParenthesis) // just if some arguments are given!
				{
					if (HasTemplateArgs)
						ParseTemplateArguments(ref tv);
					else
						ParseFunctionArguments(ref tv);
				}

				if (HasTemplateArgs) // void templFunc(S,T[],U*) (S s, int b=2) {...}
				{
					if (!Expect(DTokens.CloseParenthesis, "Failure during Paramter parsing - \")\" missing!")) { SkipToClosingBrace(); return null; }
					// la.Kind == "("
					if (Peek(1).Kind != DTokens.CloseParenthesis)
						ParseFunctionArguments(ref tv);
					else
						lexer.NextToken();// Skip "("
				}

				if (!Expect(DTokens.CloseParenthesis, "Failure during Paramter parsing - \")\" missing!")) { SkipToClosingBrace(); return null; }

				if (la.Kind == DTokens.Assign) // this() = null;
				{
					tv.value = SkipToSemicolon();
					tv.endLoc = GetCodeLocation(la);
					return tv;
				}

				if (DTokens.Modifiers[la.Kind] && la.Kind != DTokens.In && la.Kind != DTokens.Out && la.Kind != DTokens.Body) // void foo() const if(...) {...}
				{
					lexer.NextToken();
				}

				if (la.Kind == DTokens.If)
				{
					lexer.NextToken(); // Skip "if"
					SkipToClosingParenthesis();
					lexer.NextToken(); // Skip ")"
				}

				if (la.Kind == DTokens.Semicolon) { goto expr_ret; } // void foo()();

				if (DTokens.Modifiers[la.Kind] && la.Kind != DTokens.In && la.Kind != DTokens.Out && la.Kind != DTokens.Body) // void foo() const {...}
				{
					lexer.NextToken();
				}

				if (la.Kind == DTokens.OpenCurlyBrace)// normal function void foo() >{<}
				{
					Location sloc = tv.StartLocation;
					ParseBlock(ref tv, true);
					tv.StartLocation = sloc;
					goto expr_ret;
				}

			in_out_body:
				if (la.Kind == DTokens.In || la.Kind == DTokens.Out || la.Kind == DTokens.Body) // void foo() in{}body{}
				{

					if (la.Kind == DTokens.Out)
					{
						if (Peek(1).Kind == DTokens.OpenParenthesis)
						{
							lexer.NextToken(); // Skip "out"
							SkipToClosingParenthesis();
						}
					}
					lexer.NextToken(); // Skip "in"

					Location sloc = tv.StartLocation;
					ParseBlock(ref tv, true);
					tv.StartLocation = sloc;

					DToken bpk = Peek(1);
					if (bpk.Kind == DTokens.In || bpk.Kind == DTokens.Out || bpk.Kind == DTokens.Body)
					{
						lexer.NextToken(); // Skip "}"
						goto in_out_body;
					}
					goto expr_ret;
				}

				SynErr(la.Kind, "unexpected end of function body!");
				return null;
			}
			else
			{
				return null;
			}

		expr_ret:
			return tv;
		}

		/// <summary>
		/// Parses a complete class, template or struct
		/// public class MyType(T,S*,U[]): public Mybase, MyInterface {...}
		/// </summary>
		/// <returns></returns>
		DNode ParseClass()
		{
			DNode myc = new DNode(FieldType.Class); // >class<
			myc.desc = CheckForExpressionComments();
			if (la.Kind == DTokens.Struct) myc.fieldtype = FieldType.Struct;
			if (la.Kind == DTokens.Template) myc.fieldtype = FieldType.Template;
			if (la.Kind == DTokens.Interface) myc.fieldtype = FieldType.Interface;
			myc.TypeToken = la.Kind;
			lexer.NextToken(); // Skip initial type ID ,e.g. "class"

			#region Apply vis modifiers
			bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
			foreach (int m in BlockModifiers)
			{
				if (!ExpressionModifiers.Contains(m))
				{
					if (cvm) if (DTokens.VisModifiers[m]) continue;
					ExpressionModifiers.Add(m);
				}
			}
			myc.modifiers.AddRange(ExpressionModifiers);
			ExpressionModifiers.Clear();
			#endregion

			if (la.Kind != DTokens.Identifier)
			{
				SynErr(DTokens.Identifier, "Identifier required!");
				return null;
			}

			myc.name = strVal; // >MyType<
			lexer.NextToken(); // Skip last ID parsed by ParseIdentifier();

			if (la.Kind == DTokens.Semicolon) return myc;

			// >(T,S,U[])<
			if (la.Kind == DTokens.OpenParenthesis) // "(" template declaration
			{
				ParseTemplateArguments(ref myc);
				if (!Expect(DTokens.CloseParenthesis, "Failure during template paramter parsing - \")\" missing!")) { SkipToClosingBrace(); }
			}

			if (myc.name != "Object" && myc.fieldtype != FieldType.Struct) myc.superClass = "Object"; // Every object except the Object class itself has "Object" as its base class!
			// >: MyBase, MyInterface< {...}
			if (la.Kind == DTokens.Colon) // : inheritance
			{
				while (!EOF) // Skip modifiers or module paths
				{
					lexer.NextToken();
					if (DTokens.Modifiers[la.Kind])
						continue;// Skip heritage modifier
					if (Peek(1).Kind == DTokens.Dot)// : std.Class
						continue;
					else if (la.Kind != DTokens.Dot)
					{
						break;
					}
				}
				myc.superClass = ParseTypeIdent();
				if (la.Kind == DTokens.Comma)
				{
					lexer.NextToken(); // Skip ","
					myc.implementedInterface = ParseTypeIdent();
				}
				lexer.NextToken(); // Skip to "{"
			}
			if (myc.superClass == myc.name)
			{
				SemErr(DTokens.Colon, "Cannot inherit \"" + myc.name + "\" from itself!");
				myc.superClass = "";
			}

			if (la.Kind == DTokens.If)
			{
				lexer.NextToken();
				SkipToClosingParenthesis();
				lexer.NextToken(); // Skip ")"
			}

			if (la.Kind != DTokens.OpenCurlyBrace)
			{
				SynErr(DTokens.OpenCurlyBrace, "Error parsing " + DTokens.GetTokenString(myc.TypeToken) + " " + myc.name + ": missing {");
				return myc;
			}

			ParseBlock(ref myc, false);

			myc.endLoc = GetCodeLocation(la);

			return myc;
		}

		/// <summary>
		/// Parses an enumeration
		/// enum myType mt=null;
		/// enum:uint {
		/// a=1,
		/// b=23,
		/// c=2,
		/// d,
		/// }
		/// </summary>
		/// <returns></returns>
		DNode ParseEnum()
		{
			DNode mye = new DNode(FieldType.Enum);
			mye.startLoc = GetCodeLocation(la);

			mye.type = strVal;
			mye.superClass = "int";

			#region Apply vis modifiers
			bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
			foreach (int m in BlockModifiers)
			{
				if (!ExpressionModifiers.Contains(m))
				{
					if (cvm) if (DTokens.VisModifiers[m]) continue;
					ExpressionModifiers.Add(m);
				}
			}
			mye.modifiers.AddRange(ExpressionModifiers);
			ExpressionModifiers.Clear();
			#endregion

			#region check for single declarations such as enum MyType i=4;
			DToken pk = la;
			lexer.StartPeek();
			int psb = 0;
			for (int i = 0; pk != null && pk.Kind != DTokens.EOF; i++)
			{
				if (pk.Kind == DTokens.OpenParenthesis) psb++;
				if (pk.Kind == DTokens.CloseParenthesis) psb--;
				if (psb < 1 && pk.Kind == DTokens.OpenCurlyBrace) // enum Name:Type >{<
				{
					break;
				}
				if (psb < 1 && (pk.Kind == DTokens.Semicolon || pk.Kind==DTokens.Assign)) // enum Type Name=Value;
				{
					int cbrace = 0;
					mye.fieldtype = FieldType.Variable;
					mye.superClass = "";
					mye.type = "";
					while (!EOF)
					{
						lexer.NextToken();

						if (la.Kind == DTokens.OpenParenthesis) psb++;
						if (la.Kind == DTokens.CloseParenthesis) psb--;

						if (psb < 1 && cbrace < 1 && la.Kind == DTokens.Semicolon) break;
						if (psb < 1 && IsVarDecl()) // enum MyType Ident= Value; or enum MyType Ident;
						{
							mye.name = strVal;
							lexer.NextToken(); // Skip ID
                            if (la.Kind == DTokens.Assign) {
                                lexer.NextToken();
                                mye.value = SkipToSemicolon();
                            }
							break;
						}else
						mye.type += strVal;
					}

					if (mye.type == "")
					{
						mye.type = "int";
						mye.TypeToken = DTokens.Int;
					}
					mye.endLoc = GetCodeLocation(la);
					return mye;
				}
				pk = lexer.Peek();
			}
			#endregion

			lexer.NextToken(); // Skip initial "enum"

			if (la.Kind == DTokens.Identifier) // Enum name
			{
				mye.name = strVal;
				lexer.NextToken();
			}

			if (la.Kind == DTokens.Colon) // Enum base type
			{
				// la = ":"
				mye.superClass = "";
				while (!EOF && la.Kind != DTokens.OpenCurlyBrace)
				{
					lexer.NextToken();
					if (la.Kind == DTokens.OpenParenthesis) { mye.superClass += "(" + SkipToClosingParenthesis(); }
					if (la.Kind == DTokens.OpenSquareBracket) { mye.superClass += "[" + SkipToClosingSquares(); }
				}
				// la = "{"
				if (la.Kind != DTokens.OpenCurlyBrace)
				{
					if (!PeekMustBe(DTokens.OpenCurlyBrace, "Error parsing Enum: missing \"{\"!"))
					{
						mye.endLoc = GetCodeLocation(la);
						return mye;
					}
					lexer.NextToken(); // Skip name
				}
			}

			if (la.Kind != DTokens.OpenCurlyBrace) // Check beginning "{"
			{
				SynErr(DTokens.OpenCurlyBrace);
				SkipToClosingBrace();
				mye.endLoc = GetCodeLocation(la);
				return mye;
			}

			DNode tt = new DNode(FieldType.EnumValue);
			while (!EOF)
			{
				lexer.NextToken();
			enumcont:
				if (tt == null) tt = new DNode(FieldType.EnumValue);
				if (ThrowIfEOF(DTokens.CloseCurlyBrace)) break;
				switch (la.Kind)
				{
					case DTokens.CloseCurlyBrace: // Final "}"
						//MessageBox.Show("}");
						if (tt.name != "") mye.Add(tt);
						mye.endLoc = GetCodeLocation(la);
						return mye;
					case DTokens.Comma: // Next item
						//MessageBox.Show(tt.name+" Comma");
						tt.endLoc = GetCodeLocation(la);
						if (tt.name != "") mye.Add(tt);
						tt = null;
						break;
					case DTokens.Assign: // Value assigment
						tt.value = ParseAssignIdent(ref mye);
						//MessageBox.Show("Set " + tt.name + " to " + tt.value);
						if (la.Kind != DTokens.Identifier) goto enumcont;
						break;
					case DTokens.Identifier: // Can just be a new item
						tt.type = mye.superClass;
						tt.startLoc = GetCodeLocation(la);
						tt.name = ParseTypeIdent();
						if (la.Kind != DTokens.Identifier) goto enumcont;
						//MessageBox.Show("New one called " + tt.name);
						break;
					default: /*SynErr(la.Kind);*/ break;
				}
			}
			mye.endLoc = GetCodeLocation(la);
			return mye;
		}







		#region Error handlers
		public delegate void ErrorHandler(string file, string module, int line, int col, int kindOf, string message);
		static public event ErrorHandler OnError, OnSemanticError;


		void SynErr(int n, int col, int ln)
		{
			OnError(PhysFileName, Document.module, ln, col, n, "");
			//errors.SynErr(ln, col, n);
		}
		void SynErr(int n, int col, int ln, string msg)
		{
			OnError(PhysFileName, Document.module, ln, col, n, msg);
			//errors.Error(ln, col, msg);
		}
		void SynErr(int n, string msg)
		{
			OnError(PhysFileName, Document.module, la.Location.Line, la.Location.Column, n, msg);
			//errors.Error(la.Location.Line, la.Location.Column, msg);
		}
		void SynErr(int n)
		{
			OnError(PhysFileName, Document != null ? Document.module : null, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, "");
			//errors.SynErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
		}

		void SemErr(int n, int col, int ln)
		{
			OnSemanticError(PhysFileName, Document.module, ln, col, n, "");
			//errors.SemErr(ln, col, n);
		}
		void SemErr(int n, int col, int ln, string msg)
		{
			OnSemanticError(PhysFileName, Document.module, ln, col, n, msg);
			//errors.Error(ln, col, msg);
		}
		void SemErr(int n, string msg)
		{
			OnSemanticError(PhysFileName, Document.module, la.Location.Line, la.Location.Column, n, msg);
			//errors.Error(la.Location.Line, la.Location.Column, msg);
		}
		void SemErr(int n)
		{
			OnSemanticError(PhysFileName, Document != null ? Document.module : null, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, "");
			//errors.SemErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
		}
		#endregion
	}
}
