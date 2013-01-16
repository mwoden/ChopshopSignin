using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ChopshopSignin
{
    class ViewModel : INotifyPropertyChanged
    {
        public string ScanStatus
        {
            get { lock (syncObject) { return m_LastScan; } }
            set { lock (syncObject) { m_LastScan = value; FirePropertyChanged("ScanStatus"); } }
        }

        public string CurrentTimeString
        {
            get { lock (syncObject) { return CurrentTime.ToString("ddd MMM d, yyyy") + Environment.NewLine + CurrentTime.ToLongTimeString(); } }
        }

        public DateTime CurrentTime
        {
            get { lock (syncObject) { return m_CurrentTime; } }
            set { lock (syncObject) { m_CurrentTime = value; FirePropertyChanged("CurrentTime"); FirePropertyChanged("CurrentTimeString"); } }
        }

        public IEnumerable<string> CheckedInList
        {
            set
            {
                CheckedInDisplayList = new ObservableCollection<string>(value);
                FirePropertyChanged("StudentListHeader");
            }
        }

        public ObservableCollection<string> CheckedInDisplayList
        {
            get { lock (syncObject) { return m_StudentsIn; } }
            private set { lock (syncObject) { m_StudentsIn = value; FirePropertyChanged("CheckedInDisplayList"); } }
        }

        public string StudentListHeader
        {
            get { lock (syncObject) { return string.Format("Students Signed In ({0})", CheckedInDisplayList.Count()); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private object syncObject = new object();

        private string m_LastScan = string.Empty;
        private DateTime m_CurrentTime = DateTime.Now;
        private ObservableCollection<string> m_StudentsIn = new ObservableCollection<string>();

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
