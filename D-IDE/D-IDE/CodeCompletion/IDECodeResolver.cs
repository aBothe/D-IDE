using System;
using System.Collections.Generic;
using System.Text;
using D_Parser;
using System.IO;
using ICSharpCode.TextEditor;
using D_IDE.Misc;

namespace D_IDE.CodeCompletion
{
    /// <summary>
    /// Little additions to the DCodeResolver class / Compatibility issue fixes
    /// </summary>
    public class D_IDECodeResolver:DCodeResolver
    {
        public static DNode[] ResolveTypeDeclarations(CodeModule Module, TextArea ta, TextLocation CursorLocation)
        {
            DToken t = null;
            TypeDeclaration ids = null;
            return ResolveTypeDeclarations(Module, ta, CursorLocation, out t,out ids);
        }

        public static DNode[] ResolveTypeDeclarations(CodeModule Module, TextArea ta, TextLocation CursorLocation, out TypeDeclaration Identifiers)
        {
            DToken t = null;
            return ResolveTypeDeclarations(Module, ta, CursorLocation, out t, out Identifiers);
        }

        public static DNode[] ResolveTypeDeclarations(CodeModule Module, TextArea ta, TextLocation CursorLocation, out DToken ExtraOrdinaryToken)
        {
            TypeDeclaration ids = null;
            return ResolveTypeDeclarations(Module, ta, CursorLocation, out ExtraOrdinaryToken, out ids);
        }

        public static DNode[] ResolveTypeDeclarations(CodeModule Module, TextArea ta,TextLocation CursorLocation, out DToken ExtraOrdinaryToken, out TypeDeclaration Identifiers)
        {
            ExtraOrdinaryToken = null;
            Identifiers = null;

            int mouseOffset = ta.TextView.Document.PositionToOffset(CursorLocation);

            // To save time, look if we are within a comment or a string literal first - if so, return
            if (mouseOffset < 1 || Commenting.IsInCommentAreaOrString(ta.TextView.Document.TextContent, mouseOffset)) 
                return null;

            // Our finally resolved node
            DNode DeclarationBlock = SearchBlockAt(Module, Util.ToCodeLocation(CursorLocation));

            // Retrieve the identifierlist that's located beneath the cursor
            Identifiers = BuildIdentifierList(ta.TextView.Document.TextContent, mouseOffset, false, out ExtraOrdinaryToken);

            /*
             * 1) Normally we don't have any extra tokens here, e.g. Object1.ObjProp1.MyProp.
             * 2) Otherwise we check if there's a 'this' or 'super' at the very beginning of our ident list - then retrieve the fitting (base-)class and go on searching within these.
             * 3) On totally different tokens (like hovering a 'for' or '__FILE__') we react just by showing a description or some example code.
             */

            // Handle cases 2 and 3
            if (ExtraOrdinaryToken != null)
            {
                // Handle case 2
                if (ExtraOrdinaryToken.Kind == DTokens.This || ExtraOrdinaryToken.Kind == DTokens.Super)
                {
                    var ClassDef = SearchClassLikeAt(Module, Util.ToCodeLocation(CursorLocation)) as DClassLike;

                    // If 'this'
                    DeclarationBlock = ClassDef;
                    if (DeclarationBlock == null) return null;

                    // If we have a 'super' token, look for ClassDef's superior classes
                    if (ClassDef != null && ExtraOrdinaryToken.Kind == DTokens.Super)
                    {
                        DeclarationBlock = Module.Project != null ?
                            ResolveBaseClass(Module.Project.Compiler.GlobalModules, Module.Project.Modules, ClassDef) :
                            ResolveBaseClass(Module.Project.Compiler.GlobalModules, ClassDef);
                    }

                    // If '(' follows, return ctors
                    if (ExtraOrdinaryToken.Next != null && ExtraOrdinaryToken.Next.Kind == DTokens.OpenParenthesis)
                    {
                        var rl = new List<DNode>();

                        foreach (var n in DeclarationBlock as DBlockStatement)
                            if (n is DMethod && (n as DMethod).SpecialType == DMethod.MethodType.Constructor)
                                rl.Add(n);
                            
                        return rl.ToArray();
                    }

                    // If there are any other identifiers, return our looked-up block
                    if (Identifiers== null)
                        return new DNode[] { DeclarationBlock};
                }
                else // Other tokens
                    return null;
            }

            /*
             * Note: We also need to follow class heritages and module paths!
             */
            if (Identifiers != null)
            {
                var istr = Identifiers.ToString();

                // Look for module paths and if they fit to our identifier path or not
                var rl = new List<DNode>();
                foreach (var m in Module.Project != null ? Module.Project.Compiler.GlobalModules : D_IDE_Properties.Default.DefaultCompiler.GlobalModules)
                {
                    // If our module name totally equals our id string, go on with returning all its children and not the module itself!
                    if (m.ModuleName.StartsWith(istr))
                        rl.Add(m);
                    else if (istr.StartsWith(m.ModuleName))
                        return ResolveTypeDeclarations_ModuleOnly(new List<DModule>(),m,Identifiers,NodeFilter.PublicOnly);
                }
                if (rl.Count > 0)
                    return rl.ToArray();


                // Get imported modules first
                var Imports = Module.Project != null ?
                    ResolveImports(Module.Project.Compiler.GlobalModules, Module.Project.Modules, Module) :
                    ResolveImports(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, Module);

                return ResolveTypeDeclarations(Imports, DeclarationBlock as DBlockStatement, Identifiers);
            }
            return null;
        }





