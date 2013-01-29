using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ChopshopSignin
{
    sealed class Settings
    {
        /// <summary>
        /// The instance of the singleton Settings
        /// </summary>
        public static Settings Instance { get { return lazy.Value; } }

        /// <summary>
        /// The day that the season starts (typically the first Saturday in January)
        /// </summary>
        public DateTime Kickoff { get; private set; }

        /// <summary>
        /// The day that the robot has to be finished (Midnight, the Wednesday after the last full week)
        /// </summary>
        public DateTime Ship { get; private set; }

        /// <summary>
        /// The length of time (in seconds) between total time spent updates
        /// </summary>
        public int TotalTimeUpdateInterval { get; private set; }

        /// <summary>
        /// The length of time (in seconds) before requiring someone to scan their name again
        /// </summary>
        public int ScanInTimeoutWindow { get; private set; }

        /// <summary>
        /// The length of time (in seconds) before the current scan will be cleared unless a '\r' is entered
        /// </summary>
        public int ScanDataResetTime { get; private set; }

        /// <summary>
        /// The length of time (in seconds) before the scan status string will be cleared
        /// </summary>
        public int ClearScanStatusTime { get; private set; }

        /// <summary>
        /// Choose whether or not to show time remaining until ship
        /// </summary>
        public bool ShowTimeUntilShip { get; private set; }

        /// <summary>
        /// Static member holding the single instance
        /// </summary>
        private static readonly Lazy<Settings> lazy = new Lazy<Settings>(() => new Settings(System.Reflection.Assembly.GetExecutingAssembly().Location, Properties.Resources.SettingsFileName));

        /// <summary>
        /// Constructor to set all to default
        /// </summary>
        private Settings()
        {
            Kickoff = Defaults.Kickoff;
            Ship = Defaults.Ship;
            TotalTimeUpdateInterval = Defaults.TotalTimeUpdateInterval;
            ScanInTimeoutWindow = Defaults.ScanInTimeoutWindow;
            ScanDataResetTime = Defaults.ScanDataResetTime;
            ClearScanStatusTime = Defaults.ClearScanStatusTime;
            ShowTimeUntilShip = Defaults.ShowTimeUntilShip;
        }


        /// <summary>
        /// Constructor to load settings from a settings file
        /// </summary>
        /// <param name="executingAssembly">The location returned by System.Reflection.Assembly.GetExecutingAssembly().Location</param>
        /// <param name="settingsFile">The settings file name</param>
        private Settings(string executingAssembly, string settingsFile)
            : this()
        {
            var folder = System.IO.Path.GetDirectoryName(executingAssembly);
            var file = System.IO.Path.Combine(folder, settingsFile);

            if (System.IO.File.Exists(file))
            {
                var settingsData = XElement.Load(file);

                try
                {
                    Kickoff = (DateTime?)settingsData.Element("Kickoff") ?? Defaults.Kickoff;
                    Ship = (DateTime?)settingsData.Element("Ship") ?? Defaults.Ship;
                    TotalTimeUpdateInterval = (int?)settingsData.Element("TotalTimeUpdateInterval") ?? Defaults.TotalTimeUpdateInterval;
                    ScanInTimeoutWindow = (int?)settingsData.Element("ScanInTimeoutWindow") ?? Defaults.ScanInTimeoutWindow;
                    ScanDataResetTime = (int?)settingsData.Element("ScanDataResetTime") ?? Defaults.ScanDataResetTime;
                    ClearScanStatusTime = (int?)settingsData.Element("ClearScanStatusTime") ?? Defaults.ClearScanStatusTime;
                    ShowTimeUntilShip = (bool?)settingsData.Element("ShowTimeUntilShip") ?? Defaults.ShowTimeUntilShip;
                }
                catch (FormatException)
                {
                    System.Windows.MessageBox.Show("There was an error in the settings file. Please fix it and run the program again", "Error Reading Settings File", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }


        /// <summary>
        /// Number of full weeks in an FRC season
        /// </summary>
        private const int SeasonWeeks = 6;

        /// <summary>
        /// Default values for the adjustable paramters
        /// </summary>
        private static class Defaults
        {
            // Set kickoff to default to the first Saturday of the year
            public static DateTime Kickoff { get { return Enumerable.Range(1, 7).Select(x => new DateTime(DateTime.Today.Year, 1, x)).Single(x => x.DayOfWeek == DayOfWeek.Saturday); } }

            // Set the ship default to the first Wednesday that occurs 6 weeks after kickoff
            public static DateTime Ship { get { return Enumerable.Range(1, 7).Select(x => Kickoff.AddDays(SeasonWeeks * 7).AddDays(x)).Single(s => s.DayOfWeek == DayOfWeek.Wednesday); } }

            // Set the default total time update interval to 5 minutes (300 seconds)
            public const int TotalTimeUpdateInterval = 300;

            // Set the default scan in window to 10 seconds
            public const int ScanInTimeoutWindow = 10;

            // Set the default scan data reset time to 3 seconds
            public const int ScanDataResetTime = 3;

            // Set the default clear scan status time to 60 seconds
            public const int ClearScanStatusTime = 60;

            // Set the default to display the time left until ship
            public const bool ShowTimeUntilShip = true;
        }
    }
}