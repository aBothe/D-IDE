using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using DebugEngineWrapper;
using D_Parser.Core;
using D_Parser;
using System.Runtime.InteropServices;

namespace D_IDE.D
{
	public class DDebugSupport:GenericDebugSupport
	{
		Dictionary<DebugScopedSymbol, DebugSymbolWrapper[]> _childArray=new Dictionary<DebugScopedSymbol,DebugSymbolWrapper[]>();

		public DebugSymbolWrapper[] GetChildren(DebugSymbolGroup locals, DebugSymbolWrapper parent)
		{
			var ret = new List<DDebugSymbolWrapper>();

			var scache=locals.Symbols;

			// If requesting root-leveled items, return all whose depth equals 0
			if (parent == null)
			{
				foreach (var sym in scache)
					if (sym.Depth < 1)
						ret.Add(new DDebugSymbolWrapper(sym));

				return ret.ToArray();
			}

			// Find out index of parent item in locals
			int i = 0;
			for (; i < scache.Length; i++)
				if (scache[i].Offset == parent.Symbol.Offset)
					break;

			// If any items aren't there for searching, return empty list
			if (i >= scache.Length)
				return _childArray[parent.Symbol] = null;

			// Scan if following items are deeper-leveled
			for (int j = i + 1; j < scache.Length; j++)
			{
				var d=scache[j].Depth;
				if (d == parent.Symbol.Depth + 1) // Only add direct child items
				{
					//TODO: Scan for base classes
					ret.Add(new DDebugSymbolWrapper(scache[j]));
				}
				else if(d<=parent.Symbol.Depth) 
					break; // If on the same level again or moved up a level, break
			}

			return  ret.ToArray();
		}

		public override IEnumerable<DebugSymbolWrapper> GetChildSymbols(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Parent)
		{
			//return GetChildren(LocalSymbolCache,Parent);
			if (Parent == null)
				return GetChildren(LocalSymbolCache, Parent);

			if (_childArray.ContainsKey(Parent.Symbol))
				return _childArray[Parent.Symbol];

			return null;
		}

