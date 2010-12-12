using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core
{
	public interface ISolution
	{
		string Name { get; set; }
		string FileName { get; set; }
		IProject StartProject { get; set; }
		List<IProject> Projects { get; }

		void Save();
		void Reload();
		void LoadFromFile(string FileName);

		void Build();
		void BuildIncrementally();
		void CleanUpOutput();
	}
}
