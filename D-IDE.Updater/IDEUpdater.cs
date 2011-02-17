using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;
using System.Diagnostics;

namespace D_IDE.Updater
{
	/// <summary>
	/// Initializes/Updates the current D-IDE installation 
	/// </summary>
	public class IDEUpdater
	{
		public const string FileVersionFile = "LastModificationTime";
		public const string TimeStampUrl = "http://d-ide.sourceforge.net/d-ide.php?action=fileversion";
		public const string ArchiveUrl = "http://d-ide.sourceforge.net/d-ide.php";
		public static string OutputDir = Directory.GetCurrentDirectory();

		[STAThread]
		static int Main(string[] args)
		{
			string mFileVerFile=Path.Combine(OutputDir,FileVersionFile);

			Console.WriteLine("D-IDE Updater\r\n");
			string LastOnlineModTime="";

			// Get latest online file timestamp
			try
			{
				Console.Write("Get last modification timestamp... ");
				LastOnlineModTime = new WebClient().DownloadString(TimeStampUrl);
				Console.WriteLine(LastOnlineModTime);

				// Check if offline version is already the latest
				if (File.Exists(mFileVerFile) && File.ReadAllText(mFileVerFile) == LastOnlineModTime)
				{
					Console.WriteLine("You already have got the latest version!");
					return 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 1;
			}

			if (!CheckForOpenInstances())
				return 1;

			// Get the latest build archive
			string archive = "";
			if (DownloadLatestBuild(ArchiveUrl, out archive) && CheckForOpenInstances() && ExtractFiles(archive, OutputDir))
			{
				Console.WriteLine("Download successful!");

				// Save archive modification time to make later watch-outs for program updates working
				// Note: Just save it AFTER the update was successful!
				File.WriteAllText(mFileVerFile, LastOnlineModTime);
				
				return 0;
			}
			Console.ReadKey(); // Halt on error
			return 1;
		}

		/// <summary>
		/// Check for open D-IDE.exes that are located in the OutputDir and wait until they've been closed down
		/// </summary>
		public static bool CheckForOpenInstances()
		{
			int i = 10;
			while (true)
			{
				var prcs = Process.GetProcessesByName("D-IDE");
				if (prcs == null || prcs.Length < 1)
					break;

				if (i < 0)
					return false;

				bool br = true;
				foreach (var prc in prcs)
				{
					if (Path.GetDirectoryName(prc.Modules[0].FileName) == OutputDir)
					{
						Console.WriteLine("Close D-IDE.exe first to enable update! (" + i.ToString() + " attempts remaining!)");
						i--;
						Thread.Sleep(2000);
						br = false;
					}
				}
				if (br)
					break;
			}
			return true;
		}

		public static bool DownloadLatestBuild(string ArchiveUrl,out string TempFile)
		{
			TempFile = Path.GetTempFileName();
			Console.WriteLine("Download "+ArchiveUrl);
			try
			{
				var wc = new WebClient();
				wc.DownloadFile(new Uri(ArchiveUrl), TempFile);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error:");
				Console.WriteLine(ex.Message);
				return false;
			}
			return true;
		}

		public static bool ExtractFiles(string ZipFile, string OutputDirectory)
		{
			CreateDirectoryRecursively(OutputDirectory);
			try
			{
				Console.WriteLine("Extract archive " + ZipFile + " to " + OutputDirectory + "\r\n");
				using (ZipInputStream s = new ZipInputStream(File.OpenRead(ZipFile)))
				{
					ZipEntry theEntry;
					while ((theEntry = s.GetNextEntry()) != null)
					{
						Console.WriteLine(theEntry.Name);

						string directoryName = Path.GetDirectoryName(theEntry.Name);
						string fileName = Path.GetFileName(theEntry.Name);

						if (theEntry.IsDirectory)
						{
							CreateDirectoryRecursively(theEntry.Name);
							continue;
						}

						// create directory
						if (directoryName.Length > 0)
							CreateDirectoryRecursively(OutputDirectory + "\\" + directoryName);

						if (fileName != String.Empty)
						{
							using (FileStream streamWriter = File.Create(OutputDirectory + "\\" + theEntry.Name))
							{
								int size = 2048;
								byte[] data = new byte[2048];
								while (true)
								{
									size = s.Read(data, 0, data.Length);
									if (size > 0)
										streamWriter.Write(data, 0, size);
									else
										break;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error:");
				Console.WriteLine(ex.Message);
				return false;
			}

			return true;
		}

		#region Util
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

		public static long UnixTimeFromDate(DateTime t)
		{
			var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return (long)(t - ret).TotalSeconds;
		}
		#endregion
	}
}
