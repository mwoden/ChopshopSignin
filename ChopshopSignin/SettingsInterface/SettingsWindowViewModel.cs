using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ChopshopSignin
{
    class SettingsWindowViewModel : INotifyPropertyChanged
    {
        public int TotalTimeUpdateInterval
        {
            get { return m_TotalTimeUpdateInterval; }
            set { m_TotalTimeUpdateInterval = value; FirePropertyChanged("TotalTimeUpdateInterval"); IsDirty = true; }
        }

        public int ScanInTimeoutWindow
        {
            get { return m_ScanInTimeoutWindow; }
            set { m_ScanInTimeoutWindow = value; FirePropertyChanged("ScanInTimeoutWindow"); IsDirty = true; }
        }

        public int ScanDataResetTime
        {
            get { return m_ScanDataResetTime; }
            set { m_ScanDataResetTime = value; FirePropertyChanged("ScanDataResetTime"); IsDirty = true; }
        }

        public int ClearScanStatusTime
        {
            get { return m_ClearScanStatusTime; }
            set { m_ClearScanStatusTime = value; FirePropertyChanged("ClearScanStatusTime"); IsDirty = true; }
        }

        public int MaxBackupFilesToKeep
        {
            get { return m_MaxBackupFilesToKeep; }
            set { m_MaxBackupFilesToKeep = value; FirePropertyChanged("MaxBackupFilesToKeep"); IsDirty = true; }
        }

        public bool ShowTimeUntilShip
        {
            get { return m_ShowTimeUntilShip; }
            set { m_ShowTimeUntilShip = value; FirePropertyChanged("ShowTimeUntilShip"); IsDirty = true; }
        }

        public bool CreateSummaryOnExit
        {
            get { return m_CreateSummaryOnExit; }
            set { m_CreateSummaryOnExit = value; FirePropertyChanged("CreateSummaryOnExit"); IsDirty = true; }
        }

        public DateTime Kickoff
        {
            get { return m_Kickoff; }
            set { m_Kickoff = value; FirePropertyChanged("Kickoff"); IsDirty = true; }
        }

        public DateTime Ship
        {
            get { return m_Ship; }
            set { m_Ship = value; FirePropertyChanged("Ship"); IsDirty = true; }
        }

        public DateTime TimeSince
        {
            get { return m_TimeSince; }
            set { m_TimeSince = value; FirePropertyChanged("TimeSince"); IsDirty = true; }
        }

        public bool IsDirty
        {
            get { return m_IsDirty; }
            private set { m_IsDirty = value; FirePropertyChanged("IsDirty"); }
        }

        public SettingsWindowViewModel(Properties.Settings currentSettings)
        {
            settings = currentSettings;

            TotalTimeUpdateInterval = settings.TotalTimeUpdateInterval;
            ScanInTimeoutWindow = settings.ScanInTimeoutWindow;
            ScanDataResetTime = settings.ScanDataResetTime;
            ClearScanStatusTime = settings.ClearScanStatusTime;
            MaxBackupFilesToKeep = settings.MaxBackupFilesToKeep;
            ShowTimeUntilShip = settings.ShowTimeUntilShip;
            CreateSummaryOnExit = settings.CreateSummaryOnExit;

            if (settings.Kickoff == DateTime.MinValue)
                Kickoff = new DateTime(DateTime.Now.Year, 1, 1);
            else
                Kickoff = settings.Kickoff;

            if (settings.Ship == DateTime.MinValue)
                Ship = new DateTime(DateTime.Now.Year, 2, 1);
            else
                Ship = settings.Ship;

            if (settings.TimeSince == DateTime.MinValue)
                TimeSince = Kickoff;
            else
                TimeSince = settings.TimeSince;

            IsDirty = false;
        }

        public void Save()
        {
            if (IsDirty)
            {
                settings.TotalTimeUpdateInterval = TotalTimeUpdateInterval;
                settings.ScanInTimeoutWindow = ScanInTimeoutWindow;
                settings.ScanDataResetTime = ScanDataResetTime;
                settings.ClearScanStatusTime = ClearScanStatusTime;
                settings.MaxBackupFilesToKeep = MaxBackupFilesToKeep;
                settings.ShowTimeUntilShip = ShowTimeUntilShip;
                settings.CreateSummaryOnExit = CreateSummaryOnExit;
                settings.Kickoff = Kickoff;
                settings.Ship = Ship;
                settings.TimeSince = TimeSince;

                settings.Save();
                IsDirty = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private int m_TotalTimeUpdateInterval;
        private int m_ScanInTimeoutWindow;
        private int m_ScanDataResetTime;
        private int m_ClearScanStatusTime;
        private int m_MaxBackupFilesToKeep;
        private bool m_ShowTimeUntilShip;
        private bool m_CreateSummaryOnExit;
        private DateTime m_Kickoff;
        private DateTime m_Ship;
        private DateTime m_TimeSince;

        private bool m_IsDirty;

        private Properties.Settings settings;

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
