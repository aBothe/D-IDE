using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.NRefactory;

using D_Parser;
using ICSharpCode.TextEditor.Document;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace D_IDE
{
	public partial class DCodeCompletionProvider : ICompletionDataProvider
	{
		public CompilerConfiguration cc=D_IDE_Properties.Default.DefaultCompiler;
		/// <summary>
		/// classA.classB.memberC
		/// </summary>
		/// <param name="mouseOffset"></param>
		/// <param name="isNewConstructor"></param>
		/// <returns><![CDATA[string["classA","classB","memberC"]]]></returns>
		public static string[] GetExpressionStringsAtOffset(string TextContent, ref int mouseOffset, out bool isNewConstructor, bool backwardOnly)
		{
			int origOff = mouseOffset;
			isNewConstructor = false;
			if (mouseOffset < 1 || String.IsNullOrEmpty(TextContent)) return null;

			int TextLength = TextContent.Length;

			try
			{
				List<string> expressions = new List<string>();

				char tch;
				string texpr = "";
				int psb = 0;
				for (; mouseOffset > 0 && mouseOffset < TextLength; mouseOffset--)
				{
					tch = TextContent[mouseOffset];

					if (tch == ']') psb++;

					if (char.IsLetterOrDigit(tch) || tch == '_' || psb > 0) texpr += tch;

					if (!char.IsLetterOrDigit(tch) && tch != '_' && psb < 1)
					{
						if (mouseOffset > 3 && TextContent.Substring(mouseOffset - 3, 3) == "new") isNewConstructor = true; // =>new< MyClass()
						expressions.Add(ReverseString(texpr.Trim()));

						if (tch != '.')
						{
							break;
						}
						texpr = "";
						//off = i;
					}

					if (tch == '[') psb--;
				}

				int off = origOff;
				texpr = "";

				if (!backwardOnly)
				{
					if (!char.IsLetterOrDigit(TextContent[off]) && TextContent[off] != '_') return null;
					for (int i = off + 1; i > 0 && i < TextLength - 1; i++) // Parse forward
					{
						tch = TextContent[i];
						if (!char.IsLetterOrDigit(tch) && tch != '_') break;
						if (expressions.Count < 1) expressions.Add("");
						texpr += tch;
					}
					expressions[0] += texpr.Trim();
				}

				expressions.Reverse();

				if (expressions[0].Trim() == "") return null;
				return expressions.ToArray();
			}
			catch { }
			return null;
		}

		/// <summary>
		/// Reinterpretes all given expression strings to scan the global class hierarchy and find the member called like the last given expression
		/// </summary>
		/// <param name="local"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		public static DNode FindActualExpression(DProject prj, CodeModule local, CodeLocation caretLocation, string[] expressions, bool dotPressed, bool ResolveBaseType, out bool isSuper, out bool isInstance, out bool isNameSpace, out CodeModule module)
		{
            CompilerConfiguration cc = prj != null ? prj.Compiler : D_IDE_Properties.Default.DefaultCompiler;
			module = local;
			isSuper = false;
			isInstance = false;
			isNameSpace = false;
			try
			{
				int i = 0;
				if (expressions == null || expressions.Length < 1) return null;

				DNode seldt = null, seldd = null; // Selected DNode - Will be returned later

				if (expressions[0] == "this")
				{
					seldt = GetClassAt(local, caretLocation);
					i++;
				}
				else if (expressions[0] == "super")
				{
					seldt = GetClassAt(local.dom, caretLocation);
					if (seldt is DClassLike && (seldt as DClassLike).BaseClasses.Count>0)
					{
                        seldt = SearchGlobalExpr(prj, local, (seldt as DClassLike).BaseClasses[0].ToString(), false, out module);
					}
					isSuper = true;
					i++;
				}
				else
				{
					// Search expression in all superior blocks
					DNode cblock = GetBlockAt(local.dom, caretLocation);
					seldt = SearchExprInClassHierarchyBackward(cc,cblock, RemoveTemplatePartFromDecl(expressions[0]));
					// Search expression in current module root first
					if (seldt == null)	seldt = SearchGlobalExpr(prj, local, RemoveTemplatePartFromDecl(expressions[0]), true, out module);
					// If there wasn't found anything, search deeper and recursive
					//if (seldt == null) seldt = SearchExprInClassHierarchy(local.dom, GetBlockAt(local.dom, caretLocation), RemoveArrayOrTemplatePartFromDecl(expressions[0]));
					// EDIT: Don't search recursively in all blocks of local.dom because you'd resolve something you couldn't access...

					// If seldt is a variable, resolve its basic type such as a class etc
					
					seldd = seldt;
					bool IsLastInChain = i >= expressions.Length - 1;
					if ((ResolveBaseType && IsLastInChain) || !IsLastInChain) seldt = ResolveReturnOrBaseType(prj, local, seldt, IsLastInChain);
					if (seldt != seldd) isInstance = true;
					if (seldt != null) i++;

					#region Seek in global and local(project) namespace names
					if (seldt == null) // if there wasn't still anything found in global space
					{
						string modpath = "";
						string[] modpath_packages;
						List<CodeModule> dmods = new List<CodeModule>(cc.GlobalModules),
							dmods2 = new List<CodeModule>();
						if(prj!=null)dmods.AddRange(prj.files);// Very important: add the project's files to the search list

						i = expressions.Length;
						/*
						 * i=0	i=1			i=2			i=3
						 * std.
						 * std.	socket.
						 * std. socketstream
						 * std.	windows.	windows.
						 * std.	c.			stdio.		printf();
						 * std.	complex
						 */
						while (i > 0)
						{
							modpath = "";
							for (int _i = 0; _i < i; _i++) modpath += (_i > 0 ? "." : "") + expressions[_i];
							modpath_packages = modpath.Split('.');
							module = null;
							seldt = null;

							foreach (CodeModule gpf in dmods)
							{
								if (gpf.ModuleName.StartsWith(modpath, StringComparison.Ordinal))
								{
									string[] path_packages = gpf.ModuleName.Split('.');
									dmods2.Add(gpf);
									module = gpf;
									seldt = gpf.dom;
									if (gpf.ModuleName == modpath) // if this module has the same path as equally typed in the editor, take this as the only one
									{
										dmods2.Clear();
										dmods2.Add(gpf);
										break;
									}
								}
							}

							if (dmods2.Count < 1) { i--; continue; }
							isNameSpace = true;

							if (prj == null || (module = prj.FileDataByFile(modpath)) == null)
								module = D_IDE_Properties.Default.GetModule(D_IDE_Properties.Default.DefaultCompiler,modpath);

							if (dmods2.Count == 1 && dmods2[0].ModuleName == modpath)
							{
								break;
							}

							//Create a synthetic node which only contains module names
							seldt = new DNode(FieldType.Root);
							seldt.module = modpath;
							if (module != null)
							{
								seldt.module = module.ModuleName;
								seldt.children = module.Children;
								seldt.endLoc = module.dom.endLoc;
							}

							foreach (CodeModule dm in dmods2)
							{
								seldt.Add(dm.dom);
							}
							break;
						}
					}
					#endregion
				}

				for (; i < expressions.Length && seldt != null; i++)
				{
					isInstance = false;
					seldt = SearchExprInClassHierarchy(cc,seldt, null, RemoveTemplatePartFromDecl(expressions[i]));
					if (seldt == null) break;

					seldd = seldt;
					bool IsLastInChain=i == expressions.Length - 1;
					if((ResolveBaseType && IsLastInChain) || !IsLastInChain)seldt = ResolveReturnOrBaseType(prj, local, seldt, IsLastInChain);
					if (seldt != seldd) isInstance = true;
				}

				return seldt;
			}
			catch (Exception ex)
			{
				D_IDEForm.thisForm.Log(ex.Message);
			}
			return null;
		}

		public ICompletionData[] GenerateCompletionData(string fn, TextArea ta, char ch)
		{
			var icons = D_IDEForm.icons;

			var rl = new List<ICompletionData>();
			var expressions = new List<string>();
			try
			{
				DocumentInstanceWindow diw = D_IDEForm.SelectedTabPage;
				DProject project = diw.OwnerProject;
				if(project!=null)cc = project.Compiler;
				var pf = diw.Module;

				var tl = new Location(ta.Caret.Column + 1, ta.Caret.Line + 1);
				DNode seldt, seldd;

				int off = ta.Caret.Offset;

				bool isInst = false; // Here the return type of a function is the base type for which the data will be generated
				bool isSuper = false;
				bool isNameSpace = false;

				#region Compute expressions based on caret location
				char tch;
				string texpr = "";
				int KeyWord = -1, psb = 0;
				for (int i = off - 1; i > 0; i--)
				{
					tch = ta.Document.GetCharAt(i);

					if (tch == ']') psb++;

					if (char.IsLetterOrDigit(tch) || tch == '_' || psb > 0) texpr += tch;

					if (!char.IsLetterOrDigit(tch) && tch != '_' && psb < 1)
					{
						if (texpr == "") break;
						texpr = ReverseString(texpr);
						if (KeyWord < 0 && (KeyWord = DKeywords.GetToken(texpr)) >= 0 && texpr != "this" && texpr != "super")
						{
							break;
						}
						else
						{
							expressions.Add(texpr);
						}
						texpr = "";

						if (!char.IsWhiteSpace(tch) && tch != ';' && tch != '.')
						{
							break;
						}
						off = i;
					}
					if (tch == '[') psb--;
				}

				if (KeyWord == DTokens.New && expressions.Count < 1)
				{
					rl.AddRange(cc.GlobalCompletionList);
					presel = null;
					return rl.ToArray();
				}

				if (expressions.Count < 1 && ch != '\0') return rl.ToArray();

				expressions.Reverse();
				#endregion

				if (expressions.Count < 1)
				{
					if (ch == '\0') expressions.Add("");
					else return null;
				}

				if (ch != '.' && (expressions.Count == 1 && expressions[0].Length < 2) && KeyWord < 0) // Reflect entire cache content including D KeyWords
				{
					if (expressions.Count > 0) presel = expressions[expressions.Count - 1];
					else presel = null;
					rl.AddRange(diw.CurrentCompletionData);

					//rl.Sort();
					return rl.ToArray();
				}

				if (ch == '.')
				{
					#region A.B.c>.<
					presel = null; // Important: After a typed dot ".", set previous selection string to null!
					CodeModule gpf = null;

					seldt = FindActualExpression(project, pf, tl, expressions.ToArray(), ch == '.', true, out isSuper, out isInst, out isNameSpace, out gpf);

					if (seldt == null) return rl.ToArray();
					//Debugger.Log(0,"parsing", DCompletionData.BuildDescriptionString(seldt.Parent) + " " + DCompletionData.BuildDescriptionString(seldt));

					//seldd = seldt;
					//seldt = ResolveReturnOrBaseType(prj, pf, seldt, expressions.Count==2);
					if (seldt.fieldtype == FieldType.Function	//||(seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken])
					   )
					{
						seldd = seldt;
						seldt = SearchGlobalExpr(cc,pf.dom, seldt.Type.ToString());
						isInst = true;
					}

					if (seldt != null)
					{
						if (expressions[0] == "this" && expressions.Count < 2) // this.
						{
							AddAllClassMembers(cc,seldt, ref rl, true);

							foreach (DNode arg in (seldt as DMethod).Parameters)
							{
								if (arg.Type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
						}
						else if (expressions[0] == "super" && expressions.Count < 2) // super.
						{
							if (seldt is DClassLike && (seldt as DClassLike).BaseClasses.Count>0)
							{
                                foreach (D_Parser.TypeDeclaration td in (seldt as DClassLike).BaseClasses)
                                {
                                    seldd = SearchGlobalExpr(cc, pf.dom, td.ToString());
                                    if (seldd != null)
                                    {
                                        AddAllClassMembers(cc, seldd, ref rl, true);

                                        foreach (DNode arg in (seldt as DMethod).Parameters)
                                        {
                                            if (arg.Type == null || arg.name == null) continue;
                                            rl.Add(new DCompletionData(arg, seldd, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
                                        }
                                    }
                                }
							}
						}
						else if (seldt.fieldtype == FieldType.Enum && seldt.Count > 0) // Flags.
						{
							foreach (DNode dt in seldt)
							{
								rl.Add(new DCompletionData(dt, seldt));
							}
						}
						else if (seldt.fieldtype == FieldType.Variable) // myVar.
						{
							AddAllClassMembers(cc,seldt, ref rl, false);
							AddTypeStd(seldt, ref rl);

							#region Add function which have seldt.name as first parameter
							/*foreach(DModule gpf in D_IDE_Properties.GlobalModules)
						{
							foreach(DNode gch in gpf.dom.children)
							{
								if(gch.fieldtype != FieldType.Variable && gch.param.Count > 0 && (gch.modifiers.Contains(DTokens.Public) || gch.modifiers.Count < 1))
								{
									foreach(DNode param in gch.param)
									{
										if(param.name.Length < 1) continue; // Skip on template params
										if(param.type == seldt.name)
										{
											rl.Add(new DCompletionData(gch, seldt, icons));
											break;
										}
									}
								}
							}
						}

						foreach(DNode gch in pf.dom.children)
						{
							if(gch.fieldtype == FieldType.Function && gch.param.Count > 0 && gch.param[0].type == seldt.name)
							{
								rl.Add(new DCompletionData(gch, seldt, icons));
							}
						}*/
							#endregion
						}
						else // e.g. MessageBox>.<
						{
							if (isInst || isNameSpace)
							{
								AddAllClassMembers(cc,seldt, ref rl, !isNameSpace);
							}
							else
							{
								foreach (DNode dt in seldt)
								{
									if (
										//showAll ||
										(isSuper && dt.modifiers.Contains(DTokens.Protected)) || // super.ProtectedMember
										(isInst && dt.modifiers.Contains(DTokens.Public)) || // (MyType) foo().MyMember
										(dt.modifiers.Contains(DTokens.Static) && // 
										(dt.modifiers.Contains(DTokens.Public)  // 
										|| dt.modifiers.Count < 2)) ||
										(dt.fieldtype == FieldType.EnumValue && // 
											(dt.modifiers.Contains(DTokens.Public)  // 
											|| dt.modifiers.Count < 2)
										)
										) // int a;
										rl.Add(new DCompletionData(dt, seldt));
								}
							}
							if (!isNameSpace) AddTypeStd(seldt, ref rl);

							foreach (DNode arg in seldt.TemplateParameters)
							{
								if (arg.Type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
                            if(seldt is DMethod)
                                foreach (DNode arg in (seldt as DMethod).Parameters)
                                {
                                    if (arg.Type == null || arg.name == null) continue;
                                    rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
                                }
						}
					}
					#endregion
				}
			}
			catch (Exception ex)
			{
				D_IDEForm.thisForm.Log(ex.Message);
			}
			//rl.Sort();
			return rl.ToArray();
		}

		public static void AddGlobalSpaceContent(CompilerConfiguration cc,ref List<ICompletionData> rl)
		{
			ImageList icons = D_IDEForm.icons;
			var mods = new List<string>();
			string[] tmods;
			string tmod;

			foreach (CodeModule gpf in cc.GlobalModules)
			{
				if (!gpf.IsParsable) continue;
				if (!String.IsNullOrEmpty(gpf.ModuleName))
				{
					tmods = gpf.ModuleName.Split('.');
					tmod = tmods[0];
					if (!mods.Contains(tmod)) mods.Add(tmod);
				}

				AddAllClassMembers(cc,gpf.dom, ref rl, false);
			}

			foreach (string mod in mods)
			{
				rl.Add(new DCompletionData(mod, "Module", icons.Images.IndexOfKey("namespace")));
			}

			foreach (string kw in DKeywords.keywordList)
			{
				if (kw != "")
					rl.Add(new DCompletionData(kw, DTokens.GetDescription(kw), icons.Images.IndexOfKey("code")));
			}
		}

		private void AddTypeStd(DNode seldt, ref List<ICompletionData> rl)
		{
			ImageList icons = D_IDEForm.icons;
			rl.Add(new DCompletionData("sizeof", "Yields the memory usage of a type in bytes", icons.Images.IndexOfKey("Icons.16x16.Literal.png")));
			rl.Add(new DCompletionData("stringof", "Returns a string of the typename", icons.Images.IndexOfKey("Icons.16x16.Property.png")));
			rl.Add(new DCompletionData("init", "Returns the default initializer of a type", icons.Images.IndexOfKey("Icons.16x16.Field.png")));
		}

		/// <summary>
		/// Resolves either the return type of a method or the base type of a variable
		/// </summary>
		/// <param name="prj"></param>
		/// <param name="local"></param>
		/// <param name="owner"></param>
		/// <param name="isLastInExpressionChain">This value is needed for resolving functions because if this parameter is true then it returns the owner node</param>
		/// <returns></returns>
		public static DNode ResolveReturnOrBaseType(DProject prj, CodeModule local, DNode owner, bool isLastInExpressionChain)
		{
			if (owner == null) return null;
            CompilerConfiguration cc = prj != null ? prj.Compiler : D_IDE_Properties.Default.DefaultCompiler;
			DNode ret = owner;
			CodeModule mod = null;
			if ((!DTokens.BasicTypes[(int)owner.TypeToken] && owner.fieldtype == FieldType.Variable) || ((owner.fieldtype == FieldType.Function || owner.fieldtype == FieldType.AliasDecl) && !isLastInExpressionChain))
			{
				ret = DCodeCompletionProvider.SearchExprInClassHierarchy(cc,(DNode)owner.Parent, null, RemoveTemplatePartFromDecl(owner.Type.ToString()));
				if (ret == null)
					ret = DCodeCompletionProvider.SearchGlobalExpr(prj, local, RemoveTemplatePartFromDecl(owner.Type.ToString()), false, out mod);
			}
			return ret;
		}

		/// <summary>
		/// A subroutine for ResolveMultipleNodes
		/// </summary><see cref="ResolveMultipleNodes"/>
		/// <param name="prj"></param>
		/// <param name="local"></param>
		/// <param name="parent"></param>
		/// <param name="i"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		static List<DNode> _res(DProject prj, CodeModule local, DNode parent, int i, string[] expressions)
		{
			List<DNode> tl = new List<DNode>();
			if (expressions == null || i >= expressions.Length)
			{
				tl.Add(parent);
				return tl;
			}

			foreach (DNode dt in GetExprsByName(parent, expressions[i], true))
			{
				DNode seldt = ResolveReturnOrBaseType(prj, local, dt, i >= expressions.Length - 1);

				if (seldt == null) seldt = dt;

				tl.AddRange(_res(prj, local, seldt, i + 1, expressions));
			}
			return tl;
		}

		/// <summary>
		/// Searches nodes in global space which have the same name and returns all of these
		/// </summary>
		/// <param name="prj"></param>
		/// <param name="local"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		public static List<DNode> ResolveMultipleNodes(DProject prj, CodeModule local, string[] expressions)
		{
			if (expressions == null || expressions.Length < 1) return new List<DNode>();

			List<DNode> rl = SearchGlobalExprs(prj, local.dom, expressions[0]);
			if (expressions.Length < 2 || rl.Count < 1) return rl;

			List<DNode> ret = new List<DNode>();
			foreach (DNode dt in rl)
			{
				DNode seldt = ResolveReturnOrBaseType(prj, local, dt, expressions.Length == 2);
				if (seldt == null) seldt = dt;
				ret.AddRange(_res(prj, local, seldt, 1, expressions));
			}
			return ret;
		}
	}
}
