using System.Collections.Generic;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using D_Parser.Resolver;

namespace D_Parser.Completion
{
	public abstract class AbstractCompletionProvider
	{
		public readonly ICompletionDataGenerator CompletionDataGenerator;
		
		public AbstractCompletionProvider(ICompletionDataGenerator CompletionDataGenerator)
		{
			this.CompletionDataGenerator = CompletionDataGenerator;
		}

		public static AbstractCompletionProvider Create(ICompletionDataGenerator dataGen, string EnteredText)
		{
			if (CtrlSpaceCompletionProvider.CompletesEnteredText(EnteredText))
				return new CtrlSpaceCompletionProvider(dataGen);

			if (MemberCompletionProvider.CompletesEnteredText(EnteredText))
				return new MemberCompletionProvider(dataGen);

			if (PropertyAttributeCompletionProvider.CompletesEnteredText(EnteredText))
				return new PropertyAttributeCompletionProvider(dataGen);

			return null;
		}

		public static AbstractCompletionProvider BuildCompletionData(ICompletionDataGenerator dataGen, IEditorData editor, string EnteredText)
		{
			var provider = Create(dataGen, EnteredText);

			if (provider != null)
				provider.BuildCompletionData(editor, EnteredText);

			return provider;
		}

		#region Helper Methods
		public static bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public static bool CanItemBeShownGenerally(INode dn)
		{
			if (dn == null || string.IsNullOrEmpty(dn.Name))
				return false;

			if (dn is DMethod)
			{
				var dm = dn as DMethod;

				if (dm.SpecialType == DMethod.MethodType.Unittest ||
					dm.SpecialType == DMethod.MethodType.Destructor ||
					dm.SpecialType == DMethod.MethodType.Constructor)
					return false;
			}

			return true;
		}

		public static bool HaveSameAncestors(INode higherLeveledNode, INode lowerLeveledNode)
		{
			var curPar = higherLeveledNode;

			while (curPar != null)
			{
				if (curPar == lowerLeveledNode)
					return true;

				curPar = curPar.Parent;
			}
			return false;
		}

		public static bool IsTypeNode(INode n)
		{
			return n is DEnum || n is DClassLike;
		}

		/// <summary>
		/// Returns C:\fx\a\b when PhysicalFileName was "C:\fx\a\b\c\Module.d" , ModuleName= "a.b.c.Module" and WantedDirectory= "a.b"
		/// 
		/// Used when formatting package names in BuildCompletionData();
		/// </summary>
		public static string GetModulePath(string PhysicalFileName, string ModuleName, string WantedDirectory)
		{
			return GetModulePath(PhysicalFileName, ModuleName.Split('.').Length, WantedDirectory.Split('.').Length);
		}

		public static string GetModulePath(string PhysicalFileName, int ModuleNamePartAmount, int WantedDirectoryNamePartAmount)
		{
			var ret = "";

			var physFileNameParts = PhysicalFileName.Split('\\');
			for (int i = 0; i < physFileNameParts.Length - ModuleNamePartAmount + WantedDirectoryNamePartAmount; i++)
				ret += physFileNameParts[i] + "\\";

			return ret.TrimEnd('\\');
		}
		#endregion

		static bool IsCompletionAllowed(IEditorData Editor, string EnteredText)
		{
			// If typing a begun identifier, return immediately
			if ((EnteredText != null && 
				EnteredText.Length > 0 ? IsIdentifierChar(EnteredText[0]) : true) &&
				Editor.CaretOffset > 0 &&
				IsIdentifierChar(Editor.ModuleCode[Editor.CaretOffset - 1]))
				return false;

			if (CaretContextAnalyzer.IsInCommentAreaOrString(Editor.ModuleCode, Editor.CaretOffset))
				return false;

			return true;
		}

		protected abstract void BuildCompletionDataInternal(IEditorData Editor, string EnteredText);

		public void BuildCompletionData(IEditorData Editor,
			string EnteredText)
		{
			if(!IsCompletionAllowed(Editor, EnteredText))
				return;

			BuildCompletionDataInternal(Editor, EnteredText);
		}
	}
}
