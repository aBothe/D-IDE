using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using D_Parser.Resolver;

namespace D_IDE.D.CodeCompletion
{
	public class DMethodOverloadProvider:IOverloadProvider
	{
		public static DMethodOverloadProvider Create(DEditorDocument doc)
		{
			var imports = DCodeResolver.ResolveImports(doc.SyntaxTree, DCodeCompletionSupport.EnumAvailableModules(doc));

			var argsResult = DResolver.ResolveArgumentContext(doc.Editor.Text, doc.Editor.CaretOffset, doc.CaretLocation, doc.lastSelectedBlock, imports);

			if (argsResult == null)
				return null;

			return new DMethodOverloadProvider(argsResult);
		}



		public DMethodOverloadProvider(DResolver.ArgumentsResolutionResult argsResult)
		{
			args = argsResult;
			
		}

		DResolver.ArgumentsResolutionResult args;

		public int Count
		{
			get { return args.ResolvedTypesOrMethods.Length; }
		}

		public object CurrentContent
		{
			get { return args.ResolvedTypesOrMethods[SelectedIndex]; }
		}

		public object CurrentHeader
		{
			get { return null; }
		}

		public string CurrentIndexText
		{
			get { return SelectedIndex.ToString()+" of "+args.ResolvedTypesOrMethods.Length.ToString(); }
		}

		public int SelectedIndex
		{
			get { return args.CurrentlyCalledMethod; }
			set { args.CurrentlyCalledMethod = value; NotifyPropertyChanged("SelectedIndex"); }
		}


		private void NotifyPropertyChanged(string info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(info));
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	}
}
