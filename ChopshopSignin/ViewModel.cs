using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ChopshopSignin
{
    sealed internal class ViewModel : INotifyPropertyChanged
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


        public void UpdateCheckedInList(IEnumerable<Person> people)
        {
            // Update the checked in observable
            CheckedIn = new ObservableCollection<Person>(people.Where(x => x.CurrentLocation == Scan.LocationType.In));

            // Get the count of each type
            var studentCount = CheckedIn.Count(x => x.Role == Person.RoleType.Student);
            var mentorCount = CheckedIn.Count(x => x.Role == Person.RoleType.Mentor);

            // Generate the new headers
            StudentListHeader = string.Format("Students Signed In ({0})", studentCount);
            MentorListHeader = string.Format("Mentors Signed In ({0})", mentorCount);
}

        public string StudentListHeader
        {
            get { lock (syncObject) { return m_StudentListHeader; } }
            set { lock (syncObject) { m_StudentListHeader = value; FirePropertyChanged("StudentListHeader"); } }
        }

        public string MentorListHeader
        {
            get { lock (syncObject) { return m_MentorListHeader; } }
            set { lock (syncObject) { m_MentorListHeader = value; FirePropertyChanged("MentorListHeader"); } }
        }

        public ObservableCollection<Person> CheckedIn
        {
            get { return m_CheckedIn; }
            set { m_CheckedIn = value; FirePropertyChanged("CheckedIn"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private object syncObject = new object();

        private string m_LastScan = string.Empty;
        private DateTime m_CurrentTime = DateTime.Now;
        private string m_StudentListHeader = string.Empty;
        private string m_MentorListHeader = string.Empty;
        private ObservableCollection<Person> m_CheckedIn = new ObservableCollection<Person>();

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
