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
using D_IDE.CodeCompletion;
using D_IDE.Misc;

namespace D_IDE
{
	public partial class DCodeCompletionProvider : ICompletionDataProvider
    {
        #region Properties
        public CompilerConfiguration cc=D_IDE_Properties.Default.DefaultCompiler;
        public DocumentInstanceWindow DocWindow = null;

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

        public DCodeCompletionProvider(DocumentInstanceWindow Document)
        {
            DocWindow = Document;
        }

        public static ICompletionData[] GenerateAfterSpaceClompletionData(DocumentInstanceWindow DocWindow)
        {
            var rl = new List<ICompletionData>();

            // Module name stubs
            if (DocWindow.Module.Project != null)
            {
                AddModulePackages(DocWindow.Module.Project.Compiler.GlobalModules, ref rl);
                AddModulePackages(DocWindow.Module.Project.Modules, ref rl);
            }
            else
                AddModulePackages(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, ref rl);


            // Resolve imports
            var modules = DocWindow.Module.Project != null ?
                D_IDECodeResolver.ResolveImports(DocWindow.Module.Project.Compiler.GlobalModules, DocWindow.Module.Project.Modules, DocWindow.Module) :
                D_IDECodeResolver.ResolveImports(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, DocWindow.Module);

            // Root nodes of imported modules
            foreach (var m in modules)
            {
                foreach (var n in m)
                {
                    //TODO: Allow special node types
                    if (String.IsNullOrEmpty(n.Name)) continue;
                    //if(n.ContainsAttribute(DTokens.Public))
                    Add(ref rl,n);
                }
            }


            // Get currently scoped block
            var curBlock = D_IDECodeResolver.SearchBlockAt(DocWindow.Module, Util.ToCodeLocation(DocWindow.Caret));

            // Go rootward
            while (curBlock != null)
            {
                // Normal children
                foreach (var n in curBlock)
                    Add(ref rl,n);

                // Template arguments
                if (curBlock.TemplateParameters != null)
                    foreach (var n in curBlock.TemplateParameters)
                        Add(ref rl,n);

                // Parameters
                if (curBlock is DMethod)
                    foreach (var n in (curBlock as DMethod).Parameters)
                        Add(ref rl,n);

                // Members of base classes
                if (curBlock is DClassLike)
                {
                    DClassLike baseClass = curBlock as DClassLike;
                    while (true)
                    {
                        baseClass =
                            DocWindow.Module.Project != null ?
                            D_IDECodeResolver.ResolveBaseClass(DocWindow.Module.Project.Compiler.GlobalModules, DocWindow.Module.Project.Modules, baseClass) :
                            D_IDECodeResolver.ResolveBaseClass(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, baseClass);

                        if (baseClass == null) break;

                        foreach (var n in baseClass)
                            Add(ref rl, n);
                    }
                }

                curBlock = curBlock.Parent as DBlockStatement;
            }

            return rl.ToArray();
        }

