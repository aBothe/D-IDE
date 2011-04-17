using D_IDE.Core;

namespace D_IDE
{
	partial class IDEManager:CoreManager
	{
		public static new IDEManager Instance
		{
			get { return CoreManager.Instance as IDEManager; }
			set { CoreManager.Instance = value; }
		}

		public IDEManager(MainWindow mw):base(mw)		{}

		public new MainWindow MainWindow
		{
			get { return base.MainWindow as MainWindow; }
		}

		public override AbstractEditorDocument OpenFile(string file)
		{
			return EditingManagement.OpenFile(file);
		}

		public override AbstractEditorDocument OpenFile(string file, int line, int col)
		{
			return EditingManagement.OpenFile(file, line, col);
		}
	}
}
