using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D_IDE.Core.Controls
{
	public interface IFormBase
	{
		AvalonDock.DockingManager DockManager { get; }

		void RefreshGUI();
		void RefreshErrorList();
		void RefreshTitle();
		void RefreshProjectExplorer();
	}
}
