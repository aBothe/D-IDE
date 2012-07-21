using AvalonDock;
using Aga.Controls.Tree;
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
			Name = "DebugLocalsPanel";
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

				var r= supp.GetChildSymbols(IDEManager.DebugManagement.Engine.Symbols.ScopeLocalSymbols,parent as DebugSymbolWrapper);
				return r;
			}

			public bool HasChildren(object parent)
			{
				var pi = parent as DebugSymbolWrapper;
				if (pi == null)
					return false;

				var supp = CoreManager.DebugManagement.CurrentDebugSupport;
				if (supp == null)
					return false;

				var r= supp.HasChildren(IDEManager.DebugManagement.Engine.Symbols.ScopeLocalSymbols, pi) ;
				return r;
			}
		}
	}
}
