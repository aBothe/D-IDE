using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DIDE.Installer
{
    public class InstallerHelper
    {
        public static string GetLatestDMD1Url()
        {
            int ver;
            return DigitalMars.GetLatestDMDInfo(1, out ver);
        }

        public static string GetLatestDMD2Url()
        {
            int ver;
            return DigitalMars.GetLatestDMDInfo(2, out ver);
        }

        public static int GetLatestDMD1Version()
        {
            int ver;
            DigitalMars.GetLatestDMDInfo(1, out ver);
            return ver;
        }

        public static int GetLatestDMD2Version()
        {
            int ver;
            DigitalMars.GetLatestDMDInfo(2, out ver);
            return ver;
        }
    }
}
