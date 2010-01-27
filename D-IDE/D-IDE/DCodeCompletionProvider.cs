using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.NRefactory;

using D_Parser;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.NRefactory.Ast;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace D_IDE
{
	public partial class DCodeCompletionProvider : ICompletionDataProvider
	{
		/// <summary>
		/// classA.classB.memberC
		/// </summary>
		/// <param name="mouseOffset"></param>
		/// <param name="isNewConstructor"></param>
		/// <returns><![CDATA[string["classA","classB","memberC"]]]></returns>
		public static string[] GetExpressionStringsAtOffset(ref int mouseOffset, out bool isNewConstructor, bool backwardOnly)
		{
			int origOff = mouseOffset;
			isNewConstructor = false;
			try
			{
				List<string> expressions = new List<string>();
				if (mouseOffset < 1 || Form1.SelectedTabPage == null) return null;
				TextArea ta = Form1.SelectedTabPage.txt.ActiveTextAreaControl.TextArea;

				char tch;
				string texpr = "";
				int psb = 0;
				for (; mouseOffset > 0 && mouseOffset < ta.Document.TextLength; mouseOffset--)
				{
					tch = ta.Document.GetCharAt(mouseOffset);

					if (tch == ']') psb++;

					if (char.IsLetterOrDigit(tch) || tch == '_' || psb > 0) texpr += tch;

					if (!char.IsLetterOrDigit(tch) && tch != '_' && psb < 1)
					{
						if (mouseOffset > 3 && ta.Document.TextContent.Substring(mouseOffset - 3, 3) == "new") isNewConstructor = true; // =>new< MyClass()
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
					if (!char.IsLetterOrDigit(ta.Document.GetCharAt(off)) && ta.Document.GetCharAt(off) != '_') return null;
					for (int i = off + 1; i > 0 && i < ta.Document.TextLength - 1; i++) // Parse forward
					{
						tch = ta.Document.GetCharAt(i);
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
		public static DataType FindActualExpression(DProject prj, DModule local, CodeLocation caretLocation, string[] expressions, bool dotPressed, out bool isSuper, out bool isInstance, out bool isNameSpace, out DModule module)
		{
			module = local;
			isSuper = false;
			isInstance = false;
			isNameSpace = false;
			try
			{
				int i = 0;
				if (expressions == null || expressions.Length < 1) return null;

				DataType seldt = null, seldd = null; // Selected DataType - Will be returned later

				if (expressions[0] == "this")
				{
					seldt = GetClassAt(local.dom, caretLocation);
					i++;
				}
				else if (expressions[0] == "super")
				{
					seldt = GetClassAt(local.dom, caretLocation);
					if (seldt != null)
					{
						if (seldt.superClass == "")
						{
							seldt = SearchGlobalExpr(prj, local, seldt.superClass, false, out module);
						}
					}
					isSuper = true;
					i++;
				}
				else
				{
					seldt = SearchGlobalExpr(prj, local, RemoveArrayOrTemplatePartFromDecl(expressions[0]), true, out module);
					if (seldt == null) seldt = SearchExprInClassHierarchy(local.dom, RemoveArrayOrTemplatePartFromDecl(expressions[0]));

					if (seldt != null) i++;
					if (seldt != null && ((dotPressed && (seldt.fieldtype == FieldType.Function || seldt.fieldtype == FieldType.AliasDecl)) ||
						(seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken]))
					  )
					{
						seldd = seldt;
						seldt = SearchGlobalExpr(prj, local, RemoveArrayOrTemplatePartFromDecl(seldt.type), true, out module);
						if (seldt == null) seldt = seldt = SearchExprInClassHierarchy(local.dom, RemoveArrayOrTemplatePartFromDecl(expressions[0]));//SearchGlobalExpr(prj, local, RemoveArrayOrTemplatePartFromDecl(seldd.type), false, out module);
						isInstance = true;
					}

					#region Seek in global and local(project) namespaces
					if (seldt == null) // if there wasn't still anything found in global space
					{
						string modpath = "";
						List<DModule> dmods = new List<DModule>(D_IDE_Properties.GlobalModules),
							dmods2 = new List<DModule>();
						dmods.AddRange(prj.files);

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

							module = null;
							seldt = null;

							foreach (DModule gpf in dmods)
							{
								if (gpf.mod.StartsWith(modpath, StringComparison.Ordinal))
								{
									dmods2.Add(gpf);
									module = gpf;
									seldt = gpf.dom;
									if (gpf.mod == modpath) // if this module has the same path as equally typed in the editor, take this as the only one
									{
										dmods2.Clear();
										dmods2.Add(gpf);
										break;
									}
								}
							}

							if (dmods2.Count < 1) { i--; continue; }
							isNameSpace = true;
							if (dmods2.Count == 1 && dmods2[0].mod == modpath)
							{
								break;
							}

							if ((module = prj.FileDataByFile(modpath)) == null)
								module = D_IDE_Properties.Default[modpath];

							seldt = new DataType(FieldType.Root);
							seldt.module = modpath;
							if (module != null)
							{
								seldt.module = module.mod;
								seldt.Children = module.Children;
								seldt.endLoc = module.dom.endLoc;
							}

							foreach (DModule dm in dmods2)
							{
								seldt.Add(dm.dom);
							}
							break;
						}
					}
				}
					#endregion

				for (; i < expressions.Length && seldt != null; i++)
				{
					isInstance = false;
					seldt = SearchExprInClassHierarchy(seldt, RemoveArrayOrTemplatePartFromDecl(expressions[i]));
					if (seldt == null) break;

					if ((seldt.fieldtype == FieldType.Function || seldt.fieldtype == FieldType.AliasDecl ||
							(seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken])
						)
					  )
					{
						seldd = seldt;
						seldt = SearchGlobalExpr(prj, local, RemoveArrayOrTemplatePartFromDecl(seldt.type), true, out module);
						if (seldt == null)
							seldt = SearchGlobalExpr(prj, local, RemoveArrayOrTemplatePartFromDecl(seldd.type), false, out module);
						isInstance = true;
					}
				}

				return seldt;
			}
			catch (Exception ex)
			{
				Form1.thisForm.Log(ex.Message);
			}
			return null;
		}

		public static void GenerateCurrentGlobalData(TextArea ta, DProject project, DModule module)
		{
			globalList.Clear();

			CodeLocation tl = new CodeLocation(ta.Caret.Column + 1, ta.Caret.Line + 1);
			DataType seldt, seldd;
			seldt = DCodeCompletionProvider.GetBlockAt(module.dom, tl);
			if (seldt != null)
			{/*
				if(!DTokens.ClassLike[seldt.TypeToken])
				{
					//seldd = DCodeCompletionProvider.GetClassAt(module.dom, tl);
					//if(seldd != null)
						DCodeCompletionProvider.AddAllClassMembers(seldt, ref DCodeCompletionProvider.globalList, true, Form1.thisForm.icons);
				}
				else*/
				DCodeCompletionProvider.AddAllClassMembers(seldt, ref DCodeCompletionProvider.globalList, true, Form1.thisForm.icons);
			}
			if (project != null && project.files.Contains(module))
				foreach (DModule ppf in project.files)
				{
					if (!ppf.IsParsable) continue;

					AddAllClassMembers(ppf.dom, ref DCodeCompletionProvider.globalList, false, Form1.thisForm.icons);
				}
			else // Add classes etc from current module
				AddAllClassMembers(module.dom, ref DCodeCompletionProvider.globalList, true, Form1.thisForm.icons);

			AddGlobalSpaceContent(ref DCodeCompletionProvider.globalList, Form1.thisForm.icons);
		}

		public ICompletionData[] GenerateCompletionData(string fn, TextArea ta, char ch)
		{
			List<ICompletionData> rl = new List<ICompletionData>();
			List<string> expressions = new List<string>();
			try
			{
				DModule pf = Form1.SelectedTabPage.fileData;

				CodeLocation tl = new CodeLocation(ta.Caret.Column + 1, ta.Caret.Line + 1);
				DataType seldt, seldd;

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
					rl.AddRange(globalList);
					presel = null;
				}

				if (expressions.Count < 1) return rl.ToArray();

				expressions.Reverse();
				#endregion

				if (ch != '.' && expressions.Count == 1 && expressions[0].Length < 2 && KeyWord < 0) // Reflect entire cache content including D KeyWords
				{
					presel = expressions[expressions.Count - 1];
					rl.AddRange(globalList);
					rl.Sort();
					return rl.ToArray();
				}

				if (ch == '.')
				{
					#region A.B.c>.<
					presel = null; // Important: After a typed dot ".", set previous selection string to null!
					DModule gpf = null;
					seldt = FindActualExpression(prj, pf, tl, expressions.ToArray(), ch == '.', out isSuper, out isInst, out isNameSpace, out gpf);

					if (seldt == null) return rl.ToArray();

					if (seldt.fieldtype == FieldType.Function	//||(seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken])
					   )
					{
						seldd = seldt;
						seldt = SearchGlobalExpr(pf.dom, seldt.type);
						isInst = true;
					}

					if (seldt != null)
					{
						if (expressions[0] == "this" && expressions.Count < 2) // this.
						{
							AddAllClassMembers(seldt, ref rl, true, icons);

							foreach (DataType arg in seldt.param)
							{
								if (arg.type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
						}
						else if (expressions[0] == "super" && expressions.Count < 2) // super.
						{
							if (seldt.superClass != "")
							{
								seldd = SearchGlobalExpr(pf.dom, seldt.superClass);
								if (seldd != null)
								{
									AddAllClassMembers(seldd, ref rl, true, icons);

									foreach (DataType arg in seldd.param)
									{
										if (arg.type == null || arg.name == null) continue;
										rl.Add(new DCompletionData(arg, seldd, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
									}
								}
							}
						}
						else if (seldt.fieldtype == FieldType.Enum && seldt.Count > 0) // Flags.
						{
							foreach (DataType dt in seldt)
							{
								rl.Add(new DCompletionData(dt, seldt, icons));
							}
						}
						else if (seldt.fieldtype == FieldType.Variable) // myVar.
						{
							AddAllClassMembers(seldt, ref rl, false, icons);
							AddTypeStd(seldt, ref rl);

							#region Add function which have seldt.name as first parameter
							/*foreach(DModule gpf in D_IDE_Properties.GlobalModules)
						{
							foreach(DataType gch in gpf.dom.children)
							{
								if(gch.fieldtype != FieldType.Variable && gch.param.Count > 0 && (gch.modifiers.Contains(DTokens.Public) || gch.modifiers.Count < 1))
								{
									foreach(DataType param in gch.param)
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

						foreach(DataType gch in pf.dom.children)
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
								AddAllClassMembers(seldt, ref rl, !isNameSpace, icons);
							}
							else
							{
								foreach (DataType dt in seldt)
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
										rl.Add(new DCompletionData(dt, seldt, icons));
								}
							}
							if (!isNameSpace) AddTypeStd(seldt, ref rl);

							foreach (DataType arg in seldt.param)
							{
								if (arg.type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
						}
					}
					#endregion
				}
			}
			catch (Exception ex)
			{
				Form1.thisForm.Log(ex.Message);
			}
			rl.Sort();
			return rl.ToArray();
		}

		public static void AddGlobalSpaceContent(ref List<ICompletionData> rl, ImageList icons)
		{
			List<string> mods = new List<string>();
			string tmod;

			foreach (DModule gpf in D_IDE_Properties.GlobalModules)
			{
				if (!gpf.IsParsable) continue;
				if (!String.IsNullOrEmpty(gpf.mod))
				{
					tmod = gpf.mod.Split('.')[0];
					if (!mods.Contains(tmod)) mods.Add(tmod);
				}

				AddAllClassMembers(gpf.dom, ref rl, false, icons);
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

		private void AddTypeStd(DataType seldt, ref List<ICompletionData> rl)
		{
			rl.Add(new DCompletionData("sizeof", "Yields the memory usage of a type in bytes", icons.Images.IndexOfKey("Icons.16x16.Literal.png")));
			rl.Add(new DCompletionData("stringof", "Returns a string of the typename", icons.Images.IndexOfKey("Icons.16x16.Property.png")));
			rl.Add(new DCompletionData("init", "Returns the default initializer of a type", icons.Images.IndexOfKey("Icons.16x16.Field.png")));
		}
	}
}