        public static List<DModule> ResolveImports(List<CodeModule> GlobalModules, List<CodeModule> LocalModules, DModule CurrentModule)
        {
            var SearchArea = new List<DModule>(GlobalModules.Count + LocalModules.Count);

            foreach (var m in GlobalModules)
                SearchArea.Add(m);
            foreach (var m in LocalModules)
                SearchArea.Add(m);

            return ResolveImports(SearchArea, CurrentModule);
        }

        public static List<DModule> ResolveImports(List<CodeModule> ModuleCache, DModule CurrentModule)
        {
            var SearchArea = new List<DModule>(ModuleCache.Count);

            foreach (var m in ModuleCache)
                SearchArea.Add(m);

            return ResolveImports(SearchArea, CurrentModule);
        }

        public static DNode[] ResolveTypeDeclarations(List<CodeModule> GlobalModules, List<CodeModule> LocalModules, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var SearchArea = new List<DModule>(GlobalModules.Count+LocalModules.Count);

            foreach (var m in GlobalModules)
                SearchArea.Add(m);
            foreach (var m in LocalModules)
                SearchArea.Add(m);

            return ResolveTypeDeclarations(SearchArea, CurrentlyScopedBlock, IdentifierList);
        }

        public static DNode[] ResolveTypeDeclarations(List<CodeModule> ModuleCache, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var SearchArea = new List<DModule>(ModuleCache.Count);

            foreach (var m in ModuleCache)
                SearchArea.Add(m);

            return ResolveTypeDeclarations(SearchArea, CurrentlyScopedBlock, IdentifierList);
        }

        public static DClassLike ResolveBaseClass(List<CodeModule> ModuleCache, DClassLike ActualClass)
        {
            var SearchArea = new List<DModule>(ModuleCache.Count);

            foreach (var m in ModuleCache)
                SearchArea.Add(m);

            return ResolveBaseClass(SearchArea, ActualClass);
        }

        public static DClassLike ResolveBaseClass(List<CodeModule> GlobalModules, List<CodeModule> LocalModules,DClassLike ActualClass)
        {
            var SearchArea = new List<DModule>(GlobalModules.Count + LocalModules.Count);

            foreach (var m in GlobalModules)
                SearchArea.Add(m);
            foreach (var m in LocalModules)
                SearchArea.Add(m);

            return ResolveBaseClass(SearchArea, ActualClass);
        }
    }
}
