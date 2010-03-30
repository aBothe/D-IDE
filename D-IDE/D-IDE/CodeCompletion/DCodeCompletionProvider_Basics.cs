using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Windows.Forms;
using D_IDE;
using ICSharpCode.TextEditor;
using D_Parser;

namespace D_IDE
{
	public partial class DCodeCompletionProvider
	{
		public static int ParseCurLevel(string t, int offset)
		{
			int ret = 0;
			if (offset > t.Length) offset = t.Length;

			for (int i = 0; i < offset; i++)
			{
				if (t[i] == '{') ret++;
				if (t[i] == '}') ret--;
			}
			return ret;
		}
		public static bool isInCommentAreaOrString(string t, int offset)
		{
			bool commenting = false, multicomm = false, inString=false;
			for (int i = 0; i < offset; i++)
			{
				char c = t[i];

				if (c == '"' && t[i > 0 ? (i - 1) : 0] != '\\') inString = !inString;

				if (i >= 1)
				{
					if (t[i - 1] == '/' && (c == '/' || c == '+')) { commenting = true; }
					if (c == '*' && t[i - 1] == '/') { multicomm = true; }


					if (multicomm && (t[i - 1] == '*' && c == '/'))
					{
						multicomm = false; continue;
					}

					if (commenting)
						if ((t[i - 1] == '+' && c == '/') || c == '\n')
						{
							commenting = false; continue;
						}
				}
			}
			return (commenting || multicomm || inString);
		}

		public static DataType GetBlockAt(DataType dataType, TextLocation textLocation)
		{
			return GetBlockAt(dataType, new CodeLocation(textLocation.Column + 1, textLocation.Line + 1));
		}
		public static DataType GetBlockAt(DataType env, CodeLocation where)
		{
			if (env!=null && where!=null && where >= env.startLoc && where <= env.endLoc && env.Count > 0)
			{
				foreach (DataType dt in env)
				{
					dt.Parent = env;
					if (where >= dt.startLoc && where <= dt.endLoc)
					{
						return GetBlockAt(dt, where);
					}
				}
			}
			return env;
		}

		public static DataType GetClassAt(DataType env, CodeLocation where)
		{
			if (where >= env.startLoc && where <= env.endLoc && env.Count > 0)
			{
				foreach (DataType dt in env)
				{
					dt.Parent = env;
					if (where >= dt.startLoc && where <= dt.endLoc && DTokens.ClassLike[dt.TypeToken])
					{
						return GetClassAt(dt, where);
					}
				}
			}
			return env;
		}

		#region Props
		public DCodeCompletionProvider(/*ref DProject dprj, */)
		{
			presel = null;
			//this.prj = dprj;
		}

		public ImageList ImageList
		{
			get
			{
				return Form1.icons;
			}
		}

		string presel;
		int defIndex;
		public string PreSelection
		{
			get
			{
				return presel;
			}
			set
			{

				presel = value;
			}
		}

		public int DefaultIndex
		{
			get
			{
				return defIndex;
			}
			set
			{
				defIndex = value;
			}
		}
		#endregion

		public static DataType GetRoot(DataType dt)
		{
			if (dt == null) return null;
			if (dt.fieldtype == FieldType.Root) return dt;
			return GetRoot((DataType)dt.Parent);
		}

		public CompletionDataProviderKeyResult ProcessKey(char key)
		{
			if (char.IsLetterOrDigit(key) || key == '_')
			{
				return CompletionDataProviderKeyResult.NormalKey;
			}
			else
			{
				// key triggers insertion of selected items
				return CompletionDataProviderKeyResult.InsertionKey;
			}
		}

