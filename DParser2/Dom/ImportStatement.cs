using System.Collections.Generic;
using D_Parser.Dom.Statements;
using D_Parser.Parser;

namespace D_Parser.Dom
{
	public class ModuleStatement:AbstractStatement
	{
		public ITypeDeclaration ModuleName;

		public override string ToCode()
		{
			return "module "+ModuleName==null?"": ModuleName.ToString();
		}
	}

	public class ImportStatement:AbstractStatement
	{
		public bool IsStatic
		{
			get { return DAttribute.ContainsAttribute(Attributes, DTokens.Static); }
		}

		public bool IsPublic
		{
			get { return DAttribute.ContainsAttribute(Attributes, DTokens.Public); }
		}

		public class Import
		{
			/// <summary>
			/// import io=std.stdio;
			/// </summary>
			public string ModuleAlias;
			public ITypeDeclaration ModuleIdentifier;

			public override string ToString()
			{
				var r= string.IsNullOrEmpty(ModuleAlias) ? "":(ModuleAlias+" = ");

				if (ModuleIdentifier != null)
					r += ModuleIdentifier.ToString();

				return r;
			}
		}

		public class ImportBindings
		{
			public Import Module;

			/// <summary>
			/// Keys: symbol alias
			/// Values: symbol
			/// 
			/// If value empty: Key is imported symbol
			/// </summary>
			public List<KeyValuePair<string, string>> SelectedSymbols = new List<KeyValuePair<string, string>>();

			public override string ToString()
			{
				var r = Module==null?"":Module.ToString();

				r += " : ";

				if(SelectedSymbols!=null)
					foreach (var kv in SelectedSymbols)
					{
						r += kv.Key;

						if (!string.IsNullOrEmpty(kv.Value))
							r += "="+kv.Value;

						r += ",";
					}

				return r.TrimEnd(',');
			}
		}

		public List<Import> Imports = new List<Import>();
		public ImportBindings ImportBinding;

		public override string ToCode()
		{
			var ret = AttributeString + "import ";

			foreach (var imp in Imports)
			{
				ret += imp.ToString()+",";
			}

			if (ImportBinding != null)
				ret = ret.TrimEnd(',');
			else
				ret += ImportBinding.ToString();

			return ret;
		}

		#region Pseudo alias variable generation
		public List<DVariable> PseudoAliases = new List<DVariable>();

		internal void CreatePseudoAliases()
		{
			PseudoAliases.Clear();

			foreach (var imp in Imports)
				if (!string.IsNullOrEmpty(imp.ModuleAlias))
					PseudoAliases.Add(new ImportSymbolAlias(imp));

			if (ImportBinding != null)
			{
				foreach (var bind in ImportBinding.SelectedSymbols)
				{
					PseudoAliases.Add();
				}
			}
		}

		#endregion
	}

	public class ImportSymbolAlias : DVariable
	{
		public ImportSymbolAlias(ImportStatement.Import imp)
		{
			Name = imp.ModuleAlias;
			Type = imp.ModuleIdentifier;
			IsAlias = true;
		}

		public ImportSymbolAlias()
		{

		}
	}
}
