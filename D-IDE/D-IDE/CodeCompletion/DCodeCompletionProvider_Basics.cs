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

        public class Commenting
        {
            public static int IndexOf(string HayStack, bool Nested, int Start)
            {
                string Needle = Nested ? "+/" : "*/";
                char cur = '\0';
                int off = Start;
                bool IsInString = false;
                int block = 0, nested = 0;

                while (off < HayStack.Length)
                {
                    cur = HayStack[off];

                    // String check
                    if (cur == '\"' && (off < 1 || HayStack[off - 1] != '\\'))
                    {
                        IsInString = !IsInString;
                    }

                    if (!IsInString && (cur == '/') && (HayStack[off + 1] == '*' || HayStack[off + 1] == '+'))
                    {
                        if (HayStack[off + 1] == '*')
                            block++;
                        else
                            nested++;

                        off += 2;
                        continue;
                    }

                    if (!IsInString && cur == Needle[0])
                    {
                        if (off + Needle.Length >= HayStack.Length)
                            return -1;

                        if (HayStack.Substring(off, Needle.Length) == Needle)
                        {
                            if (Nested) nested--; else block--;

                            if ((Nested ? nested : block) < 0) // that value has to be -1 because we started to count at 0
                                return off;

                            off++; // Skip + or *
                        }

                        if (HayStack.Substring(off, 2) == (Nested ? "*/" : "+/"))
                        {
                            if (Nested) block--; else nested--;
                            off++;
                        }
                    }

                    off++;
                }
                return -1;
            }

            public static int LastIndexOf(string HayStack, bool Nested, int Start)
            {
                string Needle = Nested ? "/+" : "/*";
                char cur = '\0', prev = '\0';
                int off = Start;
                bool IsInString = false;
                int block = 0, nested = 0;

                while (off >= 0)
                {
                    cur = HayStack[off];
                    if (off > 0) prev = HayStack[off - 1];

                    // String check
                    if (cur == '\"' && (off < 1 || HayStack[off - 1] != '\\'))
                    {
                        IsInString = !IsInString;
                    }

                    if (!IsInString && (cur == '+' || cur == '*') && HayStack[off + 1] == '/')
                    {
                        if (cur == '*')
                            block--;
                        else
                            nested--;

                        off -= 2;
                        continue;
                    }

                    if (!IsInString && cur == '/')
                    {
                        if (HayStack.Substring(off, Needle.Length) == Needle)
                        {
                            if (Nested) nested++; else block++;

                            if ((Nested ? nested : block) >= 1)
                                return off;
                        }

                        if (HayStack.Substring(off, 2) == (Nested ? "/*" : "/+"))
                        {
                            if (Nested) block++; else nested++;
                            off--;
                        }
                    }

                    off--;
                }
                return -1;
            }

            public static void IsInCommentAreaOrString(string Text, int Offset, out bool IsInString, out bool IsInLineComment, out bool IsInBlockComment, out bool IsInNestedBlockComment)
            {
                char cur = '\0', peekChar = '\0';
                int off = 0;
                IsInString = IsInLineComment = IsInBlockComment = IsInNestedBlockComment = false;

                while (off < Offset - 1)
                {
                    cur = Text[off];
                    if (off < Text.Length - 1) peekChar = Text[off + 1];

                    // String check
                    if (!IsInLineComment && !IsInBlockComment && !IsInNestedBlockComment && cur == '\"' && (off < 1 || Text[off - 1] != '\\'))
                        IsInString = !IsInString;

                    if (!IsInString)
                    {
                        // Line comment check
                        if (!IsInBlockComment && !IsInNestedBlockComment)
                        {
                            if (cur == '/' && peekChar == '/')
                                IsInLineComment = true;
                            if (IsInLineComment && cur == '\n')
                                IsInLineComment = false;
                        }

                        // Block comment check
                        if (cur == '/' && peekChar == '*')
                            IsInBlockComment = true;
                        if (IsInBlockComment && cur == '*' && peekChar == '/')
                            IsInBlockComment = false;

                        // Nested comment check
                        if (!IsInString && cur == '/' && peekChar == '+')
                            IsInNestedBlockComment = true;
                        if (IsInNestedBlockComment && cur == '+' && peekChar == '/')
                            IsInNestedBlockComment = false;
                    }

                    off++;
                }
            }

            public static bool IsInCommentAreaOrString(string Text, int Offset)
            {
                bool a, b, c, d;
                IsInCommentAreaOrString(Text, Offset, out a, out b, out c, out d);

                return a || b || c || d;
            }
        }

        public static DNode GetBlockAt(DNode dataType, TextLocation textLocation)
        {
            return GetBlockAt(dataType, new CodeLocation(textLocation.Column + 1, textLocation.Line + 1));
        }
        public static DNode GetBlockAt(DNode env, CodeLocation where)
        {
            if (env != null && where != null && where >= env.startLoc && where <= env.endLoc && env.Count > 0)
            {
                foreach (DNode dt in env)
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

        public static DNode GetClassAt(DNode env, CodeLocation where)
        {
            if (where >= env.startLoc && where <= env.endLoc && env.Count > 0)
            {
                foreach (DNode dt in env)
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
                return D_IDEForm.icons;
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

        public static DNode GetRoot(DNode dt)
        {
            if (dt == null) return null;
            if (dt.fieldtype == FieldType.Root) return dt;
            return GetRoot((DNode)dt.Parent);
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

        public static DNode GetExprByName(DNode env, CodeLocation where, string name)
        {
            if (env == null) return null;
            if (env.name == name) return env;

            if (where >= env.startLoc && where <= env.endLoc)
            {
                if (env.Count > 0)
                {
                    foreach (DNode dt in env)
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
        public static DNode GetExprByName(DNode env, string name)
        {
            return GetExprByName(env, name, false);
        }
        static bool CheckForParamNameEquation(DNode env, string name, ref DNode match)
        {
            if (env.TemplateParameters.Count > 0)
                foreach (DNode dt in env.TemplateParameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                    {
                        match = dt; return true;
                    }
                }
            if (env is DMethod)
                foreach (DNode dt in (env as DMethod).Parameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                    {
                        match = dt; return true;
                    }
                }
            return false;
        }
        public static DNode GetExprByName(DNode env, string name, bool RootOnly)
        {
            if (env == null) return null;

            if (env.name == name) { return env; }

            DNode possibleReturn = null;
            if (CheckForParamNameEquation(env, name, ref possibleReturn)) return possibleReturn;

            if (env.Count > 0)
                foreach (DNode dt in env)
                {
                    if (dt == env) continue;
                    dt.Parent = env;
                    if (dt.name == name)
                        return dt;

                    if (!RootOnly)
                    {
                        DNode tdt = GetExprByName(dt, name, false);
                        if (tdt != null) return tdt;
                    }
                }
            return null;
        }
        public static DNode GetExprByName(DNode env, DNode levelnode, string name, bool RootOnly)
        {
            if (env == null) return null;

            if (env.name == name) { return env; }

            DNode possibleReturn = null;
            if (CheckForParamNameEquation(env, name, ref possibleReturn)) return possibleReturn;

            if (env.Count > 0)
                foreach (DNode dt in env)
                {
                    if (dt == env) continue;
                    dt.Parent = env;
                    if (dt.name == name)
                        return dt;

                    if (!RootOnly)
                    {
                        DNode tdt = GetExprByName(dt, levelnode, name, dt == levelnode);
                        if (tdt != null) return tdt;
                    }
                }
            return null;
        }
        public static List<DNode> GetExprsByName(DNode env, string name, bool RootOnly)
        {
            List<DNode> ret = new List<DNode>();
            if (env == null) return ret;

            //if(env.name == name) {return ret; }

            if (env.TemplateParameters.Count > 0)
                foreach (DNode dt in env.TemplateParameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        ret.Add(dt);
                }
            if (env is DMethod)
                foreach (DNode dt in (env as DMethod).Parameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        ret.Add(dt);
                }

            if (env.Count > 0)
                foreach (DNode dt in env)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        ret.Add(dt);

                    if (!RootOnly) ret.AddRange(GetExprsByName(dt, name, RootOnly));
                }
            return ret;
        }

        public static DNode SearchExprInClassHierarchy(CompilerConfiguration cc, DNode env, DNode currentLevelNode, string name)
        {
            if (env == null) return null;

            if (env.name == name) { return env; }

            DNode possibleReturn = null;
            if (CheckForParamNameEquation(env, name, ref possibleReturn)) return possibleReturn;

            if (env.Count > 0)
                foreach (DNode dt in env)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        return dt;

                    DNode tdt = GetExprByName(dt, currentLevelNode, name, dt == currentLevelNode);
                    if (tdt != null) return tdt;
                }

            if (env is DClassLike && (env as DClassLike).BaseClass != null)
            {
                string super = (env as DClassLike).BaseClass.ToString();// Should be superior class
                /*if(!DTokens.ClassLike[env.TypeToken] && !exact)
                {
                    super = env.type;
                }*/
                DNode dt = SearchGlobalExpr(cc, null, super);
                if (dt == null) return null;
                return SearchExprInClassHierarchy(cc, dt, currentLevelNode, name);
            }

            return null;
        }
        public static List<DNode> SearchExprsInClassHierarchy(CompilerConfiguration cc, DNode env, string name)
        {
            List<DNode> ret = new List<DNode>();
            if (env == null) return ret;
            if (env.name == name) { ret.Add(env); return ret; }

            if (env.TemplateParameters.Count > 0)
                foreach (DNode dt in env.TemplateParameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        ret.Add(dt);
                }
            if (env is DMethod)
                foreach (DNode dt in (env as DMethod).Parameters)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                        ret.Add(dt);
                }

            if (env.Count > 0)
                foreach (DNode dt in env)
                {
                    dt.Parent = env;
                    if (dt.name == name)
                    {
                        if (dt.fieldtype != FieldType.AliasDecl)
                            ret.Add(dt);
                        else
                        {
                            if (dt.Type != null)
                                ret.AddRange(SearchGlobalExprs(null, null, dt.Type.ToString()));
                            continue;
                        }
                    }
                    ret.AddRange(GetExprsByName(dt, name, false));
                }

            if (env is DClassLike)
            {
                string super = (env as DClassLike).BaseClass.ToString();// Should be superior class
                if (super != "")
                {
                    DNode dt = SearchGlobalExpr(cc, null, super);
                    if (dt == null) return null;
                    ret.AddRange(SearchExprsInClassHierarchy(cc, dt, name));
                }
            }

            return ret;
        }
        public static DNode SearchExprInClassHierarchyBackward(CompilerConfiguration cc, DNode node, string name)
        {
            if (node == null) return null;

            if (node.name == name) { return node; }

            DNode dt = GetExprByName(node, name, true);
            if (dt != null) return dt;

            if (node is DClassLike)
            {
                string super = (node as DClassLike).BaseClass.ToString();// Should be superior class
                /*if(!DTokens.ClassLike[env.TypeToken] && !exact)
                {
                    super = env.type;
                }*/
                if (super != "")
                {
                    dt = SearchGlobalExpr(cc, null, super);
                    if (dt != null)
                    {
                        return SearchExprInClassHierarchyBackward(cc, dt, name);
                    }
                }
            }

            return SearchExprInClassHierarchyBackward(cc, (DNode)node.Parent, name);
        }

        public static DNode SearchGlobalExpr(CompilerConfiguration cc, DNode local, string expr)
        {
            return SearchGlobalExpr(cc, local, expr, false);
        }
        public static DNode SearchGlobalExpr(CompilerConfiguration cc, DNode local, string expr, bool RootOnly)
        {
            DNode ret = null;

            if (local != null) ret = GetExprByName(local, expr, RootOnly);
            if (ret != null) return ret;

            foreach (DModule gpf in cc.GlobalModules)
            {
                ret = GetExprByName(gpf.dom, expr, RootOnly);
                if (ret != null) return ret;
            }
            return null;
        }
        public static DNode SearchGlobalExpr(DProject prj, DModule local, string expr, bool RootOnly, out DModule module)
        {
            CompilerConfiguration cc = prj != null ? prj.Compiler : D_IDE_Properties.Default.DefaultCompiler;
            module = null;
            DNode ret = null;

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

            foreach (DModule gpf in cc.GlobalModules)
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
        public static List<DNode> SearchGlobalExprs(DProject prj, DNode local, string expr)
        {
            return SearchGlobalExprs(prj, local, expr, false);
        }
        ///<see cref="SearchGlobalExprs"/>
        public static List<DNode> SearchGlobalExprs(DProject prj, DNode local, string expr, bool RootOnly)
        {
            CompilerConfiguration cc = prj != null ? prj.Compiler : D_IDE_Properties.Default.DefaultCompiler;
            List<DNode> ret = new List<DNode>();

            if (local != null) ret.AddRange(GetExprsByName(local, expr, RootOnly));

            if (prj != null)
                foreach (DModule ppf in prj.files)
                {
                    ret.AddRange(GetExprsByName(ppf.dom, expr, RootOnly));
                }

            foreach (DModule gpf in cc.GlobalModules)
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
        public static void AddAllClassMembers(CompilerConfiguration cc, DNode selectedExpression, ref List<ICompletionData> rl, bool all)
        {
            ImageList icons = D_IDEForm.icons;
            if (selectedExpression != null)
            {
                foreach (DNode ch in selectedExpression)
                {
                    ch.Parent = selectedExpression;
                    if ((!ch.modifiers.Contains(DTokens.Private) || all) && ch.fieldtype != FieldType.Constructor) // Exlude ctors because they aren't needed
                        rl.Add(new DCompletionData(ch, selectedExpression));
                }

                foreach (DNode arg in selectedExpression.TemplateParameters)
                {
                    rl.Add(new DCompletionData(arg, selectedExpression, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
                }
                if (selectedExpression is DMethod)
                    foreach (DNode arg in (selectedExpression as DMethod).Parameters)
                    {
                        rl.Add(new DCompletionData(arg, selectedExpression, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
                    }

                if (selectedExpression is DClassLike && (selectedExpression as DClassLike).BaseClass != null)
                {
                    // if not, add items of all superior classes or interfaces
                    DNode dt = SearchGlobalExpr(cc, null, (selectedExpression as DClassLike).BaseClass.ToString()); // Should be superior class
                    AddAllClassMembers(cc, dt, ref rl, false);
                }
                else
                {
                    //TODO: Find out what gets affected by returning here
                    //return;
                    if (selectedExpression.fieldtype != FieldType.Variable && !DTokens.ClassLike[selectedExpression.TypeToken] && selectedExpression.Parent != null && (selectedExpression.Parent as DNode).fieldtype != FieldType.Root)
                        AddAllClassMembers(cc, (DNode)selectedExpression.Parent, ref rl, all);
                }
            }
        }

        public static void AddAllClassMembers(CompilerConfiguration cc, DNode selectedExpression, ref List<ICompletionData> rl, bool all, bool exact, string searchExpr)
        {
            ImageList icons = D_IDEForm.icons;

            if (selectedExpression != null)
            {
                foreach (DNode ch in selectedExpression)
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
                foreach (DNode arg in selectedExpression.TemplateParameters)
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

                if (selectedExpression is DMethod)
                    foreach (DNode arg in (selectedExpression as DMethod).Parameters)
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

                if (selectedExpression is DClassLike && (selectedExpression as DClassLike).BaseClass != null)
                {
                    DNode dt = SearchGlobalExpr(cc, null, (selectedExpression as DClassLike).BaseClass.ToString()); // Should be superior class
                    if (dt == null) return;
                    AddAllClassMembers(cc, dt, ref rl, all, exact, searchExpr);
                }
            }
        }

        /// <summary>
        /// Called when entry should be inserted. Forward to the insertion action of the completion data.
        /// </summary>
        public bool InsertAction(ICompletionData idata, TextArea ta, int off, char key)
        {
            if (idata == null) return false;
            if (idata.Text==null || idata.Text.Trim()=="") { presel = null; return false; }
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
            if (t > 0)
            {
                ret = ret.Remove(0, t + 1);
                t = ret.IndexOf(')');
                if (t > 0)
                    ret = ret.Remove(t, 1);
            }
            return ret;
        }
    }
}
