﻿using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Gui.InsightWindow;
using ICSharpCode.TextEditor;
using ICSharpCode.NRefactory;
using D_Parser;
using System.Windows.Forms;

namespace D_IDE
{
	class InsightWindowProvider : IInsightDataProvider
	{
		public List<string> data;
		DocumentInstanceWindow diw;
		int initialOffset = 0;
		char key;

		public InsightWindowProvider(DocumentInstanceWindow instWin, char keyPressed)
		{
			key = keyPressed;
			diw = instWin;
			data = new List<string>();
		}

		#region IInsightDataProvider Member

		bool IInsightDataProvider.CaretOffsetChanged()
		{
			bool closeDataProvider = diw.txt.ActiveTextAreaControl.Caret.Offset <= initialOffset;
			int brackets = 0;
			int curlyBrackets = 0;
			if (!closeDataProvider)
			{
				bool insideChar = false;
				bool insideString = false;
				for (int offset = initialOffset; offset < Math.Min(diw.txt.ActiveTextAreaControl.Caret.Offset, diw.txt.Document.TextLength); ++offset)
				{
					char ch = diw.txt.Document.GetCharAt(offset);
					switch (ch)
					{
						case '\'':
							insideChar = !insideChar;
							break;
						case '(':
							if (!(insideChar || insideString))
							{
								++brackets;
							}
							break;
						case ')':
							if (!(insideChar || insideString))
							{
								--brackets;
							}
							if (brackets <= 0)
							{
								return true;
							}
							break;
						case '"':
							insideString = !insideString;
							break;
						case '}':
							if (!(insideChar || insideString))
							{
								--curlyBrackets;
							}
							if (curlyBrackets < 0)
							{
								return true;
							}
							break;
						case '{':
							if (!(insideChar || insideString))
							{
								++curlyBrackets;
							}
							break;
						case ';':
							if (!(insideChar || insideString))
							{
								return true;
							}
							break;
					}
				}
			}
			return closeDataProvider;
		}

		int IInsightDataProvider.DefaultIndex
		{
			get { return 0; }
		}

		string IInsightDataProvider.GetInsightData(int number)
		{
			return data[number];
		}

		int IInsightDataProvider.InsightDataCount
		{
			get { return data.Count; }
		}

		void IInsightDataProvider.SetupDataProvider(string fileName, TextArea ta)
		{
			initialOffset = ta.Caret.Offset;

			if (key == ',')
			{
				char tch;
				int psb = 0;
				for (int off = initialOffset; off > 0; off--)
				{
					tch = ta.Document.GetCharAt(off);

					if (tch == ')' || tch == '}' || tch == ']') psb++;
					if (tch == '(' || tch == '{' || tch == '[') psb--;

					if (psb < 1 && tch == '(')
					{
						initialOffset = off;
						break;
					}
				}
			}

			CodeLocation caretLocation = new CodeLocation(ta.Caret.Column - 1, ta.Caret.Line - 1);
			bool ctor = false;
			int newOff = initialOffset - 1;
			int i = 0;
			string[] expressions = DCodeCompletionProvider.GetExpressionStringsAtOffset(ref newOff, out ctor, true);

			if (expressions == null || expressions.Length < 1) return;

			DataType seldt = null; // Selected DataType
			DModule module = null;

			if (expressions[0] == "this")
			{
				seldt = DCodeCompletionProvider.GetClassAt(diw.fileData.dom, caretLocation);

				if (expressions.Length < 2)
				{
					foreach (DataType dt in DCodeCompletionProvider.GetExprsByName(seldt, seldt.name, false))
					{
						data.Add(DCompletionData.BuildDescriptionString(dt));
					}
					return;
				}
			}
			else if (expressions[0] == "super")
			{
				seldt = DCodeCompletionProvider.GetClassAt(diw.fileData.dom, caretLocation);
				if (seldt != null && seldt.superClass != "")
				{
					seldt = DCodeCompletionProvider.SearchGlobalExpr(diw.fileData.dom, seldt.superClass);

					if (seldt != null && expressions.Length < 2)
					{
						foreach (DataType dt in DCodeCompletionProvider.GetExprsByName(seldt, seldt.name, false))
						{
							data.Add(DCompletionData.BuildDescriptionString(dt));
						}
						return;
					}
				}
			}
			else
			{
				if (expressions.Length > 1)
				{
					seldt = DCodeCompletionProvider.SearchGlobalExpr(diw.project, diw.fileData, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0]), true, out module);
					if (seldt == null) seldt = DCodeCompletionProvider.SearchExprInClassHierarchy(diw.fileData.dom, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0]));
					
					if (seldt != null) i++;
					if (seldt != null && ((seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken]) || seldt.fieldtype == FieldType.AliasDecl))
					{
						seldt = DCodeCompletionProvider.SearchGlobalExpr(diw.fileData.dom, seldt.type);
					}
				}
				else
				{
					if (ctor)
					{
						seldt = DCodeCompletionProvider.SearchGlobalExpr(diw.project, diw.fileData, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0]), true, out module);
						if (seldt == null) seldt = DCodeCompletionProvider.SearchExprInClassHierarchy(diw.fileData.dom, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0]));
						
						if (seldt != null && seldt.Count > 0)
						{
							foreach (DataType dt in seldt.Children)
								if (dt.fieldtype == FieldType.Constructor)
									data.Add(DCompletionData.BuildDescriptionString(dt));
						}
						return;
					}
					seldt = DCodeCompletionProvider.GetClassAt(diw.fileData.dom, caretLocation);

					if (seldt != null)
					{
						List<DataType> dts = DCodeCompletionProvider.SearchExprsInClassHierarchy(seldt, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0]));
						if (dts != null)
							foreach (DataType dt in dts)
							{
								data.Add(DCompletionData.BuildDescriptionString(dt));
							}
					}

					foreach (DataType dt in DCodeCompletionProvider.SearchGlobalExprs(diw.project,diw.fileData.dom, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[0])))
					{
						data.Add(DCompletionData.BuildDescriptionString(dt));
					}
					return;
				}
			}




			
			#region Seek in global and local(project) namespaces
			if (seldt == null) // if there wasn't still anything found in global space
			{
				string modpath = "";
				List<DModule> dmods = new List<DModule>(D_IDE_Properties.GlobalModules),
					dmods2 = new List<DModule>();
				if(diw.project!=null)dmods.AddRange(diw.project.files);

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
					if (dmods2.Count == 1 && dmods2[0].mod == modpath)
					{
						break;
					}

					if ((module = diw.project.FileDataByFile(modpath)) == null)
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
			#endregion




			for (; i < expressions.Length && seldt != null; i++)
			{
				if (i == expressions.Length - 1) // One before the last one
				{
					List<DataType> tt = DCodeCompletionProvider.SearchExprsInClassHierarchy(seldt, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[i]));
					if (tt != null)
						foreach (DataType dt in tt)
						{
							data.Add(DCompletionData.BuildDescriptionString(dt));
						}
					break;
				}

				seldt = DCodeCompletionProvider.SearchExprInClassHierarchy(seldt, DCodeCompletionProvider.RemoveArrayOrTemplatePartFromDecl(expressions[i]));
				if (seldt == null) break;

				if ((seldt.fieldtype == FieldType.Function || seldt.fieldtype == FieldType.AliasDecl || (seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken])) && i < expressions.Length - 1)
				{
					seldt = DCodeCompletionProvider.SearchGlobalExpr(diw.fileData.dom, seldt.type);
				}
			}


		}

		#endregion
	}

}
