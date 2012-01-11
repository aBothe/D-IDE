using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using D_IDE.Core.Controls;

namespace D_IDE
{
	partial class IDEManager
	{
		public class FileSearchManagement
		{
			public static FileSearchManagement Instance = new FileSearchManagement();

			#region Properties
			public enum SearchLocations
			{
				CurrentDocument=0,
				OpenDocuments=1,
				CurrentProject=2,
				CurrentSolution=3
			}

			[Flags]
			public enum SearchFlags
			{
				CaseSensitive,
				FullWord,
				Upward
			}

			// Primary search criteria
			public SearchLocations CurrentSearchLocation { get; set; }

			string currentSearchString;
			string currentReplaceString;
			public string CurrentSearchString { 
				get { return currentSearchString; } 
				set {
					if (currentSearchString == value)
						return;

					if(lastSearchStrings.Count>9)
						lastSearchStrings.RemoveAt(9);
					lastSearchStrings.Insert(0, value);

					currentSearchString = value; 
				} 
			}
			public string CurrentReplaceString
			{
				get { return currentReplaceString; }
				set
				{
					if (currentReplaceString == value)
						return;

					if(lastReplaceStrings.Count>9)
						lastReplaceStrings.RemoveAt(9);
					lastReplaceStrings.Insert(0, value);

					currentReplaceString = value;
				}
			}

			public SearchFlags SearchOptions { get; set; }

			
			List<string> lastSearchStrings = new List<string>();
			public List<string> LastSearchStrings { get { return lastSearchStrings; } }

			List<string> lastReplaceStrings = new List<string>();
			public List<string> LastReplaceStrings { get { return lastReplaceStrings; } }

			#endregion

