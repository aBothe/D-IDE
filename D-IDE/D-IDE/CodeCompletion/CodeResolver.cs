using System;
using System.Collections.Generic;
using System.Text;
using D_Parser;
using System.IO;

namespace D_IDE.CodeCompletion
{
    public class CodeResolver
    {
        public static TypeDeclaration BuildIdentifierList(string Text, int CaretOffset, bool BackwardOnly)
        {
            // At first we only want to find the beginning of our identifier list
            // later we will pass the text beyond the beginning to the parser - there we parse all needed expressions from it
            int IdentListStart = -1;

            /*
            T!(...)>.<
             */

            int isComment = 0;
            bool isString = false, expectDot=false, hadDot=true;
            var bracketStack = new Stack<char>();
            bool stopSeeking=false;

            for (int i = CaretOffset; i >=0 && !stopSeeking; i--)
            {
                IdentListStart = i;
                var c = Text[i];
                var str = Text.Substring(i);
                char p=' ';
                if (i > 0) p = Text[i - 1];

                // Primitive comment check
                if(!isString && c=='/' && (p=='*' || p=='+'))
                    isComment++;
                if (!isString && isComment > 0 && (c == '+' || c == '*') && p == '/')
                    isComment--;

                // Primitive string check
                //TODO: "blah">.<
                if (isComment<1 && c == '"' && p!='\\')
                    isString = !isString;

                // If string or comment, just continue
                if (isString || isComment>0)
                    continue;

                // If between brackets, skip
                if (bracketStack.Count > 0 && c!=bracketStack.Peek())
                    continue;

                // Bracket check
                switch (c)
                {
                    case ']':
                        bracketStack.Push('[');
                        continue;
                    case ')':
                        bracketStack.Push('(');
                        continue;
                    case '}':
                        bracketStack.Push('{');
                        continue;

                    case '[':
                    case '(':
                    case '{':
                        if (bracketStack.Count>0 && bracketStack.Peek() == c){
                            bracketStack.Pop();
                            if(p=='!') // Skip template stuff
                                i--;
                        }else
                        {
                            // Stop if we reached the most left existing bracket
                            // e.g. foo>(< bar| )
                            stopSeeking = true;
                            IdentListStart++;
                        }
                        continue;
                }

                // whitespace check
                if (Char.IsWhiteSpace(c)) { if (hadDot) expectDot = false; else expectDot = true; continue; }

                if (c == '.')
                {
                    expectDot=false;
                    hadDot = true;
                    continue;
                }

                /*
                 * abc
                 * abc . abc
                 * T!().abc[]
                 * def abc.T
                 */
                if (Char.IsLetterOrDigit(c) || c == '_')
                {
                    hadDot = false;

                    if (!expectDot)
                        continue;
                    else
                        IdentListStart++;
                }
                
                stopSeeking = true;
            }


            // Part 2: Init the parser
            if (!stopSeeking || IdentListStart < 0) 
                return null;

            // If code e.g. starts with a bracket, increment IdentListStart
            var ch = Text[IdentListStart];
            if (!Char.IsLetterOrDigit(ch) && ch != '_' && ch!='.')
                IdentListStart++;

            var psr = DParser.ParseBasicType(BackwardOnly?Text.Substring(IdentListStart,CaretOffset-IdentListStart): Text.Substring(IdentListStart));
            return psr;
        }
    }
}
