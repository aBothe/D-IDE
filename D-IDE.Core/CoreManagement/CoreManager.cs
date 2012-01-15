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
			get {
				if (MainWindow==null || MainWindow.DockManager == null)
					return null;
				return MainWindow.DockManager.ActiveDocument as AbstractEditorDocument; }
		}

		public ICollection<AbstractEditorDocument> Editors
		{
			get
			{
				var l = new List<AbstractEditorDocument>();

				foreach (var ed in MainWindow.DockManager.Documents)
					if (ed is AbstractEditorDocument)
						l.Add(ed as AbstractEditorDocument);

				return l;
			}
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
		public abstract AbstractEditorDocument OpenFile(string file,int line,int col);
	}
}
