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
            return ResolveTypeDeclarations(Module, ta, CursorLocation, out t);
        }

        public static DNode[] ResolveTypeDeclarations(CodeModule Module, TextArea ta,TextLocation CursorLocation, out DToken ExtraOrdinaryToken)
        {
            // Prerequisites
            ExtraOrdinaryToken = null;
            int mouseOffset = ta.TextView.Document.PositionToOffset(CursorLocation);

            // To save time, look if we are within a comment or a string literal first - if so, return
            if (mouseOffset < 1 || DCodeResolver.Commenting.IsInCommentAreaOrString(ta.TextView.Document.TextContent, mouseOffset)) 
                return null;

            // Our finally resolved node
            DNode DeclarationBlock = DCodeResolver.SearchBlockAt(Module, Util.ToCodeLocation(CursorLocation));

            // Retrieve the identifierlist that's located beneath the cursor
            var expr = DCodeResolver.BuildIdentifierList(ta.TextView.Document.TextContent, mouseOffset, false, out ExtraOrdinaryToken);

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
                    var ClassDef = D_IDECodeResolver.SearchClassLikeAt(Module, Util.ToCodeLocation(CursorLocation)) as DClassLike;

                    // If 'this'
                    DeclarationBlock = ClassDef;

                    // If we have a 'super' token, look for ClassDef's superior classes
                    if (ClassDef != null && ExtraOrdinaryToken.Kind == DTokens.Super)
                    {
                        DeclarationBlock = Module.Project != null ?
                            D_IDECodeResolver.ResolveBaseClass(Module.Project.Compiler.GlobalModules, Module.Project.Modules, ClassDef) :
                            D_IDECodeResolver.ResolveBaseClass(Module.Project.Compiler.GlobalModules, ClassDef);
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
                    if (expr == null)
                        return new DNode[] { DeclarationBlock};
                }
                else // Other tokens
                    return null;
            }

            /*
             * Notes:
             * We also need to follow class heritages
             */
            if (expr != null)
            {
                // Get imported modules first
                var Imports = Module.Project != null ?
                    D_IDECodeResolver.ResolveImports(Module.Project.Compiler.GlobalModules, Module.Project.Modules, Module) :
                    D_IDECodeResolver.ResolveImports(D_IDE_Properties.Default.DefaultCompiler.GlobalModules, Module);

                return DCodeResolver.ResolveTypeDeclarations(Imports, DeclarationBlock as DBlockStatement, expr);
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
