using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
}
