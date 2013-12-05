using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Timers;

namespace ChopshopSignin
{
    sealed internal class ViewModel : ViewModelBase
    {
        /// <summary>
        /// The scan status text to display
        /// </summary>
        public string ScanStatus
        {
            get { lock (syncObject) { return m_LastScan; } }
            set { lock (syncObject) { m_LastScan = value; eventList.Set(EventList.Event.ClearDisplayStatus, clearStatusTime); FirePropertyChanged(); } }
        }

        /// <summary>
        /// The formatted string for the current time
        /// </summary>
        public string CurrentTimeString
        {
            get { lock (syncObject) { return CurrentTime.ToString("ddd MMM d, yyyy") + Environment.NewLine + CurrentTime.ToLongTimeString(); } }
        }

        /// <summary>
        /// The current time to display
        /// </summary>
        public DateTime CurrentTime
        {
            get { lock (syncObject) { return m_CurrentTime; } }
            set { lock (syncObject) { m_CurrentTime = value; FirePropertyChanged(); FirePropertyChanged("CurrentTimeString"); } }
        }

        /// <summary>
        /// The number of signed in students
        /// </summary>
        public int StudentsSignedIn
        {
            get { lock (syncObject) { return m_StudentsSignedIn; } }
            set { lock (syncObject) { m_StudentsSignedIn = value; FirePropertyChanged(); } }
        }

        /// <summary>
        /// The number of signed in mentorss
        /// </summary>
        public int MentorsSignedIn
        {
            get { lock (syncObject) { return m_MentorsSignedIn; } }
            set { lock (syncObject) { m_MentorsSignedIn = value; FirePropertyChanged(); } }
        }

        /// <summary>
        /// Listing of everyone who is currently checked in
        /// </summary>
        public ObservableCollection<Person> CheckedIn
        {
            get { return m_CheckedIn; }
            set { m_CheckedIn = value; FirePropertyChanged(); }
        }

        /// <summary>
        /// The total time spent by people at FIRST
        /// </summary>
        public TimeSpan TotalTime
        {
            get { return m_TotalTime; }
            set { m_TotalTime = value; FirePropertyChanged(); FirePropertyChanged("TotalTimeString"); }
        }

        /// <summary>
        /// Formatted string for displaying total time spent at FIRST
        /// </summary>
        public string TotalTimeString
        {
            get { return string.Format("{0:F0} days, {1:F0} hours, {2:F0} minutes", TotalTime.Days, TotalTime.Hours, TotalTime.Minutes); }
        }

        /// <summary>
        /// The header for time spent, with the starting date
        /// </summary>
        public string TimeSpentHeader
        {
            get
            {
                if (Properties.Settings.Default.TimeSince == DateTime.MinValue)
                    return "Time spent";

                return string.Format("Time spent since {0}", Properties.Settings.Default.TimeSince.ToShortDateString());
            }
        }

        /// <summary>
        /// The date that the robot must be packed up
        /// </summary>
        public DateTime ShipDate
        {
            get { return Utility.Ship; }
        }

        /// <summary>
        /// Formatted value for time left until ship
        /// </summary>
        public string TimeUntilShip
        {
            get { return m_TimeUntilShip; }
            set { m_TimeUntilShip = value; FirePropertyChanged(); }
        }

        /// <summary>
        /// Determine whether to show the time left until ship or not
        /// </summary>
        public bool ShowTimeUntilShip
        {
            get { return Properties.Settings.Default.ShowTimeUntilShip; }
        }

        /// <summary>
        /// Update the checked in list from a list of people
        /// </summary>
        /// <param name="people">The list of people</param>
        public void UpdateCheckedInList(IEnumerable<Person> people)
        {
            // Update the checked in observable
            CheckedIn = new ObservableCollection<Person>(people.Where(x => x.CurrentLocation == Scan.LocationType.In));

            // Get the count of each type
            StudentsSignedIn = CheckedIn.Count(x => x.Role == Person.RoleType.Student);
            MentorsSignedIn = CheckedIn.Count(x => x.Role == Person.RoleType.Mentor);
        }

        public ViewModel()
        {
            eventList = new EventList();

            clearStatusTime = TimeSpan.FromSeconds(Properties.Settings.Default.ClearScanStatusTime);

            timer = new Timer(timerInterval);
            timer.Elapsed += ClockTick;
            timer.Enabled = true;

            Properties.Settings.Default.PropertyChanged += SettingChanged;
        }

        void SettingChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ClearScanStatusTime":
                    clearStatusTime = TimeSpan.FromSeconds(Properties.Settings.Default.ClearScanStatusTime);
                    break;
                case "TimeSince":
                    FirePropertyChanged("TimeSpentHeader");
                    break;
            }
            FirePropertyChanged(null);
        }

        private object syncObject = new object();

        private string m_LastScan = string.Empty;
        private DateTime m_CurrentTime = DateTime.Now;
        private int m_StudentsSignedIn;
        private int m_MentorsSignedIn;
        private ObservableCollection<Person> m_CheckedIn = new ObservableCollection<Person>();
        private TimeSpan m_TotalTime = TimeSpan.Zero;
        private DateTime m_StartTime = DateTime.Now;
        private DateTime m_ShipDate = DateTime.MinValue;
        private string m_TimeUntilShip = string.Empty;

        private const int timerInterval = 200;
        private Timer timer;

        private EventList eventList;

        // Time that a status message will be displayed
        private TimeSpan clearStatusTime = TimeSpan.FromSeconds(Properties.Settings.Default.ClearScanStatusTime);

        /// <summary>
        /// Timer handler for clearing the status
        /// </summary>
        private void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            CurrentTime = e.SignalTime;

            if (ShowTimeUntilShip && ShipDate > e.SignalTime)
                TimeUntilShip = (ShipDate - DateTime.Now).ToString(@"dd\.hh\:mm\:ss");


            if (eventList.HasExpired(EventList.Event.ClearDisplayStatus, e.SignalTime))
            {
                ScanStatus = string.Empty;
                eventList.Clear(EventList.Event.ClearDisplayStatus);
            }
        }

        public void Dispose()
        {            
            Dispose(true);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    timer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }

    }
}
