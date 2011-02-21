using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core.Controls;

namespace D_IDE.Core
{
	public abstract partial class CoreManager
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

		public abstract AbstractEditorDocument OpenFile(string file);
	}
}
