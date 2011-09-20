using System.Collections.Generic;

namespace D_Parser.Dom
{
	public class ImportStatement
	{
		public bool IsStatic;
		public bool IsPublic;

		/// <summary>
		/// import io=std.stdio;
		/// </summary>
		public string ModuleAlias;
		public string ModuleIdentifier;

		/// <summary>
		/// import std.stdio:writeln,foo=writeln;
		/// 
		/// Key:	symbol, alias identifier
		/// Value:	empty,	aliased symbol
		/// </summary>
		public Dictionary<string, string> ExclusivelyImportedSymbols=null;

		public bool IsSimpleBinding
		{
			get
			{
				return string.IsNullOrEmpty(ModuleAlias) && (ExclusivelyImportedSymbols==null || ExclusivelyImportedSymbols.Count<1);
			}
		}

		public override string ToString()
		{
			var ret= (IsPublic?"public ":"")+(IsStatic?"static ":"")+"import ";

			if (!string.IsNullOrEmpty(ModuleAlias))
				ret += ModuleAlias+'=';

			ret += ModuleIdentifier;

			if (ExclusivelyImportedSymbols != null && ExclusivelyImportedSymbols.Count > 0)
			{
				ret += ':';

				foreach (var kv in ExclusivelyImportedSymbols)
				{
					ret += kv.Key;

					if (!string.IsNullOrEmpty(kv.Value))
						ret += '='+kv.Value;

					ret += ',';
				}

				ret = ret.TrimEnd(',');
			}

			return ret;
		}
	}
}
