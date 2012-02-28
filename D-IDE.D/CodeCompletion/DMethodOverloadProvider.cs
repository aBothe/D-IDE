using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using D_Parser.Resolver;
using D_IDE.Core;
using System.Windows.Controls;
using System.Windows;
using D_Parser.Completion;

namespace D_IDE.D.CodeCompletion
{
	public class DMethodOverloadProvider:IOverloadProvider
	{
		public static DMethodOverloadProvider Create(DEditorDocument doc)
		{
			if (!(doc.lastSelectedBlock is D_Parser.Dom.DMethod))
				return null;

			try
			{
				var argsResult = ParameterInsightResolution.ResolveArgumentContext(
					doc.Editor.Text, 
					doc.Editor.CaretOffset, 
					doc.CaretLocation, 
					doc.lastSelectedBlock as D_Parser.Dom.DMethod, 
					doc.ParseCache);

				if (argsResult == null || argsResult.ResolvedTypesOrMethods == null || argsResult.ResolvedTypesOrMethods.Length < 1)
					return null;

				return new DMethodOverloadProvider(argsResult);
			}
			catch { return null; }
		}
		
		DMethodOverloadProvider(ArgumentsResolutionResult argsResult)
		{
			args = argsResult;
			SelectedIndex = args.CurrentlyCalledMethod;
		}

		ArgumentsResolutionResult args;
		int selIndex = 0;

		public int Count
		{
			get { return args.ResolvedTypesOrMethods.Length; }
		}

		public ResolveResult CurrentResult { get { return args.ResolvedTypesOrMethods[selIndex]; } }

		public object CurrentHeader
		{
			get { return new TextBlock() { Text=CurrentResult.ToString(), FontWeight=FontWeights.DemiBold}; }
		}

		public object CurrentContent
		{
			get {

				if (CurrentResult is MemberResult)
					return (CurrentResult as MemberResult).Node.Description;
				if (CurrentResult is TypeResult)
					return (CurrentResult as TypeResult).Node.Description;

				return null;
			}
		}

		public string CurrentIndexText
		{
			get { return (SelectedIndex+1).ToString()+"/"+args.ResolvedTypesOrMethods.Length.ToString(); }
		}

		public int SelectedIndex
		{
			get { return selIndex; }
			set { 
				selIndex = value;

				try
				{
					NotifyPropertyChanged("SelectedIndex");
					NotifyPropertyChanged("CurrentContent");
					NotifyPropertyChanged("CurrentHeader");
					NotifyPropertyChanged("CurrentIndexText");
				}
				catch (Exception ex) { ErrorLogger.Log(ex); }
			}
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
