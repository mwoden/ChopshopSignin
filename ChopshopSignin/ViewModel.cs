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

        public void UpdateCheckedInLists(IEnumerable<Person> checkedInList)
        {
            var students = checkedInList.Where(x => x.CurrentLocation == Scan.LocationType.In).Where(x => x.Role == Person.RoleType.Student).Select(x => x.FullName).OrderBy(x => x);
            var mentors = checkedInList.Where(x => x.CurrentLocation == Scan.LocationType.In).Where(x => x.Role == Person.RoleType.Mentor).Select(x => x.FullName).OrderBy(x => x);

            StudentsCheckedInDisplayList = new ObservableCollection<string>(students);
            MentorsCheckedInDisplayList = new ObservableCollection<string>(mentors);
        }

        public ObservableCollection<string> StudentsCheckedInDisplayList
        {
            get { lock (syncObject) { return m_StudentsIn; } }
            private set
            {
                lock (syncObject)
                {
                    m_StudentsIn = value;
                    FirePropertyChanged("StudentsCheckedInDisplayList");
                    FirePropertyChanged("StudentListHeader");
                }
            }
        }

        public string StudentListHeader
        {
            get { lock (syncObject) { return string.Format("Students Signed In ({0})", StudentsCheckedInDisplayList.Count()); } }
        }

        public ObservableCollection<string> MentorsCheckedInDisplayList
        {
            get { lock (syncObject) { return m_MentorsIn; } }
            private set
            {
                lock (syncObject)
                {
                    m_MentorsIn = value;
                    FirePropertyChanged("MentorsCheckedInDisplayList");
                    FirePropertyChanged("MentorListHeader");
                }
            }
        }

        public string MentorListHeader
        {
            get { lock (syncObject) { return string.Format("Mentors Signed In ({0})", MentorsCheckedInDisplayList.Count()); } }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private object syncObject = new object();

        private string m_LastScan = string.Empty;
        private DateTime m_CurrentTime = DateTime.Now;
        private ObservableCollection<string> m_StudentsIn = new ObservableCollection<string>();
        private ObservableCollection<string> m_MentorsIn = new ObservableCollection<string>();

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
