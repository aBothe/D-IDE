using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;

namespace D_Parser.Resolver
{
	public abstract class RootsEnum
	{
		public static DVariable __ctfe;

		static RootsEnum()
		{
			__ctfe = new DVariable
			{
				Name = "__ctfe",
				Type = new DTokenDeclaration(DTokens.Bool),
				Initializer = new TokenExpression(DTokens.True),
				Description = @"The __ctfe boolean pseudo-vari­able, 
which eval­u­ates to true at com­pile time, but false at run time, 
can be used to pro­vide an al­ter­na­tive ex­e­cu­tion path 
to avoid op­er­a­tions which are for­bid­den at com­pile time.",
			};

			__ctfe.Attributes.Add(new DAttribute(DTokens.Static));
			__ctfe.Attributes.Add(new DAttribute(DTokens.Const));
		}

		protected abstract void HandleItem(INode n);

		protected virtual void HandleItems(IEnumerable<INode> nodes)
		{
			foreach (var n in nodes)
				HandleItem(n);
		}

		public void IterateThroughScopeLayers(
			ResolverContext ctxt, 
			CodeLocation Caret, 
			MemberTypes VisibleMembers= MemberTypes.All)
		{
			#region Current module/scope related members

			// 1)
			if (ctxt.ScopedStatement != null)
			{
				var hierarchy = BlockStatement.GetItemHierarchy(ctxt.ScopedStatement, Caret);

				HandleItems(hierarchy);
			}

			var curScope = ctxt.ScopedBlock;

			// 2)
			while (curScope != null)
			{
				// Walk up inheritance hierarchy
				if (curScope is DClassLike)
				{
					var curWatchedClass = curScope as DClassLike;
					// MyClass > BaseA > BaseB > Object
					while (curWatchedClass != null)
					{
						if (curWatchedClass.TemplateParameters != null)
							HandleItems(curWatchedClass.TemplateParameterNodes as IEnumerable<INode>);

						foreach (var m in curWatchedClass)
						{
							var dm2 = m as DNode;
							var dm3 = m as DMethod; // Only show normal & delegate methods
							if (!DResolver.CanAddMemberOfType(VisibleMembers, m) || dm2 == null ||
								(dm3 != null && !(dm3.SpecialType == DMethod.MethodType.Normal || dm3.SpecialType == DMethod.MethodType.Delegate))
								)
								continue;

							// Add static and non-private members of all base classes; 
							// Add everything if we're still handling the currently scoped class
							if (curWatchedClass == curScope || dm2.IsStatic || !dm2.ContainsAttribute(DTokens.Private))
								HandleItem(m);
						}

						// Stop adding if Object class level got reached
						if (!string.IsNullOrEmpty(curWatchedClass.Name) && curWatchedClass.Name.ToLower() == "object")
							break;

						// 3)
						var baseclassDefs = DResolver.ResolveBaseClass(curWatchedClass, ctxt);

						if (baseclassDefs == null || baseclassDefs.Length < 0)
							break;
						if (curWatchedClass == baseclassDefs[0].ResolvedTypeDefinition)
							break;
						curWatchedClass = baseclassDefs[0].ResolvedTypeDefinition as DClassLike;
					}
				}
				else if (curScope is DMethod)
				{
					var dm = curScope as DMethod;

					// Add 'out' variable if typing in the out test block currently
					if (dm.OutResultVariable != null && dm.Out != null && dm.GetSubBlockAt(Caret) == dm.Out)
						HandleItem(DResolver.BuildOutResultVariable(dm));

					if (VisibleMembers.HasFlag(MemberTypes.Variables))
						HandleItems(dm.Parameters);

					if (dm.TemplateParameters != null)
						HandleItems(dm.TemplateParameterNodes as IEnumerable<INode>);

					// The method's declaration children are handled above already via BlockStatement.GetItemHierarchy().
					// except AdditionalChildren:
					foreach (var ch in dm.AdditionalChildren)
						if (DResolver.CanAddMemberOfType(VisibleMembers, ch))
							HandleItem(ch);

					// If the method is a nested method,
					// this method won't be 'linked' to the parent statement tree directly - 
					// so, we've to gather the parent method and add its locals to the return list
					if (dm.Parent is DMethod)
					{
						var parDM = dm.Parent as DMethod;
						var nestedBlock = parDM.GetSubBlockAt(Caret);

						// Search for the deepest statement scope and add all declarations done in the entire hierarchy
						HandleItems(BlockStatement.GetItemHierarchy(nestedBlock.SearchStatementDeeply(Caret), Caret));
					}
				}
				else foreach (var n in curScope)
					{
						// Add anonymous enums' items
						if (n is DEnum && string.IsNullOrEmpty(n.Name) && DResolver.CanAddMemberOfType(VisibleMembers, n))
						{
							HandleItems((n as DEnum).Children);
							continue;
						}

						var dm3 = n as DMethod; // Only show normal & delegate methods
						if (
							!DResolver.CanAddMemberOfType(VisibleMembers, n) ||
							(dm3 != null && !(dm3.SpecialType == DMethod.MethodType.Normal || dm3.SpecialType == DMethod.MethodType.Delegate)))
							continue;

						HandleItem(n);
					}

				curScope = curScope.Parent as IBlockNode;
			}

			// Add __ctfe variable
			if(DResolver.CanAddMemberOfType(VisibleMembers, __ctfe))
				HandleItem(__ctfe);

			#endregion

			#region Global members
			// Add all non-private and non-package-only nodes
			foreach (var mod in ctxt.ImportCache)
			{
				if (mod.FileName == (ctxt.ScopedBlock.NodeRoot as IAbstractSyntaxTree).FileName)
					continue;

				foreach (var i in mod)
				{
					var dn = i as DNode;
					if (dn != null)
					{
						// Add anonymous enums' items
						if (dn is DEnum &&
							string.IsNullOrEmpty(i.Name) &&
							dn.IsPublic &&
							!dn.ContainsAttribute(DTokens.Package) &&
							DResolver.CanAddMemberOfType(VisibleMembers, i))
						{
							HandleItems((i as DEnum).Children);
							continue;
						}

						if (dn.IsPublic && !dn.ContainsAttribute(DTokens.Package) &&
							DResolver.CanAddMemberOfType(VisibleMembers, dn))
							HandleItem(dn);
					}
					else
						HandleItem(i);
				}
			}
			#endregion
		}
	}
}
