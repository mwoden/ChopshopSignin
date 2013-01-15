using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ChopshopSignin
{
    class Scan : IEquatable<Scan>
    {
        public enum ScanDirection { Invalid, In, Out }
        public enum RoleType { Invalid, Student, Mentor }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName { get { return LastName + ", " + FirstName; } }
        public ScanDirection Direction { get; private set; }
        public RoleType Role { get; private set; }
        public DateTime ScanTime { get; private set; }

        private Scan()
        {
            ScanTime = DateTime.Now;
        }

        public Scan(string combinedName, bool scannedIn, RoleType role)
            : this()
        {
            var names = combinedName.Split(',');
            FirstName = names.Last().Trim();
            LastName = names.First().Trim();
            Role = role;
            Direction = scannedIn ? ScanDirection.In : ScanDirection.Out;
        }

        private Scan(XElement xmlScan)
        {
            FirstName = (string)xmlScan.Attribute("firstName");
            LastName = (string)xmlScan.Attribute("lastName");
            ScanTime = (DateTime)xmlScan.Attribute("timestamp");

            Direction = ParseEnum<ScanDirection>((string)xmlScan.Attribute("direction"));
            Role = ParseEnum<RoleType>((string)xmlScan.Attribute("role") ?? "student");
        }

        public static void SaveScans(IEnumerable<Scan> scans, string file)
        {
            new XElement("Scans", scans.Select(x => x.ToXml())).Save(file);
        }

        public static IEnumerable<Scan> LoadScans(string file)
        {
            return XElement.Load(file)
                           .Elements()
                           .Select(x => new Scan(x));
        }

        public override string ToString()
        {
            return string.Format(FormatString, LastName, FirstName, Direction.ToString(), ScanTime);
        }

        private XElement ToXml()
        {
            return new XElement("Scan",
                        new XAttribute("lastName", LastName),
                        new XAttribute("firstName", FirstName),
                        new XAttribute("direction", Direction),
                        new XAttribute("role", Role),
                        new XAttribute("timestamp", ScanTime));
        }

        private const string FormatString = "{0,-20}{1,-20}{2,-10}{3}";

        public override int GetHashCode()
        {
            return LastName.GetHashCode() + 13 * FirstName.GetHashCode() + 23 * ScanTime.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Scan);
        }

        public bool Equals(Scan other)
        {
            if (object.ReferenceEquals(this, other)) return true;

            if (other != null)
                if (FirstName == other.FirstName)
                    if (LastName == other.LastName)
                        if (ScanTime == other.ScanTime)
                            return true;

            return false;
        }

        private static T ParseEnum<T>(string enumString) where T : struct
        {
            T result;
            if (Enum.TryParse(enumString, true, out result))
                return result;

            return default(T);
        }
    }
}
