using Parser.Core;
using D_IDE.Core;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using D_IDE.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DebugEngineWrapper;

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
	}
}