		public override bool HasChildren(DebugSymbolGroup LocalSymbolCache, DebugSymbolWrapper Symbol)
		{
			/* Note: HasChildren gets called first. To save searching time,
			 * write our pre-results into the dictionary to return them when GetChildSymbols() is called.
			 */
			var ret=_childArray[Symbol.Symbol] = GetChildren(LocalSymbolCache,Symbol);
			return ret != null && ret.Length > 0;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
		public struct DObject
		{
			public uint vTbl;
			public uint monitor;
			public string toString() { return ""; }
			public int toHash() { return 0; }
			public int opCmp(DObject o) { return 0; }
			public bool opEquals(DObject o) { return false; }
			public bool opEquals(DObject lhs, DObject rhs) { return false; }
		}

		public class DDebugSymbolWrapper : DebugSymbolWrapper
		{
			IAbstractSyntaxTree module;
			int codeLine;
			INode variableNode;
			INode typeNode;
			//IEnumerable<IAbstractSyntaxTree> moduleCache;

			public DDebugSymbolWrapper(DebugScopedSymbol sym):base(sym)
			{
				try
				{
					/*var ci=CoreManager.DebugManagement.Engine.Symbols.GetPointerTarget(sym.Offset);
					object mo = null;
					IntPtr moPtr = new IntPtr();
					var raw = CoreManager.DebugManagement.Engine.Memory.ReadVirtual(ci, 4);
					Marshal.StructureToPtr(raw, moPtr, false);
					mo = Marshal.PtrToStructure(moPtr, typeof(DObject));*/
				}
				catch { }

				// Search currently scoped module
				string file = "";
				uint line = 0;
				CoreManager.DebugManagement.Engine.Symbols.GetLineByOffset(CoreManager.DebugManagement.Engine.CurrentInstructionOffset,out file,out line);
				codeLine = (int)line;

				if (string.IsNullOrWhiteSpace(file))
					return;

				// If file name found, get syntax tree, as far as possible
				DProject ownerPrj=null;
				module = DLanguageBinding.GetFileSyntaxTree(file,out ownerPrj);



				// If syntax tree built, search the variable location
				if (module != null)
				{
					var block = DCodeResolver.SearchBlockAt(module, new CodeLocation(0, codeLine));

					var res = DCodeResolver.ResolveTypeDeclarations_ModuleOnly(block, new NormalDeclaration(Symbol.Name), DCodeResolver.NodeFilter.All, null);

					if (res.Length > 0)
					{
						variableNode = res[0];
						//moduleCache = DCodeCompletionSupport.Instance.EnumAvailableModules(ownerPrj);
					}
				}



				// Set type string
				_typeString= base.TypeString;
				if (variableNode != null)
				{
					var t = DCodeResolver.GetDNodeType(variableNode);
					if (t != null)
						_typeString= t.ToString();
				}
				


				// Set value string
				_valueString=base.ValueString;

				if(variableNode!=null)
				{
				ITypeDeclaration curValueType=variableNode.Type;
				if (curValueType != null)
				{
					if (!IsBasicType(curValueType))
					{
						if (TypeString == "string") //TODO: Replace this by searching the alias definition in the cache
							curValueType = new ClampDecl(new DTokenDeclaration(DTokens.Char), ClampDecl.ClampType.Square);
						else if (TypeString == "wstring")
							curValueType = new ClampDecl(new DTokenDeclaration(DTokens.Wchar), ClampDecl.ClampType.Square);
						else if (TypeString == "dstring")
							curValueType = new ClampDecl(new DTokenDeclaration(DTokens.Dchar), ClampDecl.ClampType.Square);

						if (IsArray(curValueType))
						{
							var clampDecl = curValueType as ClampDecl;
							var valueType = clampDecl.ValueType;

							if (valueType is DTokenDeclaration)
							{
								bool IsString = false;
								uint elsz = 0;
								var realType = DetermineArrayType((valueType as DTokenDeclaration).Token, out elsz, out IsString);

								var arr = CoreManager.DebugManagement.Engine.Symbols.ReadArray(sym.Offset, realType, elsz);

								if (arr != null)_valueString = BuildArrayContentString(arr, IsString);
							}
						}

						else
						{
							//TODO: call an object's toString method somehow to obtain its representing string manually
						}
					}
				}
				}
			}

			#region Exression evaluation
        public static Type DetermineArrayType(int Token, out uint size, out bool IsString)
        {
            IsString = false;
            Type t = typeof(int);
            size = 4;
            switch (Token)
            {
                default:
                    break;
                case DTokens.Char:
                    IsString = true;
                    t = typeof(byte);
                    size = 1;
                    break;
                case DTokens.Wchar:
                    IsString = true;
                    t = typeof(ushort);
                    size = 2;
                    break;
                case DTokens.Dchar:
                    IsString = true;
                    t = typeof(uint);
                    size = 4;
                    break;

                case DTokens.Ubyte:
                    t = typeof(byte); size = 1;
                    break;
                case DTokens.Ushort:
                    t = typeof(ushort); size = 2;
                    break;
                case DTokens.Uint:
                    t = typeof(uint); size = 4;
                    break;
                case DTokens.Int:
                    t = typeof(int); size = 4;
                    break;
                case DTokens.Short:
                    t = typeof(short); size = 2;
                    break;
                case DTokens.Byte:
                    t = typeof(sbyte); size = 1;
                    break;
                case DTokens.Float:
                    t = typeof(float); size = 4;
                    break;
                case DTokens.Double:
                    t = typeof(double); size = 8;
                    break;
                case DTokens.Ulong:
                    t = typeof(ulong); size = 8;
                    break;
                case DTokens.Long:
                    t = typeof(long); size = 8;
                    break;
            }
            return t;
        }

        public object[] ExtractArray(ulong Offset, ITypeDeclaration Type, out bool IsString)
        {
			ITypeDeclaration BaseValueType = Type.MostBasic;
            string type = (Type.MostBasic as NormalDeclaration).Name;
			IsString = false;
			object[] ret = null;
            uint elsz = 4;
			/*
            Type t = DetermineArrayType(type, out elsz, out IsString);
            
            if (!IsString) t = DetermineArrayType(DCodeCompletionProvider.RemoveArrayPartFromDecl(type), out elsz, out IsString);
            if ((IsString && DimCount < 1) || (!IsString && DimCount < 2))
                ret = dbg.Symbols.ReadArray(Offset, t, elsz);
            else
            {
                ret = dbg.Symbols.ReadArrayArray(Offset, t, elsz);
            }*/
            return ret;
        }

        public static string BuildArrayContentString(object[] marr, bool IsString)
        {
            string str = "";
            if (marr != null)
            {
                var t = marr[0].GetType();
                if (IsString && !t.IsArray)
                {
                    try
                    {
                        str = "\"";
                        foreach (object o in marr)
                        {
                            if (o is uint)
                                str += Char.ConvertFromUtf32((int)(uint)o);
                            else if (o is UInt16)
                                str += (char)(ushort)o;
                            else if (o is byte)
                                str += (char)(byte)o;
                        }
                        str += "\"";
                    }
                    catch { str = "[Invalid / Not assigned]"; }

                }
                else
                {
                    str = "{";
                    foreach (object o in marr)
                    {
                        if (t.IsArray)
                            str += BuildArrayContentString((object[])o, IsString) + "; ";
                        else
                            str += o.ToString() + "; ";
                    }
                    str = str.Trim().TrimEnd(';') + "}";
                }
            }
            return str;
        }

        public string BuildArrayContentString(ulong Offset, ITypeDeclaration type)
        {
            bool IsString;
            object[] marr = ExtractArray(Offset, type, out IsString);
            return BuildArrayContentString(marr, IsString);
        }
#endregion

			public static bool IsBasicType(ITypeDeclaration t)
			{
				return (t is DTokenDeclaration && DTokens.BasicTypes[(t as DTokenDeclaration).Token]);
			}

			public static bool IsBasicType(INode node)
			{
					return (node != null && node.Type is DTokenDeclaration && DTokens.BasicTypes[ (node.Type as DTokenDeclaration).Token]);
			}

			public static bool IsArray(ITypeDeclaration t)
			{
				return (t is ClampDecl && (t as ClampDecl).IsArrayDecl);
			}

			//TODO: Read out (array!) values, class defintion locations and rename base class references to 'base'
			public static bool IsArray(INode node)
			{
				return (node != null && node.Type is ClampDecl && (node.Type as ClampDecl).IsArrayDecl);
			}

			string _valueString;
			public override string ValueString
			{
				get { return _valueString; }
			}

			string _typeString;
			public override string TypeString
			{
				get { return _typeString; }
			}
		}
	}
}
