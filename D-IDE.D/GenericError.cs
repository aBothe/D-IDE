using D_IDE.Core;
using D_Parser.Dom;

namespace D_IDE.D
{
	public class DSemanticError : GenericError
	{
		bool isSemantic = false;
		public bool IsSemantic
		{
			get { return isSemantic; }
			set { Type = value ? ErrorType.Warning : ErrorType.Error; isSemantic = value; }
		}

		public CodeLocation Location
		{
			set { Line = value.Line; Column = value.Column; }

			get { return new CodeLocation(Column, Line); }
		}
	}

	public class DParseError : GenericError
	{
		public readonly ParserError ParserError;

		public DParseError(ParserError err)
		{
			this.ParserError = err;

			if (err.IsSemantic)
				Type = ErrorType.Warning;
		}

		public override string Message
		{
			get { return ParserError.Message; }
		}

		public bool IsSemantic
		{
			get { return ParserError.IsSemantic; }
		}

		public override int Column
		{
			get
			{
				return ParserError.Location.Column;
			}
			set { }
		}

		public override int Line
		{
			get
			{
				return ParserError.Location.Line;
			}
			set { }
		}
	}
}
