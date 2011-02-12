using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AvalonDock;
using Aga.Controls.Tree;
using DebugEngineWrapper;
using System.Collections.ObjectModel;
using D_IDE.Core;

namespace D_IDE.Controls.Panels
{
	/// <summary>
	/// Interaktionslogik für LogPanel.xaml
	/// </summary>
	public partial class DebugLocalsPanel : DockableContent
	{
		public DebugLocalsPanel()
		{
			InitializeComponent();

			MainTree.Model = null;
		}

		public void RefreshTable()
		{
			if(IDEManager.DebugManagement.IsDebugging)
				MainTree.Model =new DebugLocalsTreeModel();
		}

		public class DebugLocalsTreeModel:ITreeModel
		{
			public System.Collections.IEnumerable GetChildren(object parent)
			{
				var supp= CoreManager.DebugManagement.CurrentDebugSupport;
				if(supp==null)
					return null;

				return supp.GetChildSymbols(IDEManager.DebugManagement.Engine.Symbols.ScopeLocalSymbols,parent as DebugSymbolWrapper);
			}

			public bool HasChildren(object parent)
			{
				var pi = parent as DebugSymbolWrapper;
				if (pi == null)
					return false;

				var supp = CoreManager.DebugManagement.CurrentDebugSupport;
				if (supp == null)
					return false;

				return supp.HasChildren(IDEManager.DebugManagement.Engine.Symbols.ScopeLocalSymbols, pi) ;
			}
		}
	}
}
