// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

// Modified by A. Bothe

namespace ICSharpCode.AvalonEdit.AddIn
{
	/// <summary>
	/// Offsets and lengths of the bracket tokens within the bracket pair
	/// </summary>
	public class BracketSearchResult
	{
		public int OpeningBracketOffset { get; private set; }

		public int OpeningBracketLength { get; private set; }

		public int ClosingBracketOffset { get; private set; }

		public int ClosingBracketLength { get; private set; }

		public BracketSearchResult(int openingBracketOffset, int openingBracketLength,
								   int closingBracketOffset, int closingBracketLength)
		{
			this.OpeningBracketOffset = openingBracketOffset;
			this.OpeningBracketLength = openingBracketLength;
			this.ClosingBracketOffset = closingBracketOffset;
			this.ClosingBracketLength = closingBracketLength;
		}
	}
}
