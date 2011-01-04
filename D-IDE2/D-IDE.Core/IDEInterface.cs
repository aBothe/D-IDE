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
		public readonly static string SettingsSaveLocationFile = Util.ApplicationStartUpPath + "\\SettingsDirectory.cfg";

		/// <summary>
		/// Gets and sets the directory where all configurations are stored
		/// </summary>
		public static string ConfigDirectory
		{
			get
			{/*
				try
				{
					if (File.Exists(SettingsSaveLocationFile))
					{
						var con = File.ReadAllText(SettingsSaveLocationFile);
						if (Directory.Exists(con))
							return con;
					}
				}
				catch { }*/
				// Create and return default value
				string ret = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\D-IDE.config";
				Util.CreateDirectoryRecursively(ret);
				return ret;
			}/*
			set
			{
				try
				{
					File.WriteAllText(SettingsSaveLocationFile, value);
				}
				catch { }
			}*/
		}
	}
}
