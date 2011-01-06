using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace D_IDE.Core
{
	public class IDEInterface
	{
		readonly static string SettingsSaveLocationFile = Util.ApplicationStartUpPath + "\\StoreAtUserDocs.flag";

		/// <summary>
		/// Gets and sets the directory where all configurations are stored
		/// </summary>
		public static string ConfigDirectory
		{
			get
			{
				// Create and return default value
				var ret = Environment.GetFolderPath(StoreSettingsAtUserFiles?
					Environment.SpecialFolder.MyDocuments:
					Environment.SpecialFolder.CommonApplicationData) + "\\D-IDE.config";
				Util.CreateDirectoryRecursively(ret);
				return ret;
			}
		}

		public static bool StoreSettingsAtUserFiles
		{
			get
			{
				return File.Exists(SettingsSaveLocationFile);
			}
			set
			{
				if (value && !StoreSettingsAtUserFiles)
					File.WriteAllText(SettingsSaveLocationFile, "This flag file means that the settings are stored at the user documents ("+
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+")");
				else if (!value && StoreSettingsAtUserFiles)
					File.Delete(SettingsSaveLocationFile);
			}
		}
	}
}
