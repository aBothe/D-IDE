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
            if (String.IsNullOrEmpty(Text) || CaretOffset >= Text.Length) return null;
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

        public static DBlockStatement SearchBlockAt(DBlockStatement Parent, CodeLocation Where)
        {
            foreach (var n in Parent)
            {
                if (!(n is DBlockStatement)) continue;

                var b = n as DBlockStatement;
                if (Where > b.BlockStartLocation && Where < b.EndLocation)
                    return SearchBlockAt(b, Where);
            }

            return Parent;
        }

        /// <summary>
        /// Finds the location (module node) where a type (TypeExpression) has been declared.
        /// </summary>
        /// <param name="Module"></param>
        /// <param name="IdentifierList"></param>
        /// <returns>When a type was found, the declaration entry will be returned. Otherwise, it'll return null.</returns>
        public static DNode ResolveTypeDeclaration(DBlockStatement BlockNode, TypeDeclaration IdentifierList)
        {
            if (BlockNode == null || IdentifierList == null) return null;

            // Modules    Declaration
            // |---------|-----|
            // std.stdio.writeln();
            if (IdentifierList is IdentifierList)
            {
                var il = IdentifierList as IdentifierList;
                var skippedIds = 0;

                // Now search the entire block
                var istr = il.ToString();

                var mod = BlockNode.NodeRoot as DModule;
                /* If the id list start with the name of BlockNode's root module, 
                 * skip those first identifiers to proceed seeking the rest of the list
                 */
                if (mod != null && istr.StartsWith(mod.ModuleName))
                {
                    skippedIds += mod.ModuleName.Split('.').Length;
                    istr = il.ToString(skippedIds);
                }

                // Now move stepwise deeper calling ResolveTypeDeclaration recursively
                DNode currentNode = BlockNode;
                while (skippedIds < il.Parts.Count && currentNode is DBlockStatement)
                {
                    // As long as our node can contain other nodes, scan it
                    currentNode = ResolveTypeDeclaration(currentNode as DBlockStatement, il[skippedIds]);
                    skippedIds++;
                }
                return currentNode;
            }

            if (IdentifierList is NormalDeclaration)
            {
                var nameIdent = IdentifierList as NormalDeclaration;

                // Scan from the inner to the outer level
                var currentParent = BlockNode;
                while (currentParent != null)
                {
                    foreach (var ch in currentParent)
                    {
                        if (nameIdent.Name == ch.Name)
                            return ch;
                    }
                    currentParent = currentParent.Parent as DBlockStatement;
                }
            }

            if (IdentifierList is TemplateDecl)
            {
                var template = IdentifierList as TemplateDecl;
                
            }

            return null;
        }
    }
}