		public static DataType GetExprByName(DataType env, CodeLocation where, string name)
		{
			if (env == null) return null;
			if (env.name == name) return env;

			if (where >= env.startLoc && where <= env.endLoc)
			{
				if (env.Count > 0)
				{
					foreach (DataType dt in env)
					{
						dt.Parent = env;
						if (dt.name != name)
						{
							if (where >= dt.startLoc && where <= dt.endLoc)
							{
								return GetExprByName(dt, where, name);
							}
						}
						else
							return dt;
					}
				}
			}
			return null;
		}
		public static DataType GetExprByName(DataType env, string name)
		{
			return GetExprByName(env, name, false);
		}
		public static DataType GetExprByName(DataType env, string name, bool RootOnly)
		{
			if (env == null) return null;

			if (env.name == name) { return env; }

			if (env.param.Count > 0)
				foreach (DataType dt in env.param)
				{
					dt.Parent = env;
					if (dt.name == name)
						return dt;
				}

			if (env.Count > 0)
				foreach (DataType dt in env)
				{
					if (dt == env) continue;
					dt.Parent = env;
					if (dt.name == name)
						return dt;

					if (!RootOnly)
					{
						DataType tdt = GetExprByName(dt, name, false);
						if (tdt != null) return tdt;
					}
				}
			return null;
		}
		public static DataType GetExprByName(DataType env,DataType levelnode, string name, bool RootOnly)
		{
			if (env == null) return null;

			if (env.name == name) { return env; }

			if (env.param.Count > 0)
				foreach (DataType dt in env.param)
				{
					dt.Parent = env;
					if (dt.name == name)
						return dt;
				}

			if (env.Count > 0)
				foreach (DataType dt in env)
				{
					if (dt == env) continue;
					dt.Parent = env;
					if (dt.name == name)
						return dt;

					if (!RootOnly)
					{
						DataType tdt = GetExprByName(dt, levelnode, name, dt == levelnode);
						if (tdt != null) return tdt;
					}
				}
			return null;
		}
		public static List<DataType> GetExprsByName(DataType env, string name, bool RootOnly)
		{
			List<DataType> ret = new List<DataType>();
			if (env == null) return ret;

			//if(env.name == name) {return ret; }

			if (env.param.Count > 0)
				foreach (DataType dt in env.param)
				{
					dt.Parent = env;
					if (dt.name == name)
						ret.Add(dt);
				}

			if (env.Count > 0)
				foreach (DataType dt in env)
				{
					dt.Parent = env;
					if (dt.name == name)
						ret.Add(dt);

					if (!RootOnly) ret.AddRange(GetExprsByName(dt, name, RootOnly));
				}
			return ret;
		}

		public static DataType SearchExprInClassHierarchy(DataType env, DataType currentLevelNode, string name)
		{
			if (env == null) return null;

			if (env.name == name) { return env; }

			if (env.param.Count > 0)
				foreach (DataType dt in env.param)
				{
					dt.Parent = env;
					if (dt.name == name)
						return dt;
				}

			if (env.Count > 0)
				foreach (DataType dt in env)
				{
					dt.Parent = env;
					if (dt.name == name)
						return dt;

					DataType tdt = GetExprByName(dt, currentLevelNode, name, dt == currentLevelNode);
						if (tdt != null) return tdt;
				}

			string super = env.superClass;// Should be superior class
			/*if(!DTokens.ClassLike[env.TypeToken] && !exact)
			{
				super = env.type;
			}*/
			if (super != "")
			{
				DataType dt = SearchGlobalExpr(null, super);
				if (dt == null) return null;
				return SearchExprInClassHierarchy(dt,currentLevelNode, name);
			}

			return null;
		}
		public static List<DataType> SearchExprsInClassHierarchy(DataType env, string name)
		{
			List<DataType> ret = new List<DataType>();
			if (env == null) return ret;
			if (env.name == name) { ret.Add(env); return ret; }

			if (env.param.Count > 0)
				foreach (DataType dt in env.param)
				{
					dt.Parent = env;
					if (dt.name == name)
						ret.Add(dt);
				}

			if (env.Count > 0)
				foreach (DataType dt in env)
				{
					dt.Parent = env;
					if (dt.name == name)
					{
						if (dt.fieldtype != FieldType.AliasDecl)
							ret.Add(dt);
						else
						{
							ret.AddRange(SearchGlobalExprs(null, null, dt.type));
							continue;
						}
					}
					ret.AddRange(GetExprsByName(dt, name, false));
				}

			string super = env.superClass;// Should be superior class
			if (super != "")
			{
				DataType dt = SearchGlobalExpr(null, super);
				if (dt == null) return null;
				ret.AddRange(SearchExprsInClassHierarchy(dt, name));
			}

			return ret;
		}
		public static DataType SearchExprInClassHierarchyBackward(DataType node, string name)
		{
			if (node == null) return null;

			if (node.name == name) { return node; }

			DataType dt = GetExprByName(node,name,true);
			if (dt != null) return dt;

			string super = node.superClass;// Should be superior class
			/*if(!DTokens.ClassLike[env.TypeToken] && !exact)
			{
				super = env.type;
			}*/
			if (super != "")
			{
				dt = SearchGlobalExpr(null, super);
				if (dt != null)
				{
					return SearchExprInClassHierarchyBackward(dt, name);
				}
			}

			return SearchExprInClassHierarchyBackward((DataType)node.Parent,name);
		}

