using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Gui.InsightWindow;
using ICSharpCode.TextEditor;
using ICSharpCode.NRefactory;
using D_Parser;
using System.Windows.Forms;
using D_IDE.CodeCompletion;

namespace D_IDE
{
	class InsightWindowProvider : IInsightDataProvider
	{
		public List<string> data;
		DocumentInstanceWindow diw;
		CompilerConfiguration cc=D_IDE_Properties.Default.dmd2;
		int initialOffset = 0;
		char key;

		public InsightWindowProvider(DocumentInstanceWindow instWin, char keyPressed)
		{
			key = keyPressed;
			diw = instWin;
			data = new List<string>();
			if (instWin.OwnerProject != null) cc = instWin.OwnerProject.Compiler;
		}

		#region IInsightDataProvider Member

		bool IInsightDataProvider.CaretOffsetChanged()
		{
			bool closeDataProvider = diw.txt.ActiveTextAreaControl.Caret.Offset <= initialOffset;
			int brackets = 0;
			int curlyBrackets = 0;
			if (!closeDataProvider)
			{
				bool insideChar = false;
				bool insideString = false;
				for (int offset = initialOffset; offset < Math.Min(diw.txt.ActiveTextAreaControl.Caret.Offset, diw.txt.Document.TextLength); ++offset)
				{
					char ch = diw.txt.Document.GetCharAt(offset);
					switch (ch)
					{
						case '\'':
							insideChar = !insideChar;
							break;
						case '(':
							if (!(insideChar || insideString))
							{
								++brackets;
							}
							break;
						case ')':
							if (!(insideChar || insideString))
							{
								--brackets;
							}
							if (brackets <= 0)
							{
								return true;
							}
							break;
						case '"':
							insideString = !insideString;
							break;
						case '}':
							if (!(insideChar || insideString))
							{
								--curlyBrackets;
							}
							if (curlyBrackets < 0)
							{
								return true;
							}
							break;
						case '{':
							if (!(insideChar || insideString))
							{
								++curlyBrackets;
							}
							break;
						case ';':
							if (!(insideChar || insideString))
							{
								return true;
							}
							break;
					}
				}
			}
			return closeDataProvider;
		}

		int IInsightDataProvider.DefaultIndex
		{
			get { return 0; }
		}

		string IInsightDataProvider.GetInsightData(int number)
		{
			return data[number];
		}

		int IInsightDataProvider.InsightDataCount
		{
			get { return data.Count; }
		}

		void IInsightDataProvider.SetupDataProvider(string fileName, TextArea ta)
		{
			try
			{
				initialOffset = ta.Caret.Offset;

				if (key == ',')
				{
					char tch;
					int psb = 0;
					for (int off = initialOffset; off > 0; off--)
					{
						tch = ta.Document.GetCharAt(off);

						if (tch == ')' || tch == '}' || tch == ']') psb++;
						if (tch == '(' || tch == '{' || tch == '[') psb--;

						if (psb < 1 && tch == '(')
						{
							initialOffset = off;
							break;
						}
					}
				}

                var Matches = D_IDECodeResolver.ResolveTypeDeclarations(diw.Module, ta, ta.Document.OffsetToPosition(initialOffset-1));
                if (Matches != null && Matches.Length > 0)
                    foreach (var m in Matches)
                        data.Add(m.ToString()+ m.Description!=null?("\n"+m.Description):"");
			}
			catch (Exception ex) { D_IDEForm.thisForm.Log(ex.Message+ " ("+ex.Source+")"); }
		}

		#endregion
	}

}
