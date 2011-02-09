using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace D_IDE.Updater
{
	/// <summary>
	/// Initializes/Updates the current D-IDE installation 
	/// </summary>
	public class IDEUpdater
	{
		public static string ArchiveUrl = "http://d-ide.svn.sourceforge.net/viewvc/d-ide/d-ide.zip";
		public static string OutputDir = Directory.GetCurrentDirectory();

		[STAThread]
		static void Main(string[] args)
		{
			// Get the latest build archive
			string archive = "";
			if (!DownloadLatestBuild(ArchiveUrl,out archive))
				return;

			if (!ExtractFiles(archive, OutputDir))
				return;

			Console.WriteLine("Download successful!");
		}

		public static bool DownloadLatestBuild(string ArchiveUrl,out string TempFile)
		{
			TempFile = Path.GetTempFileName();
			Console.WriteLine("Download "+ArchiveUrl);
			try
			{
				new WebClient().DownloadFile(ArchiveUrl, TempFile);
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
	}
}
