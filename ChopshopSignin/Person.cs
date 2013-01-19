using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ChopshopSignin
{
    class Person : IEquatable<Person>
    {
        public enum RoleType { Invalid, Student, Mentor }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName { get { return LastName + ", " + FirstName; } }

        /// <summary>
        /// Returns the most recent location from today's timestamps
        /// </summary>
        public Scan.LocationType CurrentLocation
        {
            get
            {
                var today = Timestamps.Where(x => x.ScanTime.Date == DateTime.Today);
                if (today.Count() == 0)
                    return Scan.LocationType.Out;

                return today.Last().Direction;
            }
        }
        public RoleType Role { get; private set; }

        public List<Scan> Timestamps { get; private set; }

        private Person()
        {
            Timestamps = new List<Scan>();
        }

        private Person(string lastName, string firstName, RoleType role)
            : this()
        {
            Role = role;
            LastName = lastName.Trim();
            FirstName = firstName.Trim();
        }

        private Person(string lastName, string firstName, bool isMentor)
            : this(lastName, firstName, isMentor ? RoleType.Mentor : RoleType.Student)
        { }

        public Person(XElement personXml)
        {
            FirstName = (string)personXml.Attribute("firstName");
            LastName = (string)personXml.Attribute("lastName");

            RoleType result;

            if (!Enum.TryParse<RoleType>((string)personXml.Attribute("role"), true, out result))
                result = RoleType.Student;

            Role = result;

            Timestamps = personXml.Element("Scans").Elements().Select(x => new Scan(x)).OrderBy(x => x.ScanTime).ToList();
        }

        public static Person Create(string scanData)
        {
            if (string.IsNullOrWhiteSpace(scanData) || !scanData.Contains(','))
                return null;

            RoleType role = Person.RoleType.Student;

            // Check for a mentor scan
            if (scanData.ToUpperInvariant().StartsWith(MentorId))
            {
                scanData = scanData.Substring(MentorId.Length).Trim();
                //scanData = scanData.Split(':').Last().Trim();

                role = Person.RoleType.Mentor;
            }

            return new Person(scanData.Split(',').First(), scanData.Split(',').Last(), role);
        }

        public bool Equals(Person other)
        {
            return FullName.Equals(other.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Person);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} : {1} : {2}", FullName, Role.ToString(), CurrentLocation.ToString());
        }

        /// <summary>
        /// Signs a person in or out, and returns an corresponding sign in/out result
        /// </summary>
        /// <param name="signingIn">If true, indicates the person is signing in</param>
        /// <returns>The result of the operatoin</returns>
        public SignInOutResult SignInOrOut(bool signingIn)
        {
            if (signingIn)
                return SignIn();

            return SignOut();
        }

        public XElement ToXml()
        {
            return new XElement("Person",
                    new XAttribute("lastName", LastName),
                    new XAttribute("firstName", FirstName),
                    new XAttribute("role", Role),
                    new XElement("Scans", Timestamps.Select(x => x.ToXml())));
        }

        /// <summary>
        /// Saves a list of people to the specified files
        /// Only people with at least 1 timestamp will be saved,
        /// sorted by Student/Mentor, then by name (last name first)
        /// </summary>
        public static void Save(IEnumerable<Person> people, string filePath)
        {
            new XElement("SignInList", people.Where(x => x.Timestamps.Any())
                                             .OrderBy(x => x.Role)
                                             .ThenBy(x => x.FullName)
                                             .Select(x => x.ToXml())).Save(filePath);
        }

        /// <summary>
        /// Load all the people from the file
        /// This will make a backup copy in the 'Backup' folder first
        /// </summary>
        public static IEnumerable<Person> Load(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                BackupDataFile(filePath);
                return XElement.Load(filePath).Elements().Select(x => new Person(x));
            }

            return Enumerable.Empty<Person>();
        }

        /// <summary>
        /// Generates a set of WeekSummary objects to create the csv summary files
        /// </summary>
        /// <param name="kickoffData"></param>
        /// <returns></returns>
        public IEnumerable<WeekSummary> GetWeekSummaries(DateTime kickoffData)
        {
            // Get the person's timestamps and group them by week
            var weeks = Timestamps.GroupBy(x => (((x.ScanTime.Date - kickoffData).Days) / 7) + 1)
                                  .Select(x => new { Week = x.Key, WeekScans = SignInPair.GetWeekInOutPairs(x) })
                                  .ToArray();

            var weekSummaries = new List<WeekSummary>();

            foreach (var week in weeks)
            {
                var maxEntries = week.WeekScans.Values.Max(x => x.Count());
                var rows = Enumerable.Range(0, maxEntries).Select(_ => new List<string>()).ToArray();

                foreach (var index in Enumerable.Range(0, maxEntries))
                {
                    foreach (var day in FirstWeek)
                    {
                        var t = week.WeekScans[day][index];
                        rows[index].Add(t.GetCsvString());
                    }
                }
                var weekSum = new WeekSummary(week.Week, FullName, rows.Where(x => x.Any()).Select(x => string.Join(",", new[] { FullName }.Concat(x))).ToArray());
                weekSummaries.Add(weekSum);
            }

            return weekSummaries;
        }

        /// <summary>
        /// Signs a person in, and returns an corresponding sign in/out result
        /// </summary>
        private SignInOutResult SignIn()
        {
            if (CurrentLocation == Scan.LocationType.Out)
            {
                Timestamps.Add(new Scan(true));

                var statusMessage = string.Format("{0} {1} in at {2}", FirstName, LastName, Timestamps.Last().ScanTime.ToShortDateString());
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed in, scan \"OUT\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

        /// <summary>
        /// Signs a person out, and returns an corresponding sign in/out result
        /// </summary>
        /// <returns></returns>
        private SignInOutResult SignOut()
        {
            if (CurrentLocation == Scan.LocationType.In)
            {
                Timestamps.Add(new Scan(false));

                var statusMessage = string.Format("{0} {1} out at {2}", FirstName, LastName, Timestamps.Last().ScanTime.ToShortDateString());
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed out, scan \"IN\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

        /// <summary>
        /// Make a backup copy of the specified file a 'Backup' subdirectory
        /// The backup file will be prefixed with yyyy-MM-dd HH_mm_ss
        /// </summary>
        private static void BackupDataFile(string originalFilePath)
        {
            if (System.IO.File.Exists(originalFilePath))
            {
                var baseFolder = System.IO.Path.GetDirectoryName(originalFilePath);
                var backupFolder = System.IO.Path.Combine(baseFolder, "Backup");

                if (!System.IO.Directory.Exists(backupFolder))
                    System.IO.Directory.CreateDirectory(backupFolder);

                var backupFile = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + " " + System.IO.Path.GetFileName(originalFilePath);
                var backupFilePath = System.IO.Path.Combine(backupFolder, backupFile);

                System.IO.File.Copy(originalFilePath, backupFilePath);
            }
        }

        /// <summary>
        /// Contains the week defintion, by FIRST season standards
        /// </summary>
        public static readonly DayOfWeek[] FirstWeek = new[] 
        {
            DayOfWeek.Saturday,
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        /// <summary>
        /// This string is prefixed to a person who is a mentor
        /// </summary>
        private const string MentorId = "MENTOR  ";
    }
}