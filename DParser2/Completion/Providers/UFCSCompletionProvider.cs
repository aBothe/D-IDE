using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Resolver;

namespace D_Parser.Completion.Providers
{
	/// <summary>
	/// Adds method items to the completion list if the current expression's type is matching the methods' first parameter
	/// </summary>
	public class UFCSCompletionProvider
	{
		public static void Generate(ResolveResult rr, ICompletionDataGenerator gen)
		{
			/*
			 * 1) Have visitor.
			 * 2) Iterate through scope levels.
			 * 3) Check if node is Method, containing at least 1 parameter
			 * 4) Get the first parameter's type.
			 * 5) Compare it to the base expression wrapped by 'rr'
			 *	-- Result comparison!?
			 * 6) Add to completion list if comparison successful
			 */
		}
	}
}