		public ICompletionData[] GenerateCompletionData(string fn, TextArea ta, char ch)
		{
			var icons = D_IDEForm.icons;
			var rl = new List<ICompletionData>();
			
            /*
             * There are 2 cases:
             * 1) Dotless call
             * 2) Called by pressing '.'
             * 
             * In case 1, return 
             *  - Module name stubs (folder wise / e.g. std.)
             *  - Root nodes of imported modules
             *  - (hierarchically ascending) Nodes of DocWindow's Module
             *  
             * In case 2, retrieve already typed identifiers (if there aren't any, behave like in case 1!)
             *  - Search in local blocks,
             *  - Global blocks
             *  - Perhaps the already found identifiers represent a module path (!)
             */

            if (ch == '.')
            {
                presel = null;
                DToken tk = null;

                var cursor = DocWindow.Caret;
                cursor.X--;
                var matches = D_IDECodeResolver.ResolveTypeDeclarations(DocWindow.Module, ta, cursor,out tk);

                // return all its children
                // Note: also include the base classes' children
                if(matches!=null)
                foreach (var m in matches)
                {
                    if (m is DBlockStatement)
                    {
                        foreach (var n in (m as DBlockStatement))
                            Add(ref rl,n);
                    }

                    if (m is DClassLike)
                    {
                        DClassLike baseClass = m as DClassLike;
                        while (true)
                        {
                            baseClass =
                                DocWindow.Module.Project != null ?
                                D_IDECodeResolver.ResolveBaseClass(DocWindow.Module.Project.Compiler.GlobalModules, DocWindow.Module.Project.Modules, baseClass) :
                                D_IDECodeResolver.ResolveBaseClass(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, baseClass);

                            if (baseClass == null) break;

                            foreach (var n in baseClass)
                                Add(ref rl,n);
                        }
                    }
                }

            }
            else
            {
                var modCode = ta.Document.TextContent;

                string initIdentifier = "";
                
                int i=DocWindow.CaretOffset-1;
                while (i>0)
                {
                    char c = ta.Document.TextContent[i];
                    if (!Char.IsLetter(c) || c!='_') break;

                    initIdentifier = c + initIdentifier;

                    i--;
                }

                if (!String.IsNullOrEmpty(initIdentifier))
                {
                    if (initIdentifier.Length > 1) return null;
                    presel = initIdentifier;
                }
                else { presel = null; return null; }

                return DocWindow.CurrentCompletionData;
            }

			return rl.ToArray();
		}

        static void Add(ref List<ICompletionData> rl, DNode n)
        {
            rl.Add(new DCompletionData(n));
        }

        /// <summary>
        /// Adds module name stubs to completion list
        /// </summary>
        /// <param name="Cache"></param>
        /// <param name="rl"></param>
        public static void AddModulePackages(List<CodeModule> Cache, ref List<ICompletionData> rl)
        {
            var tl = new List<string>();

            int nameSpaceIconKey=D_IDEForm.icons.Images.IndexOfKey("namespace");

            foreach (var m in Cache)
            {
                if (String.IsNullOrEmpty(m.ModuleName)) 
                    continue;

                var firstPackageNamePart = m.ModuleName.Split('.')[0];

                if (!tl.Contains(firstPackageNamePart))
                {
                    tl.Add(firstPackageNamePart);
                    rl.Add(new DCompletionData(firstPackageNamePart,m.ModuleName!=firstPackageNamePart?"":(m.ModuleName+"\n"+m.Description),nameSpaceIconKey));
                }
            }
        }

		public static void AddTypeStd(DNode seldt, ref List<ICompletionData> rl)
		{
			ImageList icons = D_IDEForm.icons;
			rl.Add(new DCompletionData("sizeof", "Yields the memory usage of a type in bytes", icons.Images.IndexOfKey("Icons.16x16.Literal.png")));
			rl.Add(new DCompletionData("stringof", "Returns a string of the typename", icons.Images.IndexOfKey("Icons.16x16.Property.png")));
			rl.Add(new DCompletionData("init", "Returns the default initializer of a type", icons.Images.IndexOfKey("Icons.16x16.Field.png")));
		}


        #region ICompletionProvider implementation
        /// <summary>
        /// Called when entry should be inserted. Forward to the insertion action of the completion data.
        /// </summary>
        public bool InsertAction(ICompletionData idata, TextArea ta, int off, char key)
        {
            if (idata == null) return false;
            if (idata.Text == null || idata.Text.Trim() == "") { presel = null; return false; }
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

        public CompletionDataProviderKeyResult ProcessKey(char key)
        {
            if (char.IsLetterOrDigit(key) || key == '_')
                return CompletionDataProviderKeyResult.NormalKey;
            else
                // key triggers insertion of selected items
                return CompletionDataProviderKeyResult.InsertionKey;
        }
        #endregion
    }
}
