using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Parser;
using D_Parser.Resolver;
using D_Parser.Dom.Statements;
using D_Parser.Dom.Expressions;

namespace D_Parser.Completion
{
	public class CtrlSpaceCompletionProvider : AbstractCompletionProvider
	{
		public CtrlSpaceCompletionProvider(ICompletionDataGenerator cdg) : base(cdg) { }

		public static bool CompletesEnteredText(string EnteredText)
		{
			return string.IsNullOrWhiteSpace(EnteredText) ||
				IsIdentifierChar(EnteredText[0]);
		}

		protected override void BuildCompletionDataInternal(IEditorData Editor, string EnteredText)
		{
			IEnumerable<INode> listedItems = null;
			ParserTrackerVariables trackVars = null;
			var visibleMembers = DResolver.MemberTypes.All;

			IStatement curStmt = null;
			var curBlock = DResolver.SearchBlockAt(Editor.SyntaxTree, Editor.CaretLocation, out curStmt);

			if (curBlock == null)
				return;

			// 1) Get current context the caret is at

			var parsedBlock = DResolver.FindCurrentCaretContext(
				Editor.ModuleCode,
				curBlock,
				Editor.CaretOffset,
				Editor.CaretLocation,
				out trackVars);

			// 2) If in declaration and if node identifier is expected, do not show any data
			if (trackVars == null)
			{
				// --> Happens if no actual declaration syntax given --> Show types/imports/keywords anyway
				visibleMembers = DResolver.MemberTypes.Imports | DResolver.MemberTypes.Types | DResolver.MemberTypes.Keywords;

				listedItems = DResolver.EnumAllAvailableMembers(curBlock, null, Editor.CaretLocation, Editor.ParseCache, visibleMembers);
			}
			else
			{
				if (trackVars.LastParsedObject is INode &&
					string.IsNullOrEmpty((trackVars.LastParsedObject as INode).Name) &&
					trackVars.ExpectingIdentifier)
					return;

				if (trackVars.LastParsedObject is TokenExpression &&
					DTokens.BasicTypes[(trackVars.LastParsedObject as TokenExpression).Token] &&
					!string.IsNullOrEmpty(EnteredText) &&
					IsIdentifierChar(EnteredText[0]))
					return;

				if (trackVars.LastParsedObject is DAttribute)
				{
					var attr = trackVars.LastParsedObject as DAttribute;

					if (attr.IsStorageClass && attr.Token != DTokens.Abstract)
						return;
				}

				if (trackVars.LastParsedObject is ImportStatement /*&& !CaretAfterLastParsedObject*/)
					visibleMembers = DResolver.MemberTypes.Imports;
				else if (trackVars.LastParsedObject is NewExpression && (trackVars.IsParsingInitializer/* || !CaretAfterLastParsedObject*/))
					visibleMembers = DResolver.MemberTypes.Imports | DResolver.MemberTypes.Types;
				else if (EnteredText == " ")
					return;
				// In class bodies, do not show variables
				else if (!(parsedBlock is BlockStatement || trackVars.IsParsingInitializer))
					visibleMembers = DResolver.MemberTypes.Imports | DResolver.MemberTypes.Types | DResolver.MemberTypes.Keywords;

				// In a method, parse from the method's start until the actual caret position to get an updated insight
				if (visibleMembers.HasFlag(DResolver.MemberTypes.Variables) && curBlock is DMethod)
				{
					if (parsedBlock is BlockStatement)
					{
						var blockStmt = parsedBlock as BlockStatement;

						// Insert the updated locals insight.
						// Do not take the caret location anymore because of the limited parsing of our code.
						var scopedStmt = blockStmt.SearchStatementDeeply(blockStmt.EndLocation /*Editor.CaretLocation*/);

						var decls = BlockStatement.GetItemHierarchy(scopedStmt, Editor.CaretLocation);

						foreach (var n in decls)
							CompletionDataGenerator.Add(n);
					}
				}

				if (visibleMembers != DResolver.MemberTypes.Imports) // Do not pass the curStmt because we already inserted all updated locals a few lines before!
					listedItems = DResolver.EnumAllAvailableMembers(curBlock, null/*, curStmt*/, Editor.CaretLocation, Editor.ParseCache, visibleMembers);
			}

			// Add all found items to the referenced list
			if (listedItems != null)
				foreach (var i in listedItems)
				{
					if (CanItemBeShownGenerally(i))
						CompletionDataGenerator.Add(i);
				}

			//TODO: Split the keywords into such that are allowed within block statements and non-block statements
			// Insert typable keywords
			if (visibleMembers.HasFlag(DResolver.MemberTypes.Keywords))
				foreach (var kv in DTokens.Keywords)
					CompletionDataGenerator.Add(kv.Key);

			else if (visibleMembers.HasFlag(DResolver.MemberTypes.Types))
				foreach (var kv in DTokens.BasicTypes_Array)
					CompletionDataGenerator.Add(kv);

			#region Add module name stubs of importable modules
			if (visibleMembers.HasFlag(DResolver.MemberTypes.Imports))
			{
				var nameStubs = new Dictionary<string, string>();
				var availModules = new List<IAbstractSyntaxTree>();
				foreach (var mod in Editor.ParseCache)
				{
					if (string.IsNullOrEmpty(mod.ModuleName))
						continue;

					var parts = mod.ModuleName.Split('.');

					if (!nameStubs.ContainsKey(parts[0]) && !availModules.Contains(mod))
					{
						if (parts[0] == mod.ModuleName)
							availModules.Add(mod);
						else
							nameStubs.Add(parts[0], GetModulePath(mod.FileName, parts.Length, 1));
					}
				}

				foreach (var kv in nameStubs)
					CompletionDataGenerator.Add(kv.Key, PathOverride: kv.Value);

				foreach (var mod in availModules)
					CompletionDataGenerator.Add(mod.ModuleName, mod);
			}
			#endregion
		}
	}
}
