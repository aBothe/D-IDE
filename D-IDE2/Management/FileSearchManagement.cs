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

			#region Public API
                #region Search Parameters
            const int StringHistoryCount = 10;
            const int ContextPadding = 10;

            string currentSearchString;
            LinkedList<string> lastSearchStrings = new LinkedList<string>();
			public string CurrentSearchString { 
				get { return currentSearchString; } 
				set {
					if (currentSearchString == value)
						return;

                    lastSearchStrings.AddFirst(value);
					if(lastSearchStrings.Count > StringHistoryCount)
						lastSearchStrings.RemoveLast();

					currentSearchString = value;
				} 
			}
            public ICollection<string> LastSearchStrings { get { return lastSearchStrings; } }

            string currentReplaceString;
            LinkedList<string> lastReplaceStrings = new LinkedList<string>();
			public string CurrentReplaceString
			{
				get { return currentReplaceString; }
				set
				{
					if (currentReplaceString == value)
						return;

                    lastReplaceStrings.AddFirst(value);
					if(lastReplaceStrings.Count > StringHistoryCount)
						lastReplaceStrings.RemoveLast();

					currentReplaceString = value;
				}
			}
			public ICollection<string> LastReplaceStrings { get { return lastReplaceStrings; } }

            public enum SearchLocations
            {
                CurrentDocument = 0,
                OpenDocuments = 1,
                CurrentProject = 2,
                CurrentSolution = 3
            }
            public SearchLocations CurrentSearchLocation { get; set; }

            [Flags]
            public enum SearchFlags
            {
                CaseSensitive = 1,
                FullWord = 1 << 2,
                Upward = 1 << 3,
                Wrap = 1 << 4
            }
            public SearchFlags SearchOptions { get; set; }
                #endregion

            /// <summary>
            /// Reset the search position and wrap point.
            /// </summary>
            public void ResetSearch(bool startAtCaret = true)
            {
                currentFile = null;
                startOffset = -1;

                if (CurrentSearchLocation == SearchLocations.CurrentDocument)
                {
                    var ed = IDEManager.Instance.CurrentEditor as EditorDocument;

                    if (ed != null)
                    {
                        currentFile = ed.AbsoluteFilePath;
                        if (startAtCaret)
                        {
                            startOffset = ed.Editor.TextArea.Caret.Offset - 1;
                            if (startOffset >= ed.Editor.SelectionStart && startOffset < (ed.Editor.SelectionStart + ed.Editor.SelectionLength))
                            {
                                startOffset = ed.Editor.SelectionStart;
                                if (SearchOptions.HasFlag(SearchFlags.Upward))
                                    startOffset += ed.Editor.SelectionLength;
                            }
                        }
                    }
                }

                currentOffset = startOffset;
                stopOffset = -1;
            }

            /// <summary>
            /// Selects the next match.
            /// </summary>
            public void FindNext()
            {
                if (NextMatch() == null)
                    return;

                var ed = WorkbenchLogic.Instance.OpenFile(currentFile, currentOffset) as EditorDocument;
                if (ed != null)
                {
                    // Select found match
                    ed.Editor.SelectionStart = currentOffset;
                    ed.Editor.SelectionLength = CurrentSearchString.Length;
                }
            }
            /// <summary>
            /// Outputs a list of all matches to the search results panel of the main window.
            /// </summary>
            public void FindAll() { MatchAll(false); }

            /// <summary>
            /// Replaces the current match and selects the next one.
            /// </summary>
            public void ReplaceNext() {
                if (IDEManager.Instance.CurrentEditor is EditorDocument)
                {
                    var ed = (IDEManager.Instance.CurrentEditor as EditorDocument).Editor;
                    var compMode = SearchOptions.HasFlag(SearchFlags.CaseSensitive) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    if (null != ed && string.Equals(currentSearchString, ed.SelectedText, compMode))
                        ed.Document.Replace(ed.SelectionStart, ed.SelectionLength, currentReplaceString);
                }

                FindNext();
            }
            /// <summary>
            /// Replaces all matches.
            /// </summary>
            public void ReplaceAll() { MatchAll(true); }
			#endregion

            // This region could be simpler, faster, and more correct if FileSearchManagement could subscribe to update events for all searchable files.
			#region Private Implementation
            string currentFile;
            int startOffset;
            int currentOffset;
            int stopOffset;

            protected FileSearchManagement()
            {
                currentSearchString = "";
                currentReplaceString = "";

                currentFile = null;
                startOffset = -1;
                currentOffset = -1;
                stopOffset = -1;
            }
            protected FileSearchManagement(FileSearchManagement original)
            {
                currentSearchString = original.currentSearchString;
                currentReplaceString = original.currentReplaceString;
                CurrentSearchLocation = original.CurrentSearchLocation;
                SearchOptions = original.SearchOptions;

                currentFile = original.currentFile;
                startOffset = original.startOffset;
                currentOffset = original.currentOffset;
                stopOffset = original.stopOffset;
            }

            bool NextFile()
            {
                bool isNext = false;

                switch (CurrentSearchLocation)
                {
                    case SearchLocations.CurrentDocument:
                        {
                            var ed = IDEManager.Instance.CurrentEditor;
                            if (ed == null)
                                break;

                            var edFile = ed.AbsoluteFilePath;
                            if (edFile.Equals(currentFile))
                                break;

                            currentFile = ed.AbsoluteFilePath;
                            isNext = true;
                            break;
                        }
                    case SearchLocations.OpenDocuments:
                        {
                            var openEds = IDEManager.Instance.Editors;
                            if (openEds.Count == 0)
                                break;

                            var edsEnumer = openEds.GetEnumerator();
                            if (currentFile != null)
                            {
                                // Advance past the last file searched.
                                while (edsEnumer.MoveNext())
                                {
                                    if (edsEnumer.Current.AbsoluteFilePath.Equals(currentFile))
                                        break;
                                }
                            }

                            while (edsEnumer.MoveNext())
                            {
                                // Try to find the next searchable file.
                                if (edsEnumer.Current is EditorDocument)
                                {
                                    currentFile = edsEnumer.Current.AbsoluteFilePath;
                                    isNext = true;
                                    break;
                                }
                            }

                            // There aren't any more.
                            break;
                        }
                    case SearchLocations.CurrentProject:
                        {
                            if (IDEManager.Instance.CurrentEditor == null || !IDEManager.Instance.CurrentEditor.HasProject)
                                break; // There is no current project.
                            var projFileEnumer = IDEManager.Instance.CurrentEditor.Project.GetEnumerator();

                            if(currentFile != null)
                            {
                                // Advance past the last file searched.
                                while(projFileEnumer.MoveNext())
                                {
                                    if(projFileEnumer.Current.AbsoluteFileName.Equals(currentFile))
                                        break;
                                }
                            }

                            // Switch to the next source module file, if there is one.
                            if (projFileEnumer.MoveNext())
                            {
                                currentFile = projFileEnumer.Current.AbsoluteFileName;
                                isNext = true;
                                break;
                            }

                            // There aren't any more.
                            break;
                        }
                    case SearchLocations.CurrentSolution:
                        {
                            if (IDEManager.CurrentSolution == null)
                                break;
                            var projEnumer = IDEManager.CurrentSolution.GetEnumerator();
                            IEnumerator<SourceModule> projFileEnumer;
                            if (projEnumer.MoveNext())
                                projFileEnumer = projEnumer.Current.GetEnumerator();
                            else
                                break; // The solution is empty.

                            if (currentFile != null)
                            {
                                // Advance past the last file searched.
                                while(true) {
                                    while (projFileEnumer.MoveNext())
                                    {
                                        if (projFileEnumer.Current.AbsoluteFileName.Equals(currentFile))
                                            goto Resume;
                                    }

                                    if(projEnumer.MoveNext()) // This has to be at the end of the iteration because projFileEnumer was initialized earlier.
                                        projFileEnumer = projEnumer.Current.GetEnumerator();
                                }
                            }

                        Resume:
                            // Switch to the next source module file, if there is one.
                            if (projFileEnumer.MoveNext())
                            {
                                currentFile = projFileEnumer.Current.AbsoluteFileName;
                                isNext = true;
                                break;
                            }

                            // There aren't any more.
                            break;
                        }
                    default:
                        throw new NotSupportedException("An invalid search location was selected.");
                }

                if (isNext)
                {
                    startOffset = -1;
                    currentOffset = -1;
                    stopOffset = -1;
                }
                return isNext;
            }
            bool IsIdentifierChar(char ch)
            {
                return char.IsLetterOrDigit(ch) || ch == '_';
            }
            string NextMatch(string haystack = null)
            {
                if (currentFile == null)
                {
                    haystack = null;
                    if(!NextFile())
                        return null;
                }

                while (currentFile != null)
                {
                    if (haystack == null)
                    {
                        // Use the working copy if the file is already open.
                        foreach (var ed in IDEManager.Instance.Editors)
                        {
                            if (!ed.AbsoluteFilePath.Equals(currentFile))
                                continue;

                            haystack = (ed as EditorDocument).Editor.Text;
                        }
                        // Otherwise load the file from disk.
                        if (haystack == null)
                        {
                            if (!File.Exists(currentFile))
                                continue; // Nothing to see here; try the next file.

                            haystack = File.ReadAllText(currentFile);
                        }
                    }

                Search:
                    StringComparison compMode = SearchOptions.HasFlag(SearchFlags.CaseSensitive) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    int matchX = -1;
                    bool found;
                    if (SearchOptions.HasFlag(SearchFlags.Upward)) {
                        if (currentOffset == -1)
                            currentOffset = haystack.Length;
                        currentOffset--;
                        if(currentOffset >= 0)
                            matchX = haystack.LastIndexOf(currentSearchString, currentOffset, compMode);

                        found = (matchX != -1 && matchX > stopOffset);
                    } else {
                        currentOffset++;
                        if(currentOffset < haystack.Length)
                            matchX = haystack.IndexOf(currentSearchString, currentOffset, compMode);

                        found = (matchX != -1 && (stopOffset == -1 || matchX < stopOffset));
                    }

                    if (found)
                    {
                        currentOffset = matchX;
                        if (SearchOptions.HasFlag(SearchFlags.FullWord))
                        {
                            if ((currentOffset > 0 && IsIdentifierChar(haystack[currentOffset - 1]))
                                || (currentOffset < (haystack.Length - 1) && IsIdentifierChar(haystack[currentOffset + 1])))
                                goto Search;
                        }
                        return haystack;
                    }

                    if (startOffset != -1 && SearchOptions.HasFlag(SearchFlags.Wrap))
                    {
                        stopOffset = startOffset;
                        currentOffset = -1;
                        startOffset = -1;
                        goto Search;
                    }

                    if (!NextFile())
                        return null;
                    haystack = null;
                }

                return null;
            }
            void MatchAll(bool replace = false)
            {
                var searcher = new FileSearchManagement(this);
                if(SearchOptions.HasFlag(SearchFlags.Upward))
                    searcher.SearchOptions ^= SearchFlags.Upward; // This isn't necessary, and could cause subtle bugs.
                searcher.ResetSearch(false);

                var matches = new LinkedList<SearchResult>();
                string haystack = null;
                while ((haystack = searcher.NextMatch(haystack)) != null)
                {
                    if (replace)
                    {
                        var ed = WorkbenchLogic.Instance.OpenFile(searcher.currentFile, searcher.currentOffset) as EditorDocument;
                        ed.Editor.Document.Replace(searcher.currentOffset, currentSearchString.Length, currentReplaceString);
                        haystack = ed.Editor.Document.Text;
                    }

                    int lineEnd = 0;
                    int line = 0;
                    int column = 0;
                    while (lineEnd < haystack.Length)
                    {
                        if (haystack[lineEnd] == '\n')
                        {
                            ++line;

                            if (lineEnd >= searcher.currentOffset)
                                break;

                            column = 0;
                        }

                        ++lineEnd;
                        ++column;
                    }
                    int lineStart = lineEnd - column;
                    string context = haystack.Substring(lineStart, column).Trim();
                    column = searcher.currentOffset - lineStart;

                    matches.AddLast(new SearchResult
                    {
                        File = searcher.currentFile,
                        Offset = searcher.currentOffset,
                        Line = line,
                        Column = column,
                        CodeSnippet = context
                    });
                }

                var pan = IDEManager.Instance.MainWindow.SearchResultPanel;
                pan.SearchString = replace? currentReplaceString : currentSearchString;
                pan.Results = matches;
                pan.Show();
            }
			#endregion

            #region Old Implementation
            /*/// <summary>
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
                    if (ed_ == null || ed.AbsoluteFilePath != file)
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
                        ret = fileContent.LastIndexOf(searchString, ret - 1,
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

            bool FindNext_Internal(out string file, out int offset, bool ignoreCurrentSelection = false)
            {
                file = "";
                offset = -1;

                // 1) Build array of scannable files
                // 2) Get file/offset of the currently opened file - to find the next/previous occurence of CurrentSearchString
                // 3) Go through all files (Note: if already opened, search within the EditorDocument object)

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
                    if (!ignoreCurrentSelection)
                        startOffset = curEd.Editor.SelectionStart + (SearchOptions.HasFlag(SearchFlags.Upward) ? 0 : curEd.Editor.SelectionLength);
                }

                // 3)
                int deltaJ = SearchOptions.HasFlag(SearchFlags.Upward) ? -1 : 1; // If searching "upward", move backward in the file list - decrement j
                for (var j = startFilesIndex; j >= 0 && j < files.Count; j += deltaJ)
                {
                    file = files[j];
                    var res = ScanFile(file, j == startFilesIndex ? startOffset : 0, CurrentSearchString, SearchOptions);

                    if (res >= 0)
                    {
                        offset = res;
                        return true;
                    }
                }

                return false;
            }
            /// <summary>
            /// Finds the next match (if possible), then opens the matching file and selects the matching text.
            /// </summary>
            public void FindNext()
            {
                string file = "";
                int offset = 0;

                if (!FindNext_Internal(out file, out offset))
                    return;

                var ed = WorkbenchLogic.Instance.OpenFile(file, offset) as EditorDocument;
                if (ed == null)
                    return;

                // Select found match
                ed.Editor.SelectionStart = offset;
                ed.Editor.SelectionLength = CurrentSearchString.Length;
            }*/

            /*/// <summary>
            /// Finds all matches and returns an array of them.
            /// </summary>
            SearchResult[] FindAll_Raw()
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
                        offset = fileContent.IndexOf(currentSearchString, offset,
                            SearchOptions.HasFlag(SearchFlags.CaseSensitive) ?
                            StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

                        if (offset < 0)
                            break;

                        // (If full-word search activated,) Check if result isn't surrounded by letters. Otherwise continue search
                        if (SearchOptions.HasFlag(SearchFlags.FullWord) && (
                                (fileContent.Length > offset + currentSearchString.Length - 1 &&
                                char.IsLetter(fileContent[offset + currentSearchString.Length]))
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
                        l.Add(new SearchResult
                        {
                            File = file,
                            Offset = offset,
                            Line = Line,
                            Column = Col,
                            CodeSnippet = fileContent.Substring(leftPad, rightPad - leftPad).Trim()
                        });

                        offset += currentSearchString.Length;
                    }
                }

                return l.ToArray();
            }
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
			}*/

            /*/// <summary>
            /// Replaces the current match (if any), then finds the next match (if possible) and opens the matching file,
            /// selecting the matching text.
            /// </summary>
			public bool ReplaceNext()
			{
				string file = "";
				int offset = 0;

				bool foundMatch=FindNext_Internal(out file, out offset);

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
            }*/
            #endregion
        }
	}
}
