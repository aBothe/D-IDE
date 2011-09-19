using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;

namespace D_IDE.Core.Controls
{
	public interface IFormBase
	{
		AvalonDock.DockingManager DockManager { get; }
		System.Windows.Threading.Dispatcher Dispatcher { get; }

		ISearchResultPanel SearchResultPanel { get; }

		void RefreshMenu();
		void RefreshGUI();
		void RefreshErrorList();
		void RefreshTitle();
		void RefreshProjectExplorer();

		string LeftStatusText { get; set; }
		string SecondLeftStatusText { get; set; }
	}

	public interface ISearchResultPanel
	{
		string SearchString
		{
			get;
			set;
		}

		SearchResult[] Results
		{
			get;
			set;
		}

		void Show();
	}

	public class SearchResult
	{
		public string File { get; set; }
		public string FileName
		{
			get
			{
				return System.IO.Path.GetFileName(File);
			}
		}

		public int Offset { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }

		public string CodeSnippet { get; set; }
	}

	public static class IDEUICommands
	{
		public static readonly RoutedUICommand GoTo = new RoutedUICommand("Go to line", "GoTo", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.G, ModifierKeys.Control) }));

		public static readonly RoutedUICommand SaveAll = new RoutedUICommand("Save all documents", "SaveAll", typeof(Window));
		public static readonly RoutedUICommand CommentBlock = new RoutedUICommand("Comment code", "CommentBlock", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.K, ModifierKeys.Control) }));

		public static readonly RoutedUICommand UncommentBlock = new RoutedUICommand("Uncomment code", "UncommentBlock", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.K, ModifierKeys.Control|ModifierKeys.Shift) }));

		public static readonly RoutedUICommand ReformatDoc = new RoutedUICommand("Reformat current document", "ReformatDoc", typeof(Window));

		public static readonly RoutedUICommand ToggleBreakpoint = new RoutedUICommand("Toggle breakpoint", "ToggleBreakpoint", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.F9) }));

		public static readonly RoutedUICommand StepIn = new RoutedUICommand("", "StepIn", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.F11 )}));

		public static readonly RoutedUICommand StepOver = new RoutedUICommand("", "StepOver", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.F10) }));

		public static readonly RoutedUICommand LaunchDebugger = new RoutedUICommand("", "LaunchDebugger", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.F5) }));

		public static readonly RoutedUICommand LaunchWithoutDebugger = new RoutedUICommand("", "LaunchWithoutDebugger", typeof(Window),
			new InputGestureCollection(new[] { new KeyGesture(Key.F5, ModifierKeys.Shift) }));

		public static readonly RoutedUICommand ShowProjectSettings = new RoutedUICommand("","ShowProjectSettings",typeof(Window));
	}
}
