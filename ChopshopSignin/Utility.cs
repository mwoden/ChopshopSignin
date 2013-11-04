using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ChopshopSignin
{
    static class Utility
    {
        public static string OutputFolder
        {
            get { return m_OutputFolder = (m_OutputFolder ?? System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)); }
        }

        public static string DataFile
        {
            get { return System.IO.Path.Combine(OutputFolder, Properties.Settings.Default.ScanDataFileName); }
        }

        public static string BackupFolder
        {
            get { return System.IO.Path.Combine(OutputFolder, Properties.Settings.Default.BackupFolder); }
        }

        public static DateTime Kickoff
        {
            get
            {
                return Enumerable.Range(1, 7)
                                 .Select(x => new DateTime(DateTime.Today.Year, 1, x))
                                 .Single(x => x.DayOfWeek == DayOfWeek.Saturday);
            }
        }

        public static DateTime Ship
        {
            get
            {
                return Enumerable.Range(1, 7)
                                 .Select(x => Kickoff.AddDays(Properties.Settings.Default.SeasonLengthWeeks * 7).AddDays(x))
                                 .Single(s => s.DayOfWeek == DayOfWeek.Wednesday);
            }
        }

        private static string m_OutputFolder;
    }
}