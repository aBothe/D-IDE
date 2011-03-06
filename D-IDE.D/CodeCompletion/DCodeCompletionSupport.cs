using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Parser.Core;
using D_Parser;
using D_IDE.D.CodeCompletion;

namespace D_IDE.D
{
	public class DCodeCompletionSupport
	{
		public static DCodeCompletionSupport Instance = new DCodeCompletionSupport();

		public bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public bool CanShowCompletionWindow(DEditorDocument EditorDocument)
		{
			return !DCodeResolver.Commenting.IsInCommentAreaOrString(EditorDocument.Editor.Text, EditorDocument.Editor.CaretOffset);
		}

		public void BuildCompletionData(DEditorDocument EditorDocument, IList<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> l, string EnteredText)
		{
			var caretOffset = EditorDocument.Editor.CaretOffset;
			var caretLocation = new CodeLocation(EditorDocument.Editor.TextArea.Caret.Column,EditorDocument.Editor.TextArea.Caret.Line);

			var curBlock = DCodeResolver.SearchBlockAt(EditorDocument.SyntaxTree,caretLocation);

			if (curBlock == null)
				return;

			IEnumerable<INode> listedItems = null;
			var codeCache = EnumAvailableModules(EditorDocument);

			// Usually shows variable members
			if (EnteredText == ".")
			{
				//l.Add(new DCompletionData( new DVariable() { Name="myVar", Description="A description for myVar"}));
			}

			// Enum all nodes that can be accessed in the current scope
			else if(string.IsNullOrEmpty(EnteredText) || IsIdentifierChar(EnteredText[0]))
			{
				listedItems = DCodeResolver.EnumAllAvailableMembers(curBlock, codeCache);

				//TODO: Add D keywords including their descriptions

				// Add module name stubs of importable modules
				var nameStubs=new List<string>();
				foreach (var mod in codeCache)
				{
					if (string.IsNullOrEmpty(mod.ModuleName))
						continue;
					var firstPart = mod.ModuleName.Split('.')[0];

					if (!nameStubs.Contains(firstPart))
						nameStubs.Add(firstPart);
				}

				foreach (var name in nameStubs)
					l.Add(new NamespaceCompletionData(name));
			}

			// Add all found items to the referenced list
			if(listedItems!=null)
				foreach(var i in listedItems)
					l.Add(new DCompletionData(i));
		}

		public void BuildToolTip(DEditorDocument EditorDocument, ToolTipRequestArgs ToolTipRequest)
		{
			int offset = EditorDocument.Editor.Document.GetOffset(ToolTipRequest.Line, ToolTipRequest.Column);

			if (!ToolTipRequest.InDocument||
					DCodeResolver.Commenting.IsInCommentAreaOrString(EditorDocument.Editor.Text,offset)) 
				return;

			try
			{
				var types = DCodeResolver.ResolveTypeDeclarations(
					EditorDocument.SyntaxTree, 
					EditorDocument.Editor.Text,
					offset, 
					new CodeLocation(ToolTipRequest.Column, ToolTipRequest.Line),
					true,
					EnumAvailableModules(EditorDocument) // std.cstream.din.getc(); <<-- It's resolvable but not imported explictily! So also scan the global cache!
					//DCodeResolver.ResolveImports(EditorDocument.SyntaxTree,EnumAvailableModules(EditorDocument))
					);

				string tt = "";

				//TODO: Build well-formatted tool tip string/ Do a better tool tip layout
				if (types != null)
					foreach (var n in types)
						tt += (n.Type!=null?(n.Type.ToString() + " "):"") + n.Name + "\r\n";

				tt = tt.Trim();
				if(!string.IsNullOrEmpty(tt))
					ToolTipRequest.ToolTipContent = tt;
			}catch{}
		}

		public bool IsInsightWindowTrigger(char key)
		{
			return key == '(' || key==',';
		}

		#region Module enumeration helper
		public IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DEditorDocument Editor)
		{
			return EnumAvailableModules(Editor.HasProject ? Editor.Project as DProject : null);
		}

		public IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DProject Project)
		{
			var ret =new List<IAbstractSyntaxTree>();

			if (Project != null)
			{
				// Add all parsed global modules that belong to the project's compiler configuration
				foreach (var astColl in Project.CompilerConfiguration.ASTCache)
					ret.AddRange(astColl);

				// Add all modules that exist in the current solution.
				if (Project.Solution != null)
				{
					foreach (var prj in Project.Solution)
						if (prj is DProject)
							ret.AddRange((prj as DProject).ParsedModules);
				}
				else // If no solution present, add scanned modules of our current project only
					ret.AddRange(Project.ParsedModules);
			}
			else // If no project present, only add the modules of the default compiler configuration
				foreach (var astColl in DSettings.Instance.DMDConfig().ASTCache)
					ret.AddRange(astColl);

			return ret;
		}
		#endregion
	}

	public class NamespaceCompletionData : ICompletionData
	{
		public string ModuleName{get;set;}

		public NamespaceCompletionData(string ModuleName)
		{
			this.ModuleName=ModuleName;
		}

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get { return null; }
		}

		public System.Windows.Media.ImageSource Image
		{
			get { return null; }
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return ModuleName; }
		}
	}

	public class DCompletionData : ICompletionData
	{
		public DCompletionData(INode n)
		{
			Node = n;
		}

		public INode Node { get; protected set; }

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			// If an empty description was given, do not show an empty decription tool tip
			get { return string.IsNullOrEmpty(Node.Description)?null:Node.Description; }
			//TODO: Make a more smarter tool tip
		}

		public System.Windows.Media.ImageSource Image
		{
			//TODO: Add node images
			get { return null; }
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return Node.Name; }
			protected set { }
		}
	}
}
