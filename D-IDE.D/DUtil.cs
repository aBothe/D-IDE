using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using D_Parser.Core;

namespace D_IDE.D
{	
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
