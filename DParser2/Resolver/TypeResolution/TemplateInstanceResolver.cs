using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Dom;

namespace D_Parser.Resolver.TypeResolution
{
	public class TemplateInstanceParameterHandler
	{
		/// <summary>
		/// Takes the given resolvedTypes, tries to associate 
		/// the given template instance expression to each of them 
		/// and returns the most fitting TypeResult object(s).
		/// 
		/// Note:
		/// An Argument is used as the expression which specifies a type in a template instance (e.g. T!int -- int is an argument)
		/// A Parameter is used in the declaration (in template T(U) it'd be the parameter 'U')
		/// </summary>
		public static ResolveResult[] ResolveAndFilterTemplateResults(
			TemplateInstanceExpression tix,
			IEnumerable<ResolveResult> resolvedTypes,
			ResolverContextStack ctxt)
		{
			return ResolveAndFilterTemplateResults(tix.Arguments, resolvedTypes, ctxt);
		}

		/// <summary>
		/// Used if a member method takes template arguments but doesn't have explicit ones given.
		/// 
		/// So, writeln(123) will be interpreted as writeln!int(123);
		/// </summary>
		public static ResolveResult[] ResolveAndFilterTemplateResults(
			IExpression[] templateArguments,
			IEnumerable<ResolveResult> resolvedTypes,
			ResolverContextStack ctxt)
		{
			var templateArgs = new List<ResolveResult[]>();
			// Note: If an arg wasn't able to be resolved (returns null) - add it anyway to keep the indexes parallel
			if (templateArguments != null)
				foreach (var arg in templateArguments)
					templateArgs.Add(ExpressionTypeResolver.Resolve(arg, ctxt));

			return ResolveAndFilterTemplateResults(templateArgs.Count > 0 ? templateArgs : null, resolvedTypes, ctxt);
		}

		public static ResolveResult[] ResolveAndFilterTemplateResults(
			IEnumerable<ResolveResult[]> resolvedTemplateArguments, 
			IEnumerable<ResolveResult> resolvedTypes,
			ResolverContextStack ctxt)
		{
			if (resolvedTypes == null)
				return null;

			
			var returnedTemplates = new List<ResolveResult>();

			foreach (var rr in DResolver.TryRemoveAliasesFromResult(resolvedTypes))
			{
				var dn = DResolver.GetResultMember(rr) as DNode;

				if (dn == null || dn is IAbstractSyntaxTree)
					continue;

				// Of course, if neither parameters nor arguments are given, return the result immediately
				if (dn.TemplateParameters == null || dn.TemplateParameters.Length == 0)
				{
					if (resolvedTemplateArguments == null)
						returnedTemplates.Add(rr);
					continue;
				}
				
				
				/*
				 * Things that need attention:
				 * -- Default arguments (Less args than parameters)
				 * -- Type specializations
				 * -- Type tuples (More args than parameters)
				 */
				int i = 0;
				for (; i < dn.TemplateParameters.Length; i++)
				{
					
				}
			}

			if (returnedTemplates.Count == 0)
				return null;
			return returnedTemplates.ToArray();
		}

		/// <summary>
		/// Returns the specialization expression/type declaration of a template parameter
		/// </summary>
		public static object GetTypeSpecialization(ITemplateParameter p)
		{
			if (p is TemplateAliasParameter)
			{
				var tap = p as TemplateAliasParameter;

				return (object)tap.SpecializationExpression ?? tap.SpecializationType;
			}
			else if (p is TemplateThisParameter)
				return GetTypeSpecialization((p as TemplateThisParameter).FollowParameter);
			else if (p is TemplateTupleParameter)
				return null;
			else if (p is TemplateTypeParameter)
				return (p as TemplateTypeParameter).Specialization;
			else if (p is TemplateValueParameter)
				return (p as TemplateValueParameter).SpecializationExpression;

			return null;
		}

		public static object GetTypeDefault(ITemplateParameter p)
		{
			if (p is TemplateAliasParameter)
			{
				var tap = p as TemplateAliasParameter;

				return (object)tap.DefaultExpression ?? tap.DefaultType;
			}
			else if (p is TemplateThisParameter)
				return GetTypeDefault((p as TemplateThisParameter).FollowParameter);
			else if (p is TemplateTupleParameter)
				return null;
			else if (p is TemplateTypeParameter)
				return (p as TemplateTypeParameter).Default;
			else if (p is TemplateValueParameter)
				return (p as TemplateValueParameter).DefaultExpression;

			return null;
		}
	}
}
