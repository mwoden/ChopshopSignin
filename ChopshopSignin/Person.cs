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
                if (Timestamps.Count() == 0) return Scan.LocationType.Out;
                return Timestamps.Last().Direction;
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
            if (scanData.ToUpperInvariant().Contains('-'))
            {
                scanData = scanData.Split('-').Last().Trim();
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

        public SignInOutResult Sign(bool signingIn)
        {
            if (signingIn)
                return SignIn();

            return SignOut();
        }

        public XElement ToXml()
        {
            return new XElement("Person",
                    new XAttribute("firstName", FirstName),
                    new XAttribute("lastName", LastName),
                    new XAttribute("role", Role),
                    new XElement("Scans", Timestamps.Select(x => x.ToXml())));
        }

        public static void Save(IEnumerable<Person> people, string filePath)
        {
            new XElement("SignInList", people.Select(x => x.ToXml())).Save(filePath);
        }

        public static IEnumerable<Person> Load(string filePath)
        {
            return XElement.Load(filePath).Elements().Select(x => new Person(x));
        }


        private SignInOutResult SignIn()
        {
            if (CurrentLocation == Scan.LocationType.Out)
            {
                Timestamps.Add(new Scan(true));

                var statusMessage = FullName + " IN at " + Timestamps.Last().ScanTime.ToLongTimeString();
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

                var statusMessage = FullName + " OUT at " + Timestamps.Last().ScanTime.ToLongTimeString();
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed out, scan \"IN\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }
    }
}