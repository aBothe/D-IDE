using D_IDE.Core;
using System.Threading;
using System;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using D_IDE.Dialogs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

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
			return WorkbenchLogic.Instance.OpenFile(file);
		}

		public override AbstractEditorDocument OpenFile(string file, int line, int col)
		{
			return WorkbenchLogic.Instance.OpenFile(file, line, col);
		}
	}
}
