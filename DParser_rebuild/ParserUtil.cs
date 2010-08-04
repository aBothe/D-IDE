/// <summary>
/// The following code was taken from SharpDevelop
/// </summary>

using System;
namespace D_Parser
{
    /// <summary>
    /// A line/column position.
    /// NRefactory lines/columns are counting from one.
    /// </summary>
    public struct Location : IComparable<Location>, IEquatable<Location>
    {
        public static readonly Location Empty = new Location(-1, -1);

        public Location(int column, int line)
        {
            x = column;
            y = line;
        }

        int x, y;

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Line
        {
            get { return y; }
            set { y = value; }
        }

        public int Column
        {
            get { return x; }
            set { x = value; }
        }

        public bool IsEmpty
        {
            get
            {
                return x <= 0 && y <= 0;
            }
        }

        public override string ToString()
        {
            return string.Format("(Line {1}, Col {0})", this.x, this.y);
        }

        public override int GetHashCode()
        {
            return unchecked(87 * x.GetHashCode() ^ y.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Location)) return false;
            return (Location)obj == this;
        }

        public bool Equals(Location other)
        {
            return this == other;
        }

        public static bool operator ==(Location a, Location b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Location a, Location b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static bool operator <(Location a, Location b)
        {
            if (a.y < b.y)
                return true;
            else if (a.y == b.y)
                return a.x < b.x;
            else
                return false;
        }

        public static bool operator >(Location a, Location b)
        {
            if (a.y > b.y)
                return true;
            else if (a.y == b.y)
                return a.x > b.x;
            else
                return false;
        }

        public static bool operator <=(Location a, Location b)
        {
            return !(a > b);
        }

        public static bool operator >=(Location a, Location b)
        {
            return !(a < b);
        }

        public int CompareTo(Location other)
        {
            if (this == other)
                return 0;
            if (this < other)
                return -1;
            else
                return 1;
        }
    }

    public enum LiteralFormat : byte
    {
        None,
        DecimalNumber,
        HexadecimalNumber,
        OctalNumber,
        StringLiteral,
        VerbatimStringLiteral,
        CharLiteral,
        DateTimeLiteral
    }

    public class DToken
    {
        internal readonly int kind;

        internal readonly int col;
        internal readonly int line;

        internal readonly LiteralFormat literalFormat;
        internal readonly object literalValue;
        internal readonly string val;
        internal DToken next;
        readonly Location endLocation;

        public int Kind
        {
            get { return kind; }
        }

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

        public Location EndLocation
        {
            get { return endLocation; }
        }

        public Location Location
        {
            get
            {
                return new Location(col, line);
            }
        }

        public override string ToString()
        {
            if (kind == DTokens.Identifier || kind == DTokens.Literal)
                return val;
            return DTokens.GetTokenString(kind);
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
            this.kind = kind;
            this.col = col;
            this.line = line;
            this.val = val;
            this.endLocation = new Location(col + (val == null ? 1 : val.Length), line);
        }
        public DToken(int kind, int x, int y, string val, object literalValue, LiteralFormat literalFormat)
            : this(kind, new Location(x, y), new Location(x + val.Length, y), val, literalValue, literalFormat)
        {
        }

        public DToken(int kind, Location startLocation, Location endLocation, string val, object literalValue, LiteralFormat literalFormat)
        {
            this.kind = kind;
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
        public Location StartPosition;
        public Location EndPosition;

        /// <value>
        /// Is true, when the comment is at line start or only whitespaces
        /// between line and comment start.
        /// </value>
        public bool CommentStartsLine;

        public Comment(Type commentType, string comment, bool commentStartsLine, Location startPosition, Location endPosition)
        {
            this.CommentType = commentType;
            this.CommentText = comment;
            this.CommentStartsLine = commentStartsLine;
            this.StartPosition = startPosition;
            this.EndPosition = endPosition;
        }
    }
}