		public static DataType SearchGlobalExpr(DataType local, string expr)
		{
			return SearchGlobalExpr(local, expr, false);
		}
		public static DataType SearchGlobalExpr(DataType local, string expr, bool RootOnly)
		{
			DataType ret = null;

			if (local != null) ret = GetExprByName(local, expr, RootOnly);
			if (ret != null) return ret;

			foreach (DModule gpf in D_IDE_Properties.GlobalModules)
			{
				ret = GetExprByName(gpf.dom, expr, RootOnly);
				if (ret != null) return ret;
			}
			return null;
		}
		public static DataType SearchGlobalExpr(DProject prj, DModule local, string expr, bool RootOnly, out DModule module)
		{
			module = null;
			DataType ret = null;

			if (local != null) ret = GetExprByName(local.dom, expr, RootOnly);
			if (ret != null) { module = local; return ret; }

			if (prj != null)
				foreach (DModule ppf in prj.files)
				{
					if (local != null && ppf.mod_file == local.mod_file) continue;
					ret = GetExprByName(ppf.dom, expr, RootOnly);
					if (ret != null)
					{
						module = ppf;
						return ret;
					}
				}

			foreach (DModule gpf in D_IDE_Properties.GlobalModules)
			{
				ret = GetExprByName(gpf.dom, expr, RootOnly);
				if (ret != null)
				{
					module = gpf;
					return ret;
				}
			}
			return null;
		}

		/// <summary>
		/// In the case of multiple overloads of one function or field name, seek in the whole cache for these Fields
		/// </summary>
		/// <param name="local"></param>
		/// <param name="expr"></param>
		/// <param name="exact"></param>
		/// <returns></returns>
		public static List<DataType> SearchGlobalExprs(DProject prj, DataType local, string expr)
		{
			return SearchGlobalExprs(prj, local, expr, false);
		}
		///<see cref="SearchGlobalExprs"/>
		public static List<DataType> SearchGlobalExprs(DProject prj, DataType local, string expr, bool RootOnly)
		{
			List<DataType> ret = new List<DataType>();

			if (local != null) ret.AddRange(GetExprsByName(local, expr, RootOnly));

			if (prj != null)
				foreach (DModule ppf in prj.files)
				{
					ret.AddRange(GetExprsByName(ppf.dom, expr, RootOnly));
				}

			foreach (DModule gpf in D_IDE_Properties.GlobalModules)
			{
				ret.AddRange(GetExprsByName(gpf.dom, expr, RootOnly));
			}
			return ret;
		}

		public static string ReverseString(string s)
		{
			if (s.Length < 1) return s;
			char[] ret = new char[s.Length];
			for (int i = s.Length; i > 0; i--)
			{
				ret[s.Length - i] = s[i - 1];
			}
			return new string(ret);
		}