			#region Internal methods
			/// <summary>
			/// Searches a string in a file. Works independently from FileSearchManagement class.
			/// </summary>
			/// <returns>-1 if nothing has been found. Otherwise the offset of the first occurence</returns>
			static int ScanFile(string file, int startOffset, string searchString, SearchFlags flags)
			{
				var ret = startOffset;
				string fileContent = "";

				//Note: If file is already open, take the EditorDocument instance instead loading the physical file
				foreach (var ed in IDEManager.Instance.Editors)
				{
					var ed_ = ed as EditorDocument;
					if (ed_ ==null || ed.AbsoluteFilePath!=file)
						continue;

					fileContent = ed_.Editor.Text;
				}

				if (string.IsNullOrEmpty(fileContent))
					if (File.Exists(file))
						File.ReadAllText(file);

				if (string.IsNullOrEmpty(fileContent))
					return -1;

				if (flags.HasFlag(SearchFlags.Upward))
					while (true)
					{
						// Search last occurence of searchString between 0 and ret
						ret = fileContent.LastIndexOf(searchString,ret-1,
								flags.HasFlag(SearchFlags.CaseSensitive) ?
								StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

						// (If full-word search activated,) Check if result isn't surrounded by letters. Otherwise continue search
						if (ret > -1 && flags.HasFlag(SearchFlags.FullWord) && (
								(fileContent.Length > ret + searchString.Length - 1 && char.IsLetter(fileContent[ret + searchString.Length]))
								|| (ret > 0 && char.IsLetter(fileContent[ret - 1]))
							))
						{
							ret++;
							continue;
						}

						break;
					}
				else
					while (true)
					{
						// Search first occurence of searchString between ret and fileContent.Length
						ret = fileContent.IndexOf(searchString, ret,
								flags.HasFlag(SearchFlags.CaseSensitive) ?
								StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

						// (If full-word search activated,) Check if result isn't surrounded by letters. Otherwise continue search
						if (ret>-1 && flags.HasFlag(SearchFlags.FullWord) && (
								(fileContent.Length > ret + searchString.Length-1 && char.IsLetter(fileContent[ret + searchString.Length]))
								|| (ret>0 && char.IsLetter(fileContent[ret-1]))
							))
						{
							ret++;
							continue;
						}

						break;
					}

				return ret;
			}

			/// <summary>
			/// Builds list containing all files, depending on CurrentSearchLocation
			/// </summary>
			List<string> BuildSearchFileList()
			{
				var files = new List<string>();

				switch (CurrentSearchLocation)
				{
					case SearchLocations.CurrentDocument:
						var ed = IDEManager.Instance.CurrentEditor;
						if (ed == null)
							return null;
						files.Add(ed.AbsoluteFilePath);
						break;
					case SearchLocations.OpenDocuments:
						foreach (var ed2 in IDEManager.Instance.Editors)
							if (ed2 is EditorDocument)
								files.Add(ed2.AbsoluteFilePath);
						break;
					case SearchLocations.CurrentProject:
						if (IDEManager.Instance.CurrentEditor == null || !IDEManager.Instance.CurrentEditor.HasProject)
							return null;

						foreach (var pf in IDEManager.Instance.CurrentEditor.Project)
							files.Add(pf.AbsoluteFileName);

						files.Sort();
						break;
					case SearchLocations.CurrentSolution:
						if (IDEManager.CurrentSolution == null)
							return null;

						foreach (var prj in IDEManager.CurrentSolution)
							foreach (var pf2 in prj)
								files.Add(pf2.AbsoluteFileName);

						files.Sort();
						break;
				}

				return files;
			}

			bool FindNext_Internal(out string file, out int offset, bool ignoreCurrentSelection=false)
			{
				file = "";
				offset = -1;

				/* 
				 * 1) Build array of scannable files
				 * 2) Get file/offset of the currently opened file - to find the next/previous occurence of CurrentSearchString
				 * 3) Go through all files (Note: if already opened, search within the EditorDocument object)
				 */

				// 1)
				var files = BuildSearchFileList();

				if (files == null)
					return false;

				// 2)
				var startFilesIndex = 0;
				int startOffset = 0;

				var curEd = IDEManager.Instance.CurrentEditor as EditorDocument;
				if (curEd != null)
				{
					var i = files.IndexOf(curEd.AbsoluteFilePath);
					if (i >= 0)
						startFilesIndex = i;

					// if searching backward, take the selection start (the caret position) only. If searching downward (as usual), begin to search right after the selected block 
					if(!ignoreCurrentSelection)
						startOffset = curEd.Editor.SelectionStart +( SearchOptions.HasFlag(SearchFlags.Upward)?0: curEd.Editor.SelectionLength);
				}

				// 3)
				int deltaJ = SearchOptions.HasFlag(SearchFlags.Upward) ? -1 : 1; // If searching "upward", move backward in the file list - decrement j
				for (var j = startFilesIndex; j>=0 && j < files.Count; j+=deltaJ)
				{
					file = files[j];
					var res=ScanFile(file, j == startFilesIndex ? startOffset : 0, CurrentSearchString, SearchOptions);

					if (res >= 0)
					{
						offset = res;
						return true;
					}
				}

				return false;
			}
			#endregion

			/// <summary>
			/// Finds all matches and shows them in the search result panel of the main window
			/// </summary>
			public void FindAll()
			{
				var matches = FindAll_Raw();

				var pan = IDEManager.Instance.MainWindow.SearchResultPanel;

				pan.SearchString = CurrentSearchString;
				pan.Results = matches;
				pan.Show();
			}

			/// <summary>
			/// Finds all matches and returns an array of them.
			/// </summary>
			/// <returns></returns>
			public SearchResult[] FindAll_Raw()
			{
				var l = new List<SearchResult>();

				var files = BuildSearchFileList();

				if (files == null)
					return null;

				// Iterate through all files
				foreach (var file in files)
				{
					string fileContent = "";

					//Note: If file is already open, take the EditorDocument instance instead loading the physical file
					foreach (var ed in IDEManager.Instance.Editors)
					{
						var ed_ = ed as EditorDocument;
						if (ed_ == null || ed.AbsoluteFilePath != file)
							continue;

						fileContent = ed_.Editor.Text;
					}

					if (string.IsNullOrEmpty(fileContent))
						if (File.Exists(file))
							File.ReadAllText(file);

					// Search matches until eof

					// lastOffset, Line and Col are used for calculating correct match locations
					int lastOffset = 0;
					int Line = 1;
					int Col = 1;

					int offset = 0;

					while (offset > -1)
					{
						offset = fileContent.IndexOf(currentSearchString,offset,
							SearchOptions.HasFlag(SearchFlags.CaseSensitive) ?
							StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

						if (offset < 0)
							break;

						// (If full-word search activated,) Check if result isn't surrounded by letters. Otherwise continue search
						if (SearchOptions. HasFlag(SearchFlags.FullWord) && (
								(fileContent.Length > offset + currentSearchString. Length - 1 && 
								char.IsLetter(fileContent[offset + currentSearchString. Length]))
								|| (offset > 0 && char.IsLetter(fileContent[offset - 1]))
							))
						{
							offset += currentSearchString.Length;
							continue;
						}

						// Line and Col calculation
						for (int k = lastOffset; k < offset; k++)
						{
							Col++;
							if (fileContent[k] == '\n')
							{
								Line++;
								Col = 1;
							}
						}
						lastOffset = offset;

						// Opt: Extract a code snippet with e.g. 10 surrounding chars on each side
						const int padLen = 10;
						int leftPad = offset > padLen ? (offset - padLen) : 0;
						int rightPad = offset < fileContent.Length - padLen ? (offset + padLen) : fileContent.Length;

						// Add one search result object per match
						l.Add(new SearchResult { 
							File=file, 
							Offset=offset, 
							Line=Line,
							Column=Col,
							CodeSnippet=fileContent.Substring(leftPad, rightPad-leftPad).Trim()
						});

						offset += currentSearchString.Length;
					}
				}

				return l.ToArray();
			}

			public bool FindNext()
			{
				string file = "";
				int offset = 0;

				if (FindNext_Internal(out file, out offset))
				{
					var ed = WorkbenchLogic.Instance.OpenFile(file, offset) as EditorDocument;
					
					if (ed == null)
						return false;

					// Select found match
					ed.Editor.SelectionStart = offset;
					ed.Editor.SelectionLength = CurrentSearchString.Length;

					return true;
				}

				return false;
			}

			public bool ReplaceNext()
			{
				string file = "";
				int offset = 0;

				bool foundMatch=FindNext_Internal(out file, out offset);

				if (string.IsNullOrEmpty(file))
					return false;

				var ed = WorkbenchLogic.Instance.OpenFile(file, offset) as EditorDocument;

				if (ed == null)
					return false;

				// Replace selected string
				if (string.Compare(ed.Editor.SelectedText, CurrentSearchString, !SearchOptions.HasFlag(SearchFlags.CaseSensitive)) == 0)
				{
					ed.Editor.Document.Replace(ed.Editor.SelectionStart, ed.Editor.SelectionLength, CurrentReplaceString);

					if(foundMatch)
						offset += CurrentReplaceString.Length - CurrentSearchString.Length;
				}

				if (foundMatch)
				{
					ed.Editor.SelectionStart = offset;
					ed.Editor.SelectionLength = CurrentSearchString.Length;
				}

				return false;
			}

			public bool ReplaceAll()
			{
				string file = "";
				int offset = 0;
				EditorDocument ed = null;

				while (offset>=0)
				{
					bool foundMatch=FindNext_Internal(out file, out offset);

					if (ed != null && ed.AbsoluteFilePath != file)
						ed.Editor.Document.UndoStack.EndUndoGroup();

					if (string.IsNullOrEmpty(file))
						return false;

					ed = WorkbenchLogic.Instance.OpenFile(file, offset) as EditorDocument;

					if (ed == null)
						return false;

					// Replace selected string
					if (string.Compare(ed.Editor.SelectedText, CurrentSearchString, !SearchOptions.HasFlag(SearchFlags.CaseSensitive)) == 0)
					{
						ed.Editor.Document.UndoStack.StartContinuedUndoGroup(ed);

						ed.Editor.Document.Replace(ed.Editor.SelectionStart, ed.Editor.SelectionLength, CurrentReplaceString);
						if (foundMatch)
							offset += CurrentReplaceString.Length - CurrentSearchString.Length;
					}

					if (foundMatch)
					{
						ed.Editor.SelectionStart = offset;
						ed.Editor.SelectionLength = CurrentSearchString.Length;
					}
				}

				return false;
			}
		}
	}
}
