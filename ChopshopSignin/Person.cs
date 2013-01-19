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

        public static void Save(IEnumerable<Person> people, string filePath)
        {
            new XElement("SignInList", people.OrderBy(x => x.Role).ThenBy(x => x.FullName).Select(x => x.ToXml())).Save(filePath);
        }

        public static IEnumerable<Person> Load(string filePath)
        {
            if (System.IO.File.Exists(filePath))
                return XElement.Load(filePath).Elements().Select(x => new Person(x));

            return Enumerable.Empty<Person>();
        }

        public IEnumerable<WeekSummary> GetWeekSummaries(DateTime kickoffData)
        {
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
                var temp = new WeekSummary(week.Week, FullName, rows.Where(x => x.Any()).Select(x => string.Join(",", new[] { FullName }.Concat(x))).ToArray());
                weekSummaries.Add(temp);
            }

            return weekSummaries;
        }

        private SignInOutResult SignIn()
        {
            if (CurrentLocation == Scan.LocationType.Out)
            {
                Timestamps.Add(new Scan(true));

                var statusMessage = FullName + " in at " + Timestamps.Last().ScanTime.ToLongTimeString();
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed in, scan \"OUT\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

        private SignInOutResult SignOut()
        {
            if (CurrentLocation == Scan.LocationType.In)
            {
                Timestamps.Add(new Scan(false));

                var statusMessage = FullName + " out at " + Timestamps.Last().ScanTime.ToLongTimeString();
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed out, scan \"IN\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

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

        private const string MentorId = "MENTOR  ";
    }

    /// <summary>
    /// Holds the in/out scan pair
    /// </summary>
    class SignInPair
    {
        public DateTime? In { get; private set; }
        public DateTime? Out { get; private set; }

        private SignInPair() { }

        public SignInPair(IEnumerable<Scan> timeStamps)
        {
            var scanCount = timeStamps.Count();
            System.Diagnostics.Debug.Assert(scanCount == 1 || scanCount == 2);

            In = timeStamps.First().ScanTime;
            if (scanCount == 2)
                Out = timeStamps.Last().ScanTime;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}",
                In == null ? string.Empty : ((DateTime)In).ToShortTimeString(),
                Out == null ? string.Empty : ((DateTime)Out).ToShortTimeString());
        }

        public string GetCsvString()
        {
            return string.Format("{0},{1}",
                In == null ? string.Empty : ((DateTime)In).ToShortTimeString(),
                Out == null ? string.Empty : ((DateTime)Out).ToShortTimeString());
        }

        public static IDictionary<DayOfWeek, SignInPair[]> GetWeekInOutPairs(IEnumerable<Scan> timeStamps)
        {
            System.Diagnostics.Debug.Assert(timeStamps.First().Direction == Scan.LocationType.In);

            var scans = timeStamps.GroupBy(x => x.ScanTime.DayOfWeek)
                                  .Select(x => new { Day = x.Key, Pairs = GetDayPairs(x) })
                                  .ToArray();

            var daysMissing = FirstWeek.Except(scans.Select(x => x.Day)).Select(x => new { Day = x, Pairs = Enumerable.Empty<SignInPair>().ToArray() }).ToArray();
            var max = scans.Max(x => x.Pairs.Count());

            return scans.Concat(daysMissing).ToDictionary(x => x.Day, x => x.Pairs.Concat(Enumerable.Repeat<SignInPair>(new SignInPair(), max))
                                                                                 .Take(max)
                                                                                 .ToArray());
            //    return timeStamps.GroupBy(x => x.ScanTime.DayOfWeek)
            //                     .Select(x => new { Day = x.Key, Pairs = GetDayPairs(x) })
            //                     .ToDictionary(x => x.Day, x => x.Pairs.ToArray());
        }

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

        private static SignInPair[] GetDayPairs(IEnumerable<Scan> times)
        {
            if (times.First().Direction != Scan.LocationType.In)
                System.Diagnostics.Debugger.Break();

            System.Diagnostics.Debug.Assert(times.First().Direction == Scan.LocationType.In);

            return Enumerable.Range(0, times.Count())
                             .GroupBy(x => x / 2, x => times.ElementAt(x))
                             .Select(x => new SignInPair(x))
                             .ToArray();
        }
    }
}