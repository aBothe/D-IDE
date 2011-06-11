﻿/// <summary>
/// The following code was taken from SharpDevelop
/// </summary>

using System;
using D_Parser.Dom;

namespace D_Parser.Parser
{
    public enum LiteralFormat : byte
    {
        None,
        Scalar,
        StringLiteral,
        VerbatimStringLiteral,
        CharLiteral,
    }

    public class DToken
    {
        internal readonly int col;
        internal readonly int line;

        internal readonly LiteralFormat literalFormat;
        internal readonly object literalValue;
        internal readonly string val;
        internal DToken next;
        readonly CodeLocation endLocation;

        public readonly int Kind;

        public LiteralFormat LiteralFormat
        {
            get { return literalFormat; }
        }

        public object LiteralValue
        {
            get { return literalValue; }
        }

        public string Value
        {
            get { return val; }
        }

        public DToken Next
        {
            get { return next; }
        }

        public CodeLocation EndLocation
        {
            get { return endLocation; }
        }

        public CodeLocation Location
        {
            get
            {
                return new CodeLocation(col, line);
            }
        }

        public override string ToString()
        {
            if (Kind == DTokens.Identifier || Kind == DTokens.Literal)
                return val;
            return DTokens.GetTokenString(Kind);
        }

        public DToken(DToken t)
            : this(t.Kind, t.col, t.line, t.val, t.literalValue, t.LiteralFormat)
        {
            next = t.next;
        }
        //public DToken(int kind):base(kind)	{}
        public DToken(int kind, int col, int line) : this(kind, col, line, null) { }
        public DToken(int kind, int col, int line, string val)
        {
            this.Kind = kind;
            this.col = col;
            this.line = line;
            this.val = val;
            this.endLocation = new CodeLocation(col + (val == null ? 1 : val.Length), line);
        }
        public DToken(int kind, int x, int y, string val, object literalValue, LiteralFormat literalFormat)
            : this(kind, new CodeLocation(x, y), new CodeLocation(x + val.Length, y), val, literalValue, literalFormat)
        {
        }

        public DToken(int kind, CodeLocation startLocation, CodeLocation endLocation, string val, object literalValue, LiteralFormat literalFormat)
        {
            this.Kind = kind;
            this.col = startLocation.Column;
            this.line = startLocation.Line;
            this.endLocation = endLocation;
            this.val = val;
            this.literalValue = literalValue;
            this.literalFormat = literalFormat;
        }
    }

    public class Comment
    {
        public enum Type
        {
            Block,
            SingleLine,
            Documentation
        }

        public Type CommentType;
        public string CommentText;
        public CodeLocation StartPosition;
        public CodeLocation EndPosition;

        /// <value>
        /// Is true, when the comment is at line start or only whitespaces
        /// between line and comment start.
        /// </value>
        public bool CommentStartsLine;

        public Comment(Type commentType, string comment, bool commentStartsLine, CodeLocation startPosition, CodeLocation endPosition)
        {
            this.CommentType = commentType;
            this.CommentText = comment;
            this.CommentStartsLine = commentStartsLine;
            this.StartPosition = startPosition;
            this.EndPosition = endPosition;
        }
    }
}