		/// <summary>
		/// Adds all class members and these of superior classes
		/// </summary>
		/// <param name="selectedExpression"></param>
		/// <param name="rl"></param>
		public static void AddAllClassMembers(DataType selectedExpression, ref List<ICompletionData> rl, bool all)
		{
			ImageList icons = Form1.icons;
			if (selectedExpression != null)
			{
				foreach (DataType ch in selectedExpression)
				{
					ch.Parent = selectedExpression;
					if ((!ch.modifiers.Contains(DTokens.Private) || all) && ch.fieldtype!=FieldType.Constructor) // Exlude ctors because they aren't needed
						rl.Add(new DCompletionData(ch, selectedExpression));
				}

				foreach (DataType arg in selectedExpression.param)
				{
					rl.Add(new DCompletionData(arg, selectedExpression, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
				}

				if (selectedExpression.superClass == "")
				{
					//TODO: Find out what gets affected by returning here
					//return;
					if (selectedExpression.fieldtype!=FieldType.Variable && !DTokens.ClassLike[selectedExpression.TypeToken] && selectedExpression.Parent != null && (selectedExpression.Parent as DataType).fieldtype != FieldType.Root)
						AddAllClassMembers((DataType)selectedExpression.Parent, ref rl, all);
				}
				else
				{
					// if not, add items of all superior classes or interfaces
					DataType dt = SearchGlobalExpr(null, selectedExpression.superClass); // Should be superior class
					AddAllClassMembers(dt, ref rl, false);
				}
			}
		}

		public static void AddAllClassMembers(DataType selectedExpression, ref List<ICompletionData> rl, bool all, bool exact, string searchExpr)
		{
			ImageList icons = Form1.icons;

			if (selectedExpression != null)
			{
				foreach (DataType ch in selectedExpression)
				{
					DCompletionData cd = new DCompletionData(ch, selectedExpression);
					if (exact)
					{
						if (ch.name != searchExpr) continue;
					}
					else
					{
						if (!ch.name.StartsWith(searchExpr, StringComparison.CurrentCultureIgnoreCase)) continue;
					}

					if (!ch.modifiers.Contains(DTokens.Private)/*(ch.modifiers.Contains(DTokens.Public) || ch.modifiers.Count < 1)// also take non modified fields
						|| (ch.fieldtype == FieldType.EnumValue)*/
																   || all)
						rl.Add(cd);
				}
				foreach (DataType arg in (selectedExpression as DataType).param)
				{
					if (exact)
					{
						if (arg.name != searchExpr) continue;
					}
					else
					{
						if (!arg.name.StartsWith(searchExpr, StringComparison.CurrentCultureIgnoreCase)) continue;
					}
					rl.Add(new DCompletionData(arg, selectedExpression, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
				}
				if ((selectedExpression as DataType).superClass == "") return;
				DataType dt = SearchGlobalExpr(null, (selectedExpression as DataType).superClass); // Should be superior class
				if (dt == null) return;
				AddAllClassMembers(dt, ref rl, all, exact, searchExpr);
			}
		}

		/// <summary>
		/// Called when entry should be inserted. Forward to the insertion action of the completion data.
		/// </summary>
		public bool InsertAction(ICompletionData idata, TextArea ta, int off, char key)
		{
			if (idata == null) return false;
			int o = 0;

			for (int i = off - 1; i > 0; i--)
			{
				if (!char.IsLetterOrDigit(ta.MotherTextEditorControl.Text[i]) && ta.MotherTextEditorControl.Text[i] != '_')
				{
					o = i + 1;
					break;
				}
			}

			presel = idata.Text;
			ta.Document.Replace(o, off - o, idata.Text);
			ta.Caret.Column = ta.Document.OffsetToPosition(o + idata.Text.Length).Column;
			return true;
		}

		public static string RemoveTemplatePartFromDecl(string expr)
		{
			string ret = expr;
			int t = ret.IndexOf('!');
			if (t >= 0)
			{
				ret = ret.Remove(t);
			}
			return ret;
		}
		/// <summary>
		/// int[][] --->> int[]
		/// </summary>
		/// <param name="expr"></param>
		/// <returns></returns>
		public static string RemoveArrayPartFromDecl(string expr)
		{
			string ret = expr;
			int t = ret.LastIndexOf('[');
			if (t >= 0)
			{
				ret = ret.Remove(t);
			}
			return ret;
		}

		/// <summary>
		/// immutable(char)[] --->> char[]
		/// </summary>
		/// <param name="expr"></param>
		/// <returns></returns>
		public static string RemoveAttributeFromDecl(string expr)
		{
			string ret = expr;
			int t = ret.IndexOf('(');
			if(t>0)
			{
				ret = ret.Remove(0,t+1);
				t = ret.IndexOf(')');
				if (t > 0)
					ret = ret.Remove(t,1);
			}
			return ret;
		}
	}
}
