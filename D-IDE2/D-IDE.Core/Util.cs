using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace D_IDE.Core
{
    public class Util
    {
        public static readonly string ApplicationStartUpPath = Directory.GetCurrentDirectory();

        /// <summary>
        /// Helper function to check if directory exists. Otherwise the directory will be created.
        /// </summary>
        /// <param name="dir"></param>
        public static void CreateDirectoryRecursively(string dir)
        {
            if (Directory.Exists(dir)) return;

            string tdir = "";
            foreach (string d in dir.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                tdir += d + "\\";
                if (!Directory.Exists(tdir))
                {
                    try
                    {
                        Directory.CreateDirectory(tdir);
                    }
                    catch { return; }
                }
            }
        }

        public static DateTime DateFromUnixTime(long t)
        {
            var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return ret.AddSeconds(t);
        }

        public static long UnixTimeFromDate(DateTime t)
        {
            var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long)(t - ret).TotalSeconds;
        }
        
    }

    public class ErrorLogger
    {
        public static void Log(Exception ex)
        {

        }
    }
}
