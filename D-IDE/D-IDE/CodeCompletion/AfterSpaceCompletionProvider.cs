using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.NRefactory;

using D_Parser;
using ICSharpCode.NRefactory.Parser;

namespace D_IDE
{
	public class AfterSpaceCompletionProvider : ICompletionDataProvider
	{
		public ImageList icons;
		public DProject prj;
		string presel;
		int defIndex;

		public AfterSpaceCompletionProvider(ref DProject dprj, ImageList IconData)
		{
			presel = "";
			this.prj = dprj;
			this.icons = IconData;
			defIndex = 0;
		}

		#region ICompletionDataProvider Member

		int ICompletionDataProvider.DefaultIndex
		{
			get { return defIndex; }
		}

		ICompletionData[] ICompletionDataProvider.GenerateCompletionData(string fileName, TextArea ta, char charTyped)
		{
			List<ICompletionData> rl = new List<ICompletionData>();

			#region Compute backward keyword based on caret location
			char tch;
			string texpr = "";
			int prevToken = -1, curToken = -1;
			int off = ta.Caret.Offset;

			if(!Char.IsWhiteSpace(charTyped))
				curToken = DTokens.GetTokenID(charTyped.ToString());

			for(int i = off - 1; i > 0; i--)
			{
				tch = ta.Document.GetCharAt(i);

				if(char.IsLetterOrDigit(tch) || tch == '_') texpr += tch;
				else
				{
					if(texpr == "") break;
					texpr = DCodeCompletionProvider.ReverseString(texpr);
					prevToken = DKeywords.GetToken(texpr);
					off = i; // For later usage
					break;
				}
			}

			if(Char.IsWhiteSpace(charTyped)) curToken = prevToken;
			#endregion

			off = ExpressionFinder.SkipWhiteSpaceOffsets(ta.Document.TextContent,off);
			
			if(curToken > 0)
			{
				DModule pf = Form1.SelectedTabPage.fileData;
				CodeLocation tl = new CodeLocation(ta.Caret.Column + 1, ta.Caret.Line + 1);
				DataType seldt;
				presel = null;

				switch(curToken)
				{
					case DTokens.New:
						/*if(ta.Document.GetCharAt(off) != '=') return null;
						off--;
						off = ExpressionFinder.SkipWhiteSpaceOffsets(ta.Document.TextContent, off);
						string nameID="";
						for(int i = off; i > 0; i--)
						{
							tch = ta.Document.GetCharAt(i);

							if(char.IsLetterOrDigit(tch) || tch == '_')
							{
								nameID += tch;
							}
							else
							{
								break;
							}
						}
						if(nameID == "") return null;
						nameID=DCodeCompletionProvider.ReverseString(nameID);
						*/
						seldt = DCodeCompletionProvider.GetClassAt(pf.dom, tl);
						if(seldt != null)
						{
							DCodeCompletionProvider.AddAllClassMembers(seldt, ref rl, true, icons);
						}
						if(prj != null)
							foreach(DModule ppf in prj.files)
							{
								if(!ppf.IsParsable) continue;

								DCodeCompletionProvider.AddAllClassMembers(ppf.dom, ref rl, false, icons);
							}

						//DCodeCompletionProvider.AddGlobalSpaceContent(ref rl, icons);
						rl.AddRange(DCodeCompletionProvider.globalList);
						break;
					default:
						return null;
				}
			}

			return rl.ToArray();
		}

		ImageList ICompletionDataProvider.ImageList
		{
			get { return icons; }
		}

		/// <summary>
		/// Called when entry should be inserted. Forward to the insertion action of the completion data.
		/// </summary>
		public bool InsertAction(ICompletionData idata, TextArea ta, int off, char key)
		{
			int o = 0;

			for(int i = off - 1; i > 0; i--)
			{
				if(!char.IsLetterOrDigit(ta.MotherTextEditorControl.Text[i]))
				{
					o = i + 1;
					break;
				}
			}

			presel = idata.Text;
			ta.Document.Replace(o, off - o, idata.Text);
			ta.Caret.Column = o + idata.Text.Length;
			return true;
		}

		string ICompletionDataProvider.PreSelection
		{
			get { return presel; }
		}

		public CompletionDataProviderKeyResult ProcessKey(char key)
		{
			if(char.IsLetterOrDigit(key) || key == '_')
				return CompletionDataProviderKeyResult.NormalKey;
			else
				return CompletionDataProviderKeyResult.InsertionKey;
		}

		#endregion
	}
}
