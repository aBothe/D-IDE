using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace DIDE.Installer
{
    internal class DigitalMars
    {
        private const string ERROR_PREFIX = "[ERROR] ";
        private const string DIGITAL_MARS_FTP = "ftp://ftp.digitalmars.com/";
        private const string DIGITAL_MARS_HTTP = "http://ftp.digitalmars.com/";
        private const string DIGITAL_MARS_DMD_1 = "ftp://ftp.digitalmars.com/dmd.1.056.zip";
        private const string DIGITAL_MARS_DMD_2 = "ftp://ftp.digitalmars.com/dmd.2.041.zip";

        public static string GetLatestDMDInfo(int version, out int subVersion)
        {
            string url = (version == 1) ? DIGITAL_MARS_DMD_1 : DIGITAL_MARS_DMD_2;
            int latestVersion = (version == 1) ? 56 : 41;
            subVersion = latestVersion;
            try
            {
                FtpWebRequest request = WebRequest.Create(DIGITAL_MARS_FTP) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                using (FtpWebResponse response = request.GetResponse() as FtpWebResponse)
                {
                    Console.WriteLine(response.StatusDescription);
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (line.StartsWith("dmd." + version + "."))
                            {
                                string[] tokens = line.Split('.');
                                if (tokens.Length > 2 && !int.TryParse(tokens[2], out subVersion)) subVersion = latestVersion;

                                if (tokens[tokens.Length - 1].Equals("zip", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (latestVersion < subVersion)
                                    {
                                        latestVersion = subVersion;
                                        url = DIGITAL_MARS_HTTP + line;
                                    }
                                }
                            }
                        }
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                url = ERROR_PREFIX + ex.Message + Environment.NewLine + ex.StackTrace;
            }

            return url;
        }
    }
}
