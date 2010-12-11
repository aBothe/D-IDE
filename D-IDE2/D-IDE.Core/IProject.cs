using System;
using System.Collections.Generic;
using System.Text;

namespace D_IDE.Core
{
	public interface IProject:IEnumerable<IModule>
	{
		string Name { get; set; }
		string FileName { get; set; }

		bool Save();
		bool Reload();

		List<IModule> Modules { get; }
		string[] Files { get; }

		IModule this[string FileName] { get; set; }

		void Add(string FileName);
		void Remove(string FileName);
		void Rename(string OldFileName, string NewFileName);
	}
}
