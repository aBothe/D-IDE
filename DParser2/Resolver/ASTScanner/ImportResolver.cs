using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;

namespace D_Parser.Resolver
{
	public class ImportResolver
	{
		//TODO: Selective imports

		/// <summary>
		/// Returns all imports of a module and those public ones of the imported modules
		/// </summary>
		public static IEnumerable<IAbstractSyntaxTree> ResolveImports(DModule ActualModule, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			var ret = new List<IAbstractSyntaxTree>();
			if (CodeCache == null || ActualModule == null) return ret;

			// Try to add the 'object' module
			var objmod = SearchModuleInCache(CodeCache, "object");
			if (objmod != null && !ret.Contains(objmod))
				ret.Add(objmod);

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

			/*
			 * Procedure:
			 * 
			 * 1) Take the imports of the current module
			 * 2) Add the respective modules
			 * 3) If that imported module got public imports, also make that module to the current one and repeat Step 1) recursively
			 * 
			 */

			foreach (var kv in ActualModule.Imports)
				if (kv.IsSimpleBinding && !kv.IsStatic)
				{
					if (kv.ModuleIdentifier == null)
						continue;

					var impMod = SearchModuleInCache(CodeCache, kv.ModuleIdentifier.ToString()) as DModule;

					if (impMod != null && !ret.Contains(impMod))
					{
						ret.Add(impMod);

						ScanForPublicImports(ret, impMod, CodeCache);
					}

				}

			return ret;
		}

		static void ScanForPublicImports(List<IAbstractSyntaxTree> ret, DModule currentlyWatchedImport, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			if (currentlyWatchedImport != null && currentlyWatchedImport.Imports != null)
				foreach (var kv2 in currentlyWatchedImport.Imports)
					if (kv2.IsSimpleBinding && !kv2.IsStatic && kv2.IsPublic)
					{
						if (kv2.ModuleIdentifier == null)
							continue;

						var impMod2 = SearchModuleInCache(CodeCache, kv2.ModuleIdentifier.ToString()) as DModule;

						if (impMod2 != null && !ret.Contains(impMod2))
						{
							ret.Add(impMod2);

							ScanForPublicImports(ret, impMod2, CodeCache);
						}
					}
		}

		static IAbstractSyntaxTree SearchModuleInCache(IEnumerable<IAbstractSyntaxTree> HayStack, string ModuleName)
		{
			foreach (var m in HayStack)
				if (m.Name == ModuleName)
					return m;

			return null;
		}
	}
}
