using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core
{
	public class Solution
	{
		public Solution LoadFromFile(string FileName)
		{

		}

		public void Save()
		{

		}

		#region Properties
		public string Name { get; set; }
		public string FileName { get; set; }

		readonly List<string> _ProjectFiles = new List<string>();
		int _StartPrjIndex = 0;

		public string[] ProjectFiles {
			get { 
				return _ProjectFiles.ToArray(); 
			}
		}


		public readonly List<IProject> ProjectCache = new List<IProject>();

		public IProject LoadProject(string Project)
		{
			if (!_ProjectFiles.Contains(Project))
				return null;

			IDEInterface.Current.LoadProject(Project);

            return null;
		}

		public IProject StartProject
		{
			get {
				if (_StartPrjIndex < _ProjectFiles.Count)
				{
					return ProjectCache[_StartPrjIndex];
				}
				return null;
			}
			set {
				if (ProjectCache.Contains(value))
					_StartPrjIndex = ProjectCache.IndexOf(value);
				else throw new Exception("Project "+value.Name+" is not part of this Solution");
			}
		}
		#endregion
	}
}
