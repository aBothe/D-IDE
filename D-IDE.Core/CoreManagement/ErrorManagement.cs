using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core
{
	public abstract partial class CoreManager
	{
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
				if (Errors == null || Errors.Length < 1)
					return new GenericError[] { };

				return Errors.Where(err => err.FileName == file).ToArray();
			}

			public static BuildResult LastSingleBuildResult { get; set; }

			public static GenericError[] LastParseErrors
			{
				get
				{
					var ed = Instance.CurrentEditor as EditorDocument;
					if (ed == null || ed.ParserErrors == null)
						return new GenericError[] { };

					return ed.ParserErrors.ToArray();
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
				if (LastSingleBuildResult != null)
					el.AddRange(LastSingleBuildResult.BuildErrors);
				// (Bound) Solution errors
				else if (CurrentSolution != null)
					foreach (var prj in CurrentSolution)
					{
						// Add project errors
						if (prj.LastBuildResult != null)
							el.AddRange(prj.LastBuildResult.BuildErrors);
						// Add errors of its modules
						foreach (var m in prj.Files)
							if(m.LastBuildResult!=null)
								el.AddRange(m.LastBuildResult.BuildErrors);
					}
				
				// Errors that occurred while parsing source files
				foreach(var ed in CoreManager.Instance.Editors)
					if(ed is IEditorDocument && ed.HasProject && (ed as IEditorDocument).ParserErrors!=null)
						el.AddRange((ed as IEditorDocument).ParserErrors);

				Errors = el.ToArray();

				Instance.MainWindow.RefreshErrorList();

				foreach (var ed in CoreManager.Instance.Editors)
					if (ed is EditorDocument)
						(ed as EditorDocument).RefreshErrorHighlightings();
			}
		}

	}
}
