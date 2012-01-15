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

            string escapedSearchStr;
            string translatedSearchStr;
            LinkedList<string> lastSearchStrings = new LinkedList<string>();
			public string CurrentSearchStr
            { 
				get { return escapedSearchStr; } 
				set {
					if (value.Equals(escapedSearchStr))
						return;

                    lastSearchStrings.AddFirst(value);
					if(lastSearchStrings.Count > StringHistoryCount)
						lastSearchStrings.RemoveLast();

					escapedSearchStr = value;
                    translatedSearchStr = translateEscapes(escapedSearchStr);
				} 
			}
            public ICollection<string> LastSearchStrings { get { return lastSearchStrings; } }

            string escapedReplaceStr;
            string translatedReplaceStr;
            LinkedList<string> lastReplaceStrings = new LinkedList<string>();
			public string CurrentReplaceStr
			{
				get { return escapedReplaceStr; }
				set
				{
					if (value.Equals(escapedReplaceStr))
						return;

                    lastReplaceStrings.AddFirst(value);
					if(lastReplaceStrings.Count > StringHistoryCount)
						lastReplaceStrings.RemoveLast();

					escapedReplaceStr = value;
                    translatedReplaceStr = translateEscapes(escapedReplaceStr);
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
                EscapeSequences = 1,
                CaseSensitive = 1 << 2,
                FullWord = 1 << 3,
                Upward = 1 << 4,
                Wrap = 1 << 5
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
                    ed.Editor.SelectionLength = TranslatedSearchStr.Length;
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
                    if (null != ed && string.Equals(TranslatedSearchStr, ed.SelectedText, compMode))
                        ed.Document.Replace(ed.SelectionStart, ed.SelectionLength, TranslatedReplaceStr);
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
            string TranslatedSearchStr
            {
                get
                {
                    if (SearchOptions.HasFlag(SearchFlags.EscapeSequences))
                        return translatedSearchStr;
                    else
                        return escapedSearchStr;
                }
            }
            string TranslatedReplaceStr
            {
                get
                {
                    if (SearchOptions.HasFlag(SearchFlags.EscapeSequences))
                        return translatedReplaceStr;
                    else
                        return escapedReplaceStr;
                }
            }

            string currentFile;
            int startOffset;
            int currentOffset;
            int stopOffset;

            protected FileSearchManagement()
            {
                escapedSearchStr = "";
                translatedSearchStr = "";
                escapedReplaceStr = "";
                translatedReplaceStr = "";

                currentFile = null;
                startOffset = -1;
                currentOffset = -1;
                stopOffset = -1;
            }
            protected FileSearchManagement(FileSearchManagement original)
            {
                escapedSearchStr = original.escapedSearchStr;
                translatedSearchStr = original.translatedSearchStr;
                escapedReplaceStr = original.escapedReplaceStr;
                translatedReplaceStr = original.translatedReplaceStr;

                CurrentSearchLocation = original.CurrentSearchLocation;
                SearchOptions = original.SearchOptions;

                currentFile = original.currentFile;
                startOffset = original.startOffset;
                currentOffset = original.currentOffset;
                stopOffset = original.stopOffset;
            }

            bool IsIdentifierChar(char ch)
            {
                return char.IsLetterOrDigit(ch) || ch == '_';
            }
            bool IsOctalDigit(char ch)
            {
                return (ch >= '0' && ch <= '7');
            }
            bool IsHexDigit(char ch)
            {
                return (char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f'));
            }
            string translateEscapes(string escapedString)
            {
                StringBuilder translated = new StringBuilder();
                for (int cX = 0; cX < escapedString.Length; ++cX)
                {
                    if (escapedString[cX] == '\\')
                    {
                        ++cX;
                        char ctrlC = escapedString[cX];
                        switch (ctrlC)
                        {
                            case 'a':
                                translated.Append('\a'); break;
                            case 'b':
                                translated.Append('\b'); break;
                            case 'f':
                                translated.Append('\f'); break;
                            case 'n':
                                translated.Append('\n'); break;
                            case 'r':
                                translated.Append('\r'); break;
                            case 't':
                                translated.Append('\t'); break;
                            case 'v':
                                translated.Append('\v'); break;
                            case 'x':
                                {
                                    if ((cX + 2 < escapedString.Length)
                                        && IsHexDigit(escapedString[cX + 1])
                                        && IsHexDigit(escapedString[cX + 2]))
                                    {
                                        translated.Append((char)Convert.ToInt32(escapedString.Substring(cX + 1, 2), 16));
                                        cX += 2;
                                    }
                                    else
                                        goto default;
                                } break;
                            case 'u':
                                {
                                    if ((cX + 4 < escapedString.Length)
                                        && IsHexDigit(escapedString[cX + 1])
                                        && IsHexDigit(escapedString[cX + 2])
                                        && IsHexDigit(escapedString[cX + 3])
                                        && IsHexDigit(escapedString[cX + 4]))
                                    {
                                        translated.Append((char)Convert.ToInt32(escapedString.Substring(cX + 1, 4), 16));
                                        cX += 4;
                                    }
                                    else
                                        goto default;
                                } break;
                            case 'U':
                                {
                                    if ((cX + 8 < escapedString.Length)
                                        && IsHexDigit(escapedString[cX + 1])
                                        && IsHexDigit(escapedString[cX + 2])
                                        && IsHexDigit(escapedString[cX + 3])
                                        && IsHexDigit(escapedString[cX + 4])
                                        && IsHexDigit(escapedString[cX + 5])
                                        && IsHexDigit(escapedString[cX + 6])
                                        && IsHexDigit(escapedString[cX + 7])
                                        && IsHexDigit(escapedString[cX + 8]))
                                    {
                                        translated.Append((char)Convert.ToInt32(escapedString.Substring(cX + 1, 8), 16));
                                        cX += 8;
                                    }
                                    else
                                        goto default;
                                } break;
                            // TODO: Named character entities?
                            default:
                                {
                                    if (IsOctalDigit(ctrlC))
                                    {
                                        int digitCount;
                                        if (((cX + 1) < escapedString.Length) && IsOctalDigit(escapedString[cX + 1]))
                                        {
                                            if (((cX + 2) < escapedString.Length) && IsOctalDigit(escapedString[cX + 2]))
                                                digitCount = 3;
                                            else
                                                digitCount = 2;
                                        }
                                        else
                                            digitCount = 1;

                                        translated.Append((char)Convert.ToInt32(escapedString.Substring(cX, digitCount), 8));
                                        cX += digitCount - 1;
                                    }
                                    else
                                    {
                                        translated.Append('\\');
                                        translated.Append(ctrlC);
                                    }
                                } break;
                        }
                    }
                    else
                        translated.Append(escapedString[cX]);
                }

                return translated.ToString();
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
                            matchX = haystack.LastIndexOf(TranslatedSearchStr, currentOffset, compMode);

                        found = (matchX != -1 && matchX > stopOffset);
                    } else {
                        currentOffset++;
                        if(currentOffset < haystack.Length)
                            matchX = haystack.IndexOf(TranslatedSearchStr, currentOffset, compMode);

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
                    searcher.SearchOptions ^= SearchFlags.Upward; // "Search upward" isn't necessary in bulk mode, and could cause subtle bugs.
                searcher.ResetSearch(false);

                var matches = new LinkedList<SearchResult>();
                string haystack = null;
                while ((haystack = searcher.NextMatch(haystack)) != null)
                {
                    if (replace)
                    {
                        var ed = WorkbenchLogic.Instance.OpenFile(searcher.currentFile, searcher.currentOffset) as EditorDocument;
                        ed.Editor.Document.Replace(searcher.currentOffset, TranslatedSearchStr.Length, TranslatedReplaceStr);
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
                pan.SearchString = replace? TranslatedReplaceStr : TranslatedSearchStr;
                pan.Results = matches;
                pan.Show();
            }
			#endregion
        }
	}
}
