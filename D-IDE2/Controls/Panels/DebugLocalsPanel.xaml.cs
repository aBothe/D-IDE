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
				var ret = new List<LocalsPanelItem>();
				if (parent == null)
				{
					foreach (var s in IDEManager.DebugManagement.Engine.Symbols.ScopeLocalSymbols)
						ret.Add( new LocalsPanelItem() { Symbol = s, Name = s.Name, Value = s.TextValue, ValueType = s.TypeName });
					return ret;
				}

				var pi = parent as LocalsPanelItem;
				foreach (var s in pi.Symbol.Children)
					ret.Add(new LocalsPanelItem() { Symbol = s, Name = s.Name, Value = s.TextValue, ValueType = s.TypeName });
				return ret;
			}

			public bool HasChildren(object parent)
			{
				var pi = parent as LocalsPanelItem;
				if (pi == null)
					return false;

				return pi.Symbol.ChildrenCount > 0;
			}
		}

		public class LocalsPanelItem
		{
			public DebugScopedSymbol Symbol { get; set; }
			public string Name { get; set; }
			public string Value { get; set; }
			public string ValueType { get; set; }

			readonly ObservableCollection<LocalsPanelItem> children = new ObservableCollection<LocalsPanelItem>();
			public ObservableCollection<LocalsPanelItem> Children { get { return children; } }
		}
	}
}
