using System;

namespace D_IDE.D.CodeCompletion
{
	/// <summary>
	/// Description of DAfterSpaceCompletion.
	/// </summary>
	public class DAfterSpaceCompletion
	{
		/*
		 * Notes:
		 * After space completion occurs after a ' ' was typed.
		 * 
		 * Use cases:
		 * 
		 * import %; (Module (/Package) name stubs only)
		 * % (Ctrl+Alt) (special case: show all types available in all modules; public only)
		 * new %;
		 * case %:
		 * 
		 * In all these cases, show a type list only!
		 * 
		 * cast(%) (Types, CastQualifier)
		 * is(%:%²) (Types; %²: Types, TypeSpecification tokens [e.g. return, delegate, function])
		 * class abc : % (non-final classes, interfaces), % (interfaces only!)
		 * immutable|const(%) (Types)
		 * 
		 * Important: Find out, where providing completion is not required!
		 * 
		 * class|struct|interface|template(%) % {}
		 * int %;
		 */
		
		public DAfterSpaceCompletion()
		{
		}
	}
}
