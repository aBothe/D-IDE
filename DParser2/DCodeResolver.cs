using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    /// <summary>
    /// Generic class for resolve module relations and/or declarations
    /// </summary>
    public class DCodeResolver
    {
        #region Direct Code search
        public static TypeDeclaration BuildIdentifierList(string Text, int CaretOffset, bool BackwardOnly)
        {
            if (String.IsNullOrEmpty(Text) || CaretOffset >= Text.Length) return null;
            // At first we only want to find the beginning of our identifier list
            // later we will pass the text beyond the beginning to the parser - there we parse all needed expressions from it
            int IdentListStart = -1;

            /*
            T!(...)>.<
             */

            int isComment = 0;
            bool isString = false, expectDot = false, hadDot = true;
            var bracketStack = new Stack<char>();
            bool stopSeeking = false;

            for (int i = CaretOffset; i >= 0 && !stopSeeking; i--)
            {
                IdentListStart = i;
                var c = Text[i];
                var str = Text.Substring(i);
                char p = ' ';
                if (i > 0) p = Text[i - 1];

                // Primitive comment check
                if (!isString && c == '/' && (p == '*' || p == '+'))
                    isComment++;
                if (!isString && isComment > 0 && (c == '+' || c == '*') && p == '/')
                    isComment--;

                // Primitive string check
                //TODO: "blah">.<
                if (isComment < 1 && c == '"' && p != '\\')
                    isString = !isString;

                // If string or comment, just continue
                if (isString || isComment > 0)
                    continue;

                // If between brackets, skip
                if (bracketStack.Count > 0 && c != bracketStack.Peek())
                    continue;

                // Bracket check
                switch (c)
                {
                    case ']':
                        bracketStack.Push('[');
                        continue;
                    case ')':
                        bracketStack.Push('(');
                        continue;
                    case '}':
                        bracketStack.Push('{');
                        continue;

                    case '[':
                    case '(':
                    case '{':
                        if (bracketStack.Count > 0 && bracketStack.Peek() == c)
                        {
                            bracketStack.Pop();
                            if (p == '!') // Skip template stuff
                                i--;
                        }
                        else
                        {
                            // Stop if we reached the most left existing bracket
                            // e.g. foo>(< bar| )
                            stopSeeking = true;
                            IdentListStart++;
                        }
                        continue;
                }

                // whitespace check
                if (Char.IsWhiteSpace(c)) { if (hadDot) expectDot = false; else expectDot = true; continue; }

                if (c == '.')
                {
                    expectDot = false;
                    hadDot = true;
                    continue;
                }

                /*
                 * abc
                 * abc . abc
                 * T!().abc[]
                 * def abc.T
                 */
                if (Char.IsLetterOrDigit(c) || c == '_')
                {
                    hadDot = false;

                    if (!expectDot)
                        continue;
                    else
                        IdentListStart++;
                }

                stopSeeking = true;
            }


            // Part 2: Init the parser
            if (!stopSeeking || IdentListStart < 0)
                return null;

            // If code e.g. starts with a bracket, increment IdentListStart
            var ch = Text[IdentListStart];
            if (!Char.IsLetterOrDigit(ch) && ch != '_' && ch != '.')
                IdentListStart++;

            var psr = DParser.ParseBasicType(BackwardOnly ? Text.Substring(IdentListStart, CaretOffset - IdentListStart) : Text.Substring(IdentListStart));
            return psr;
        }

        public static DBlockStatement SearchBlockAt(DBlockStatement Parent, CodeLocation Where)
        {
            foreach (var n in Parent)
            {
                if (!(n is DBlockStatement)) continue;

                var b = n as DBlockStatement;
                if (Where > b.BlockStartLocation && Where < b.EndLocation)
                    return SearchBlockAt(b, Where);
            }

            return Parent;
        }
        #endregion

        #region Import path resolving
        /// <summary>
        /// Returns all imports of a module and those public ones of the imported modules
        /// </summary>
        /// <param name="cc"></param>
        /// <param name="ActualModule"></param>
        /// <returns></returns>
        public static List<DModule> ResolveImports(List<DModule> CodeCache, DModule ActualModule)
        {
            var ret = new List<DModule>();
            if (CodeCache == null || ActualModule == null) return ret;

            // First add all local imports
            var localImps = new List<string>();
            foreach (var kv in ActualModule.Imports)
                localImps.Add(kv.Key);

            foreach (var m in CodeCache)
                if (localImps.Contains(m.ModuleName) && !ret.Contains(m))
                {
                    ret.Add(m);
                    /* 
                     * dmd-feature: public imports only affect the directly superior module
                     *
                     * Module A:
                     * import B;
                     * 
                     * foo(); // Will fail, because foo wasn't found
                     * 
                     * Module B:
                     * import C;
                     * 
                     * Module C:
                     * public import D;
                     * 
                     * Module D:
                     * void foo() {}
                     * 
                     * 
                     * Whereas
                     * Module B:
                     * public import C;
                     * 
                     * will succeed because we have a closed import hierarchy in which all imports are public.
                     * 
                     */

                    // So if there aren't any public imports in our import, continue without doing anything
                    ResolveImports(ref ret, CodeCache, m);
                }

            return ret;
        }

        public static void ResolveImports(ref List<DModule> ImportModules, List<DModule> CodeCache, DModule ActualModule)
        {
            var localImps = new List<string>();
            foreach (var kv in ActualModule.Imports)
                if (kv.Value)
                    localImps.Add(kv.Key);

            foreach (var m in CodeCache)
                if (localImps.Contains(m.ModuleName) && !ImportModules.Contains(m))
                {
                    ImportModules.Add(m);
                    ResolveImports(ref ImportModules, CodeCache, m);
                }
        }
        #endregion

        #region Declaration resolving

        public static DModule SearchModuleInCache(List<DModule> HayStack,string ModuleName)
        {
            foreach (var m in HayStack)
            {
                if (m.ModuleName == ModuleName) return m;
            }
            return null;
        }

        /// <summary>
        /// Finds the location (module node) where a type (TypeExpression) has been declared.
        /// </summary>
        /// <param name="Module"></param>
        /// <param name="IdentifierList"></param>
        /// <returns>When a type was found, the declaration entry will be returned. Otherwise, it'll return null.</returns>
        public static DNode ResolveTypeDeclaration(DBlockStatement BlockNode, TypeDeclaration IdentifierList)
        {
            if (BlockNode == null || IdentifierList == null) return null;

            // Modules    Declaration
            // |---------|-----|
            // std.stdio.writeln();
            if (IdentifierList is IdentifierList)
            {
                var il = IdentifierList as IdentifierList;
                var skippedIds = 0;

                // Now search the entire block
                var istr = il.ToString();

                var mod = BlockNode.NodeRoot as DModule;
                /* If the id list start with the name of BlockNode's root module, 
                 * skip those first identifiers to proceed seeking the rest of the list
                 */
                if (mod != null && istr.StartsWith(mod.ModuleName))
                {
                    skippedIds += mod.ModuleName.Split('.').Length;
                    istr = il.ToString(skippedIds);
                }

                // Now move stepwise deeper calling ResolveTypeDeclaration recursively
                DNode currentNode = BlockNode;
                while (skippedIds < il.Parts.Count && currentNode is DBlockStatement)
                {
                    // As long as our node can contain other nodes, scan it
                    currentNode = ResolveTypeDeclaration(currentNode as DBlockStatement, il[skippedIds]);
                    skippedIds++;
                }
                return currentNode;
            }

            // Scan the type declaration list for any NormalDeclarations
            var td = IdentifierList;
            while (td != null && !(td is NormalDeclaration))
                td = td.Base;

            if (td is NormalDeclaration)
            {
                var nameIdent = td as NormalDeclaration;

                // Scan from the inner to the outer level
                var currentParent = BlockNode;
                while (currentParent != null)
                {
                    foreach (var ch in currentParent)
                    {
                        if (nameIdent.Name == ch.Name)
                            return ch;
                    }
                    currentParent = currentParent.Parent as DBlockStatement;
                }
            }

            return null;
        }

        public static DNode ResolveTypeDeclaration(List<DModule> ModuleCache, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var ThisModule = CurrentlyScopedBlock.NodeRoot as DModule;
            var LookupModules = ResolveImports(ModuleCache, ThisModule);

            // Of course it's needed to scan our own module at first
            if (ThisModule != null)
            {
                var typeNode = ResolveTypeDeclaration(CurrentlyScopedBlock, IdentifierList);
                if (typeNode != null)
                    return typeNode;
            }

            // Important: Implicitly add the object module
            var objmod = SearchModuleInCache(ModuleCache, "object");
            if (!LookupModules.Contains(objmod))
                LookupModules.Add(objmod);

            // Then search within the imports for our IdentifierList
            foreach (var m in LookupModules)
            {
                var typeNode = ResolveTypeDeclaration(m, IdentifierList);
                // If we found a match, return the first we get
                if (typeNode != null)
                    return typeNode;
            }

            return null;
        }

        public static DNode ResolveTypeDeclaration(List<DModule> GlobalModules, List<DModule> LocalModules, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var SearchArea = new List<DModule>(LocalModules);
            SearchArea.AddRange(GlobalModules);

            return ResolveTypeDeclaration(SearchArea, CurrentlyScopedBlock, IdentifierList);
        }
        #endregion
    }
}
