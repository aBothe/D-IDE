using Parser.Core;
using D_IDE.Core;

namespace D_IDE
{
	class Manager
	{
		public static MainWindow MainWindow;
		/// <summary>
		/// There can be only one open solution. 
		/// Stand-alone modules are opened independently of any other open solutions, projects or modules
		/// </summary>
		public static ISolution CurrentSolution;
	}
}
