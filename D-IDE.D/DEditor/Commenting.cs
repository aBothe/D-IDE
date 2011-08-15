using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Code by Dima Dimov
 * 
 */

namespace D_IDE.D
{
    public class Commenting
    {
		public static string comment(string p, int selectionStart, int selectionEnd)
		{
			if ((selectionStart >= 0 && selectionStart <= selectionEnd) && selectionEnd <= p.Length)
			{
				if (selectionEnd <= p.Length - 1 && selectionStart == selectionEnd)    //if user selectionStart and selectionEnd are same than put // at selectionStart
				{
					p = p.Insert(selectionStart, "//");
				}
				else if (selectionEnd - selectionStart >= 1)
				{
					bool isOBlock = false;
					bool isCBlock = false;
					bool isNOBlock = false;
					bool isNCBlock = false;
					bool allow = false;
					/*
					 * Rule 1:There can only be 1 block comment defined as / * * /
					 * Rule 2:/++/ cannot capsulate / ** /
					 * Rule 3:/+ +/ can be put only after / * and before * / and only after or before /+ and +/ 
					 */

					for (int i = 0; i < p.Length; i++)
					{
						if (i <= p.Length - 2 && p.Substring(i, 2) == "/+")
						{
							isNOBlock = true;
							i++;
						}
						if (i <= p.Length - 2 && p.Substring(i, 2) == "+/")
						{
							isNCBlock = true;

							if (i + 1 <= p.Length - 1 && (!isOBlock && !isCBlock))
							{
								if (selectionStart < i)
								{
									allow = false;
								}
								else if (selectionStart == i)
								{
									allow = true;
								}
								break;
							}
							i++;
						}
						if (i <= p.Length - 2 && p.Substring(i, 2) == "/*")
						{
							isOBlock = true;
						}
						if (isOBlock)
						{
							if (i <= p.Length - 2 && p.Substring(i, 2) == "*/")
							{
								if ((isOBlock && isCBlock) || isNOBlock)
								{
									allow = false;
								}
								else
								{
									isCBlock = true;
									allow = true;
								}
							}
							else
							{
								allow = false;
							}
							break;
						}
					}
					if (allow)
					{
						p = p.Insert(selectionStart, "/*").Insert(selectionEnd - selectionStart + 1 + 2, "*/");
						return p;
					}
					else if (!allow)
					{
						if (selectionStart + 1 < p.Length - 1 && ((p[selectionStart] == '/' && p[selectionStart + 1] == '*') ||
							(p[selectionStart] == '/' && p[selectionStart + 1] == '+')))
						{
							selectionStart += 2; //"/*" -> "/*|"
							if (selectionEnd <= p.Length - 2)
							{
								selectionEnd = selectionEnd + selectionStart + 1;
							}
						}
						else if ((!isOBlock && !isCBlock) && selectionEnd - selectionStart == selectionEnd)
						{
							selectionEnd += 2;
						}
						p = p.Insert(selectionStart, "/+").Insert(selectionEnd, "+/");
					}
				}
			}
			return p;
		}
    }
}