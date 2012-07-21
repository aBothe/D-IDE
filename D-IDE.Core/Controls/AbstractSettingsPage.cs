using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace D_IDE.Core
{
	/// <summary>
	/// Since the WPF Designer of Visual Studio enforces declarable classes, there is a faked abstractness
	/// </summary>
	public class AbstractSettingsPage : UserControl
	{
		public virtual bool ApplyChanges() { return true; }
		public virtual void RestoreDefaults() { }
		public virtual void LoadCurrent() { }

		public virtual string SettingCategoryName { get { return String.Empty; } }
		public virtual IEnumerable< AbstractSettingsPage> SubCategories { get { return null; } }
	}

	public class AbstractProjectSettingsPage :UserControl
	{
		public virtual bool ApplyChanges(Project Project) { return true; }
		public virtual void LoadCurrent(Project Project) { }

		public virtual string SettingCategoryName { get { return String.Empty; } }
	}
}
