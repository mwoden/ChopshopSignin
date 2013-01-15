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
        public string LastScan
        {
            get { lock (syncObject) { return m_LastScan; } }
            set { lock (syncObject) { m_LastScan = value; FirePropertyChanged("LastScan"); } }
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

        public ObservableCollection<string> StudentsIn
        {
            get {lock (syncObject){ return m_StudentsIn; }}
            set { lock (syncObject) { m_StudentsIn = value; FirePropertyChanged("StudentsIn"); } }
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
