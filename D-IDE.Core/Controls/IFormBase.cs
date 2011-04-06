using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace D_IDE.Core.Controls
{
	public interface IFormBase
	{
		AvalonDock.DockingManager DockManager { get; }
		System.Windows.Threading.Dispatcher Dispatcher { get; }


		void RefreshMenu();
		void RefreshGUI();
		void RefreshErrorList();
		void RefreshTitle();
		void RefreshProjectExplorer();

		string LeftStatusText { get; set; }
	}

	public static class IDEUICommands
	{
		public static readonly RoutedUICommand GoTo = new RoutedUICommand("Go to line", "GoTo", typeof(Window));
		public static readonly RoutedUICommand SaveAll = new RoutedUICommand("Save all documents", "SaveAll", typeof(Window));
		public static readonly RoutedUICommand CommentBlock = new RoutedUICommand("Comment code", "CommentBlock", typeof(Window));
		public static readonly RoutedUICommand UncommentBlock = new RoutedUICommand("Uncomment code", "UncommentBlock", typeof(Window));
		public static readonly RoutedUICommand DuplicateLine = new RoutedUICommand("Duplicate current line", "DuplicateLine", typeof(Window));
		public static readonly RoutedUICommand ReformatDoc = new RoutedUICommand("Reformat current document", "ReformatDoc", typeof(Window));
		public static readonly RoutedUICommand ToggleBreakpoint = new RoutedUICommand("Toggle breakpoint", "ToggleBreakpoint", typeof(Window));
		public static readonly RoutedUICommand StepIn = new RoutedUICommand("", "StepIn", typeof(Window));
		public static readonly RoutedUICommand StepOver = new RoutedUICommand("", "StepOver", typeof(Window));
		public static readonly RoutedUICommand LaunchDebugger = new RoutedUICommand("", "LaunchDebugger", typeof(Window));
		public static readonly RoutedUICommand LaunchWithoutDebugger = new RoutedUICommand("", "LaunchWithoutDebugger", typeof(Window));
	}
}
