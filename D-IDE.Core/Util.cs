using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using System.Threading;

namespace D_IDE.Core
{
    public class Util
	{
		#region File I/O
		public static readonly string ApplicationStartUpPath =Path.GetDirectoryName( Assembly.GetEntryAssembly().Location);

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

		/// <summary>
		/// Tries to rename or move a file relative to the directory of the source file.
		/// </summary>
		/// <param name="newFileName">Can be a relative or a absolute file path</param>
		/// <returns>True if file could be moved, false if new file already existed</returns>
		public static bool MoveFile(string file, string newFileName)
		{
			var newName=Path.IsPathRooted(newFileName)?newFileName:Path.Combine(Path.GetDirectoryName(file) ,newFileName);

			if (!File.Exists(file) || File.Exists(newName))
				return false;
			try	{File.Move(file, newName);}
			catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			return true;
		}

		public static System.Windows.MessageBoxResult ShowFileExistsDialog(string file)
		{
			return System.Windows.MessageBox.Show(
				"\"" + Path.GetFileName(file) + "\" already exists. Continue with overwriting?", 
				"File already exists", 
				System.Windows.MessageBoxButton.YesNoCancel, 
				System.Windows.MessageBoxImage.Question, 
				System.Windows.MessageBoxResult.No);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <returns>True if user wishes to continue</returns>
		public static bool ShowDeleteFileDialog(string file)
		{
			return System.Windows.MessageBox.Show(
				"Continue with deleting \""+Path.GetFileName( file)+"\"",
				"Delete file/directory",
				System.Windows.MessageBoxButton.YesNo,
				System.Windows.MessageBoxImage.Question,
				System.Windows.MessageBoxResult.Yes) == System.Windows.MessageBoxResult.Yes;
		}

		public static string PurifyFileName(string file)
		{
			string r = file;
			foreach (var c in Path.GetInvalidFileNameChars())
				r = r.Replace(c, '_');
			return r;
		}
		public static string PurifyDirName(string dirName)
		{
			string r = dirName;
			foreach (var c in Path.GetInvalidPathChars())
				r = r.Replace(c, '_');
			return r;
		}
		#endregion

		/// <summary>
		/// Strip all XML tags from a given string
		/// Taken from http://dotnetperls.com/remove-html-tags
		/// </summary>
		public static string StripXmlTags(string source)
		{
			char[] array = new char[source.Length];
			int arrayIndex = 0;
			bool inside = false;

			for (int i = 0; i < source.Length; i++)
			{
				char let = source[i];
				if (let == '<')
				{
					inside = true;
					continue;
				}
				if (let == '>')
				{
					inside = false;
					continue;
				}
				if (!inside)
				{
					array[arrayIndex] = let;
					arrayIndex++;
				}
			}
			return new string(array, 0, arrayIndex);
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

		/// <summary>
		/// Returns true if the thread which calls IsDispatcherThread is the thread that holds up the MainWindow's Message Loop
		/// </summary>
		public static bool IsDispatcherThread
		{
			get {
				return Thread.CurrentThread.ManagedThreadId == CoreManager.Instance.MainWindow.Dispatcher.Thread.ManagedThreadId;
			}
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

		/*public unsafe static System.Drawing.Bitmap BitmapSourceToBitmap(BitmapSource src)
		{
			System.Drawing.Bitmap btm = null;
			int width = src.PixelWidth;
			int height = src.PixelHeight;
			int stride = width * ((src.Format.BitsPerPixel + 7) / 8);
			var bits = new byte[height * stride];
			src.CopyPixels(bits, stride, 0);

			fixed (byte* pB = bits)
			{
				var ptr = new IntPtr(pB);

				btm = new System.Drawing.Bitmap(
				width,height,stride,
				System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
				ptr);
			}

			return btm;
		}*/

		public static void AddGDIImageToImageList(ImageList il,string key,object imgObj)
		{
			if (imgObj is System.Drawing.Image)
				il.Images.Add(key, imgObj as System.Drawing.Image);
			else if (imgObj is System.Drawing.Icon)
				il.Images.Add(key,imgObj as System.Drawing.Icon);
		}
		#endregion
	}

    public class ErrorLogger
    {
		static ErrorLogger _inst;
		public static ErrorLogger Instance
		{
			get
			{
				if (_inst == null)
					_inst = new ErrorLogger();
				return _inst;
			}
			set { _inst = value; }
		}

        public static void Log(Exception ex)
        {
			Log(ex,ErrorType.Error,ErrorOrigin.System);
        }

		public static void Log(Exception ex, ErrorType ErrorType, ErrorOrigin Origin)
		{
			Instance.OnLog(ex,ErrorType,Origin);
		}

		public static void Log(string Message)
		{
			Log(Message, ErrorType.Error, ErrorOrigin. System);
		}

		protected virtual void OnLog(Exception ex, ErrorType ErrorType, ErrorOrigin Origin)
		{
			OnLog(ex.Message, ErrorType, Origin);
		}

		protected virtual void OnLog(string Message, ErrorType ErrorType, ErrorOrigin origin)
		{
			var icon = MessageBoxIcon.Error;
			if (ErrorType==ErrorType.Warning)
				icon = MessageBoxIcon.Warning;
			else if (ErrorType==ErrorType.Information || ErrorType==ErrorType.Message)
				icon = MessageBoxIcon.Information;

			MessageBox.Show(Message,origin.ToString() +" "+ ErrorType.ToString(),MessageBoxButtons.OK,icon);

			if (origin == ErrorOrigin.System)
				File.AppendAllText(IDEInterface.ConfigDirectory+"\\sys.log",Message+"\r\n",System.Text.Encoding.Unicode);
		}

		public static void Log(string Message, ErrorType ErrorType,ErrorOrigin Origin)
		{
			Instance.OnLog(Message,ErrorType,Origin);
		}
    }

	public enum ErrorType
	{
		/// <summary>
		/// Normal message; Mostly used for logging program outputs
		/// </summary>
		Message=0,
		Information,
		Warning,
		Error,
	}

	public enum ErrorOrigin
	{
		/// <summary>
		/// General log message
		/// </summary>
		System = 0,
		/// <summary>
		/// Build message
		/// </summary>
		Build = 1,
		/// <summary>
		/// Message that comes from the debugger
		/// </summary>
		Debug=2,
		/// <summary>
		/// Redirected program output
		/// </summary>
		Program=3,

		Parser=4,
	}

	/// <summary>
	/// A converter for WPF controls.
	/// Converts GDI Image to WPf BitmapImage objects
	/// </summary>
	[ValueConversion(typeof(System.Drawing.Image), typeof(BitmapImage))]
	[ValueConversion(typeof(System.Drawing.Icon), typeof(BitmapImage))]
	public class GDIToImageSrcConverter : IValueConverter
	{
		public object Convert(object v, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (v is System.Drawing.Image)
				return Util.FromDrawingImage(v as System.Drawing.Image);
			else if (v is System.Drawing.Icon)
				return Util.FromDrawingImage(v as System.Drawing.Icon);

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

#region LowLevel
	public class Win32
	{
		public static System.Drawing.Image GetIcon(string FilePath, bool Small)
		{
			IntPtr hImgSmall;
			IntPtr hImgLarge;
			SHFILEINFO shinfo = new SHFILEINFO();
			if (Small)
			{
				hImgSmall = SHGetFileInfo(Path.GetFileName(FilePath), 0,
					ref shinfo, (uint)Marshal.SizeOf(shinfo),
					SHGFI_ICON | SHGFI_SMALLICON |SHGFI_USEFILEATTRIBUTES);
			}
			else
			{
				hImgLarge = SHGetFileInfo(Path.GetFileName(FilePath), 0,
					ref shinfo, (uint)Marshal.SizeOf(shinfo),
					SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);
			}
			if (shinfo.hIcon == null) return Small ? CoreIcons.file16 : CoreIcons.file32;
			try
			{
				return System.Drawing.Icon.FromHandle(shinfo.hIcon).ToBitmap();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, FilePath);
				return Small ? CoreIcons.file16 : CoreIcons.file32;
			}
		}

		public static bool MoveToRecycleBin(params string[] files)
		{
			foreach (var file in files)
			{
				try
				{
					if (Directory.Exists(file))
						Directory.Delete(file,true);
					else if(File.Exists(file))
						File.Delete(file);
				}
				catch (Exception ex) { ErrorLogger.Log(ex); return false; }
			}
			return true;
		}

		public const uint SHGFI_ICON = 0x100;
		public const uint SHGFI_LARGEICON = 0x0;
		public const uint SHGFI_SMALLICON = 0x1;
		public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

		[StructLayout(LayoutKind.Sequential)]
		public struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		[DllImport("shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath,
			uint dwFileAttributes,
			ref SHFILEINFO psfi,
			uint cbSizeFileInfo,
			uint uFlags);

		[DllImport("user32.dll")]
		public static extern int DestroyIcon(IntPtr hIcon);
	}
#endregion
}
