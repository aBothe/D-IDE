using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using ICSharpCode.AvalonEdit.Highlighting;


namespace D_IDE.Core
{
	public class IDEInterface
	{
		#region Files
		const string ConfigDirectoryName = "D-IDE.config";
		readonly static string SettingsSaveLocationFile = CommonlyUsedDirectory + "\\StoreAtUserDocs.flag";

		/// <summary>
		/// Gets and sets the directory where all configurations are stored
		/// </summary>
		public static string ConfigDirectory
		{
			get
			{
				if(!StoreSettingsAtUserFiles)
					return CommonlyUsedDirectory;

				// Create and return default value
				var ret = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\"+ConfigDirectoryName;
				Util.CreateDirectoryRecursively(ret);
				return ret;
			}
		}

		public static string CommonlyUsedDirectory
		{
			get {
				var ret = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + ConfigDirectoryName;
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
		#endregion

		#region Output & Logging
		public delegate void LogEvent(string Message);
		public static event LogEvent LogHandler;
		/// <summary>
		/// Appends text to the MainWindow's output log.
		/// Note: This event will overloaded when D-IDE starts.
		/// </summary>
		
		public static void Log(string s)
		{
			LogHandler(s);
		}
		#endregion
	}
}
