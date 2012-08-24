using System;
using System.Windows.Controls;
using System.Windows;
using ICSharpCode.AvalonEdit.CodeCompletion;
using D_IDE.Core;
using D_Parser.Completion;
using D_Parser.Resolver;

namespace D_IDE.D.CodeCompletion
{
	public class DMethodOverloadProvider:IOverloadProvider
	{
		public static DMethodOverloadProvider Create(DEditorDocument doc)
		{
			try
			{
				var argsResult = ParameterInsightResolution.ResolveArgumentContext(doc);

				if (argsResult == null || argsResult.ResolvedTypesOrMethods == null || argsResult.ResolvedTypesOrMethods.Length < 1)
					return null;

				return new DMethodOverloadProvider(argsResult);
			}
			catch { return null; }
		}
		
		DMethodOverloadProvider(ArgumentsResolutionResult argsResult)
		{
			ParameterData = argsResult;
			SelectedIndex = ParameterData.CurrentlyCalledMethod;
		}

		public readonly ArgumentsResolutionResult ParameterData;
		int selIndex = 0;

		public int Count
		{
			get { return ParameterData.ResolvedTypesOrMethods.Length; }
		}

		public AbstractType CurrentResult { get { return ParameterData.ResolvedTypesOrMethods[selIndex]; } }

		public object CurrentHeader
		{
			get { return new TextBlock() { Text=CurrentResult == null ? "" : CurrentResult.ToString(), FontWeight=FontWeights.DemiBold}; }
		}

		public object CurrentContent
		{
			get {

				if (CurrentResult is DSymbol)
					return ((DSymbol)CurrentResult).Definition.Description;
				return null;
			}
		}

		public string CurrentIndexText
		{
			get { return (SelectedIndex+1).ToString()+"/"+ParameterData.ResolvedTypesOrMethods.Length.ToString(); }
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
