using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core.Controls;

namespace D_IDE.Core
{
	public abstract class CoreManager
	{
		public static CoreManager Instance;

		public CoreManager(IFormBase MainWindow)
		{
			this.MainWindow=MainWindow;
		}

		#region Properties
		public readonly IFormBase MainWindow;

		public AbstractEditorDocument CurrentEditor
		{
			get { return MainWindow.DockManager.ActiveDocument as AbstractEditorDocument; }
		}

		public IEnumerable<AbstractEditorDocument> Editors
		{
			get { return from e in MainWindow.DockManager.Documents where e is AbstractEditorDocument select e as AbstractEditorDocument; }
		}

		public static Solution CurrentSolution { get; set; }
		#endregion

		public bool CanUpdateGUI = true;
		public void UpdateGUI()
		{
			if (CanUpdateGUI)
				MainWindow.RefreshGUI();
		}

		public class ErrorManagement
		{
			/// <summary>
			/// In this array, all errors are listed
			/// </summary>
			public static GenericError[] Errors { get; protected set; }

			/// <summary>
			/// Returns file specific errors.
			/// </summary>
			public static GenericError[] GetErrorsForFile(string file)
			{
				if(Errors==null || Errors.Length<1)
					return new GenericError[]{};

				return Errors.Where(err => err.FileName == file).ToArray();
			}

			public static BuildResult LastBuildResult { get; set; }

			public static GenericError[] LastParseErrors
			{
				get
				{
					var ed = Instance.CurrentEditor as EditorDocument;
					if (ed == null || ed.SyntaxTree == null)
						return new GenericError[] { };

					var ret = new List<GenericError>();
					foreach (var err in ed.SyntaxTree.ParseErrors)
						ret.Add(new ParseError(err));
					return ret.ToArray();
				}
			}

			/// <summary>
			/// Refreshes the commonly used error list.
			/// Also updates the error panel's error list view.
			/// </summary>
			public static void RefreshErrorList()
			{
				var el = new List<GenericError>();

				// Add unbound build errors
				if (LastBuildResult!=null)
					el.AddRange(LastBuildResult.BuildErrors);
				// (Bound) Solution errors
				else if (CurrentSolution != null)
					foreach (var prj in CurrentSolution)
						if(prj.LastBuildResult!=null)
							el.AddRange(prj.LastBuildResult.BuildErrors);
				// Errors that occurred while parsing source files
				el.AddRange(LastParseErrors);

				Errors = el.ToArray();

				Instance.MainWindow.RefreshErrorList();

				foreach (var ed in CoreManager.Instance.Editors)
					if (ed is EditorDocument)
						(ed as EditorDocument).RefreshErrorHighlightings();
			}
		}
	}
}
