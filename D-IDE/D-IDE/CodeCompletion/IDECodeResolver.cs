using System;
using System.Collections.Generic;
using System.Text;
using D_Parser;
using System.IO;

namespace D_IDE.CodeCompletion
{
    public class D_IDECodeResolver:DCodeResolver
    {
        public static DNode ResolveTypeDeclaration(List<CodeModule> GlobalModules, List<CodeModule> LocalModules, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var SearchArea = new List<DModule>(GlobalModules.Count+LocalModules.Count);

            foreach (var m in GlobalModules)
                SearchArea.Add(m);
            foreach (var m in LocalModules)
                SearchArea.Add(m);

            return ResolveTypeDeclaration(SearchArea, CurrentlyScopedBlock, IdentifierList);
        }

        public static DNode ResolveTypeDeclaration(List<CodeModule> ModuleCache, DBlockStatement CurrentlyScopedBlock, TypeDeclaration IdentifierList)
        {
            var SearchArea = new List<DModule>(ModuleCache.Count);

            foreach (var m in ModuleCache)
                SearchArea.Add(m);

            return ResolveTypeDeclaration(SearchArea, CurrentlyScopedBlock, IdentifierList);
        }
    }
}
