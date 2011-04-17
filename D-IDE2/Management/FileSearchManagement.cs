using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE
{
	partial class IDEManager
	{
		public class FileSearchManagement
		{
			public enum SearchLocations
			{
				CurrentDocument,
				OpenDocuments,
				CurrentProject,
				CurrentSolution
			}

			[Flags]
			public enum SearchFlags
			{
				CaseSensitive,
				FullWord,
				Upward
			}

			// Primary search options
			public static SearchLocations CurrentSearchLocation { get; set; }
			public static string CurrentSearchString { get; set; }
			public static string CurrentReplaceString { get; set; }

			// Secondary search options
			public static SearchFlags SearchOptions { get; set; }
			public static bool ResetSearchOnNextFindCall { get; set; }
			
			static List<string> lastSearchStrings = new List<string>();
			public static List<string> LastSearchStrings { get { return lastSearchStrings; } }

			static List<string> lastReplaceStrings = new List<string>();
			public static List<string> LastReplaceStrings { get { return lastReplaceStrings; } }

			static int ScanInFile(string file, int startOffset, string searchString, SearchFlags flags)
			{
				var ret = 0;



				return ret;
			}

			static bool FindNext_Internal(out string file, out int offset)
			{
				file = "";
				offset = 0;

				/* 
				 * 1) Build array of scannable files
				 * 1.5) Get file/offset of the currently opened file - to find the next occurence of CurrentSearchString
				 * 2) Go through all files (Note: if already opened, take the EditorDocument-object to search)
				 */

				var files = new List<string>();

				switch (CurrentSearchLocation)
				{
					case SearchLocations.CurrentDocument:
						var ed = IDEManager.Instance.CurrentEditor;
						if (ed == null)
							return false;
						break;
				}

				return true;
			}

			public static void FindNext()
			{
				

			}
		}
	}
}
