using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace D_IDE.Core
{
    public class Util
	{
		#region File I/O
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

		public static string PurifyFileName(string file)
		{
			string r = file;
			foreach (var c in Path.GetInvalidFileNameChars())
				r = r.Replace(c, '_');
			return r;
		}
		#endregion

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

		#region Icons
		public static BitmapImage FromDrawingImage(System.Drawing.Icon ico)
		{
			var ms = new MemoryStream();
			ico.Save(ms);

			var bImg = new BitmapImage();
			bImg.BeginInit();
			bImg.StreamSource = new MemoryStream( ms.ToArray());
			bImg.EndInit();

			return bImg;
		}

		public static BitmapImage FromDrawingImage(System.Drawing.Image img)
		{
			var ms = new MemoryStream();
			// Temporarily save it as png image
			img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

			var bImg = new BitmapImage();
			bImg.BeginInit();
			bImg.StreamSource = new MemoryStream(ms.ToArray());
			bImg.EndInit();

			return bImg;
		}
		#endregion
	}

    public class ErrorLogger
    {
        public static void Log(Exception ex)
        {

        }
    }
}
