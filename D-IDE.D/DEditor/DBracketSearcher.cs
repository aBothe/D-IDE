using D_Parser.Completion;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.Document;

namespace D_IDE.D.DEditor
{
	public class DBracketSearcher
	{
		public static BracketSearchResult SearchBrackets(TextDocument doc, IEditorData ed, IBlockNode curBlock, IStatement stmt)
		{
			while (stmt != null)
			{
				if (stmt is IExpressionContainingStatement)
				{
					var ecs = (IExpressionContainingStatement)stmt;
					foreach (var x in ecs.SubExpressions)
					{
						SurroundingParenthesesExpression spe = null;
						var xx = x;
						while (xx is ContainerExpression)
						{
							if (xx is SurroundingParenthesesExpression)
								spe = (SurroundingParenthesesExpression)xx;

							var subX = ((ContainerExpression)xx).SubExpressions;
							if (subX != null && subX.Length != 0)
							{
								xx = null;
								foreach (var sx in subX)
									if (ed.CaretLocation > sx.Location && ed.CaretLocation < sx.EndLocation)
									{
										xx = sx as ContainerExpression;
										break;
									}
							}
						}

						if (spe != null)
							return new BracketSearchResult(
								doc.GetOffset(spe.Location.Line, spe.Location.Column), 1,
								doc.GetOffset(spe.EndLocation.Line, spe.EndLocation.Column) - 1, 1);
					}
				}

				if (stmt is BlockStatement)
					return new BracketSearchResult(
						doc.GetOffset(stmt.Location.Line, stmt.Location.Column), 1,
						doc.GetOffset(stmt.EndLocation.Line, stmt.EndLocation.Column) - 1, 1);
				stmt = stmt.Parent;
			}

			if (curBlock != null && ed.CaretLocation < curBlock.BlockStartLocation)
				curBlock = curBlock.Parent as IBlockNode;

			if (curBlock == null || curBlock is IAbstractSyntaxTree)
				return null;

			//TODO: Meta blocks, everything that could contain parentheses
			return new BracketSearchResult(
				doc.GetOffset(curBlock.BlockStartLocation.Line, curBlock.BlockStartLocation.Column), 1,
				doc.GetOffset(curBlock.EndLocation.Line, curBlock.EndLocation.Column) - 1, 1);
		}
	}
}
