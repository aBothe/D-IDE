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
			Console.WriteLine("D-IDE Updater");
			Console.WriteLine("\tby Alexander Bothe");
			Console.WriteLine();
			Console.WriteLine("Add -help to the command line to enum available commands");
			Console.WriteLine();

			// Process optional arguments
			bool DontAsk = false;
			bool StartImmediately = false;
			int i=0;
			for (i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-s":
						DontAsk = true;
						break;
					case "-a":
						StartImmediately = true;
						break;
					case "-o":
						i++;
						if (args.Length >i)
							OutputDir = args[i];
						break;
					case "-help":
					case "-?":
						Console.WriteLine("Commands:");
						Console.WriteLine("-s\tDon't halt on errors");
						Console.WriteLine("-a\tAutomatically launch D-IDE after update has been finished");
						Console.WriteLine("-o %Path%\tSpecify output directory");
						return 0;
				}
			}

			
			string mFileVerFile=Path.Combine(OutputDir,FileVersionFile);
			string LastOnlineModTime="";

			// Output local version if possible
			string offlineVersion ="";
			if (File.Exists(mFileVerFile))
			{
				long timestamp;
				if (long.TryParse(offlineVersion=File.ReadAllText(mFileVerFile), out timestamp))
					Console.WriteLine("Local D-IDE Version:\t" + DateFromUnixTime(timestamp).ToLocalTime().ToString());
			}
			else 
				Console.WriteLine("No local D-IDE Version found");

			// Get latest online version
			try
			{
				Console.Write("Online D-IDE Version:\t");
				LastOnlineModTime = new WebClient().DownloadString(TimeStampUrl);

				long timestamp;
				if (long.TryParse(LastOnlineModTime, out timestamp))
					Console.WriteLine(DateFromUnixTime(timestamp).ToLocalTime().ToString());
				else
					Console.WriteLine(LastOnlineModTime);

				// Check if offline version is already the latest
				if (offlineVersion == LastOnlineModTime)
				{
					Console.WriteLine("You already have got the latest version!");
					if (StartImmediately)
						TryOpenDIDE();
					else if (!DontAsk)
						Console.ReadKey();

					return 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 1;
			}

			// Check if d-ide instances are running
			if (!CheckForOpenInstances())
				return 1;

			// Get the latest build archive
			string archive = "";
			if (DownloadLatestBuild(ArchiveUrl, out archive) && CheckForOpenInstances() && ExtractFiles(archive, OutputDir))
			{
				Console.WriteLine();
				Console.WriteLine("Download successful!");

				// Save archive modification time to make later watch-outs for program updates working
				// Note: Just save it after the update was successful!
				File.WriteAllText(mFileVerFile, LastOnlineModTime);

				// Start program if wanted
				if (StartImmediately)
					TryOpenDIDE();
				
				return 0;
			}

			if(!DontAsk)
				Console.ReadKey(); // Halt on error
			return 1;
		}

		public static void TryOpenDIDE()
		{
			var dide = OutputDir + "\\d-ide.exe";
			try
			{
				Console.WriteLine("Launch "+dide);
				Process.Start(dide);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
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
			Console.WriteLine("Download archive");
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
				Console.WriteLine("Extract temp archive to " + OutputDirectory + "\r\n");
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

		public static DateTime DateFromUnixTime(long t)
		{
			var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return ret.AddSeconds(t);
		}
		#endregion
	}
}
