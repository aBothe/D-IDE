﻿using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;

namespace D_Parser.Completion
{
	/// <summary>
	/// Generic interface between a high level editor object and the low level completion engine
	/// </summary>
	public class EditorData:IEditorData
	{
		public virtual string ModuleCode { get; set; }
		public virtual CodeLocation CaretLocation { get; set; }
		public virtual int CaretOffset { get; set; }
		public virtual DModule SyntaxTree { get; set; }

		public virtual IEnumerable<IAbstractSyntaxTree> ParseCache { get; set; }
		public virtual IEnumerable<IAbstractSyntaxTree> ImportCache { get; set; }

		public void ApplyFrom(IEditorData data)
		{
			ModuleCode = data.ModuleCode;
			CaretLocation = data.CaretLocation;
			CaretOffset = data.CaretOffset;
			SyntaxTree = data.SyntaxTree;
			ParseCache = data.ParseCache;
			ImportCache = data.ImportCache;
		}
	}

	public interface IEditorData
	{
		string ModuleCode { get; }
		CodeLocation CaretLocation { get; }
		int CaretOffset { get; }
		DModule SyntaxTree { get; }

		IEnumerable<IAbstractSyntaxTree> ParseCache { get; }
		IEnumerable<IAbstractSyntaxTree> ImportCache { get; }
	}
}
