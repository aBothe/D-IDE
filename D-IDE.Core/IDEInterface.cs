using System;
using System.IO;


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
				var ret = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\"+ConfigDirectoryName;
				Util.CreateDirectoryRecursively(ret);
				return ret;
			}
		}

		public static string CommonlyUsedDirectory
		{
			get {
				var ret = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + "\\" + ConfigDirectoryName;

				// Provide backward compatibility
				var ret2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)+"\\"+ConfigDirectoryName;
				if (!Directory.Exists(ret) && Directory.Exists(ret2))
				{
					return ret2;
				}
				
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
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+")");
				else if (!value && StoreSettingsAtUserFiles)
					File.Delete(SettingsSaveLocationFile);
			}
		}
		#endregion
	}
}
