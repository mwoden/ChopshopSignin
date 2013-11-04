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
            set { m_TotalTimeUpdateInterval = value; FirePropertyChanged("TotalTimeUpdateInterval"); }
        }

        public int ScanInTimeoutWindow
        {
            get { return m_ScanInTimeoutWindow; }
            set { m_ScanInTimeoutWindow = value; FirePropertyChanged("ScanInTimeoutWindow"); }
        }

        public int ScanDataResetTime
        {
            get { return m_ScanDataResetTime; }
            set { m_ScanDataResetTime = value; FirePropertyChanged("ScanDataResetTime"); }
        }

        public int ClearScanStatusTime
        {
            get { return m_ClearScanStatusTime; }
            set { m_ClearScanStatusTime = value; FirePropertyChanged("ClearScanStatusTime"); }
        }

        public int MaxBackupFilesToKeep
        {
            get { return m_MaxBackupFilesToKeep; }
            set { m_MaxBackupFilesToKeep = value; FirePropertyChanged("MaxBackupFilesToKeep"); }
        }

        public bool ShowTimeUntilShip
        {
            get { return m_ShowTimeUntilShip; }
            set { m_ShowTimeUntilShip = value; FirePropertyChanged("ShowTimeUntilShip"); }
        }

        public bool CreateSummaryOnExit
        {
            get { return m_CreateSummaryOnExit; }
            set { m_CreateSummaryOnExit = value; FirePropertyChanged("CreateSummaryOnExit"); }
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
        }

        public void Save()
        {
            bool saveSettings = false;

            if (TotalTimeUpdateInterval != settings.TotalTimeUpdateInterval)
            {
                settings.TotalTimeUpdateInterval = TotalTimeUpdateInterval;
                saveSettings = true;
            }

            if (ScanInTimeoutWindow != settings.ScanInTimeoutWindow)
            {
                settings.ScanInTimeoutWindow = ScanInTimeoutWindow;
                saveSettings = true;
            }

            if (ScanInTimeoutWindow != settings.ScanInTimeoutWindow)
            {
                settings.ScanInTimeoutWindow = ScanInTimeoutWindow;
                saveSettings = true;
            }

            if (ScanDataResetTime != settings.ScanDataResetTime)
            {
                settings.ScanDataResetTime = ScanDataResetTime;
                saveSettings = true;
            }

            if (ClearScanStatusTime != settings.ClearScanStatusTime)
            {
                settings.ClearScanStatusTime = ClearScanStatusTime;
                saveSettings = true;
            }

            if (MaxBackupFilesToKeep != settings.MaxBackupFilesToKeep)
            {
                settings.MaxBackupFilesToKeep = MaxBackupFilesToKeep;
                saveSettings = true;
            }

            if (ShowTimeUntilShip != settings.ShowTimeUntilShip)
            {
                settings.ShowTimeUntilShip = ShowTimeUntilShip;
                saveSettings = true;
            }

            if (CreateSummaryOnExit != settings.CreateSummaryOnExit)
            {
                settings.CreateSummaryOnExit = CreateSummaryOnExit;
                saveSettings = true;
            }

            if (saveSettings)
                settings.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private int m_TotalTimeUpdateInterval;
        private int m_ScanInTimeoutWindow;
        private int m_ScanDataResetTime;
        private int m_ClearScanStatusTime;
        private int m_MaxBackupFilesToKeep;
        private bool m_ShowTimeUntilShip;
        private bool m_CreateSummaryOnExit;

        private Properties.Settings settings;

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
