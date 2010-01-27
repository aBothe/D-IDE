using System;

namespace ToolboxLibrary
{
	/// <summary>
	/// ToolboxTabs.
	/// </summary>
	public class ToolboxTab
	{
		private string m_name = null;
		private ToolboxItemCollection m_toolboxItemCollection = null;

		public ToolboxTab()
		{
		}

		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}

		public ToolboxItemCollection ToolboxItems
		{
			get
			{
				return m_toolboxItemCollection;
			}
			set
			{
				m_toolboxItemCollection = value;
			}
		}
	}
}
