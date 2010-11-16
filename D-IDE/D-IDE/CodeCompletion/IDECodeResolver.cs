using System;
using System.Collections.Generic;
using System.Text;
using D_Parser;
using System.IO;

namespace D_IDE.CodeCompletion
{
    /// <summary>
    /// Little additions to the DCodeResolver class / Compatibility issue fixes
    /// </summary>
    public class D_IDECodeResolver:DCodeResolver
    {
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
