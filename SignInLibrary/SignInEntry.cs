using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SignInLibrary
{
    /// <summary>
    /// Holds an in/out timestamp pair
    /// </summary>
    public sealed class SignInEntry
    {
        /// <summary>
        /// The time of signing in
        /// </summary>
        public DateTime In { get; }

        /// <summary>
        /// The time of signing out, if one exists
        /// </summary>
        public DateTime Out => _isToday ? DateTime.Now : _out ?? In;

        /// <summary>
        /// Get the total time that the pair represents
        /// If there is no Out time, the total time will be zero
        /// </summary>
        public TimeSpan Duration => Out - In;

        public SignInEntry(DateTime inTime, DateTime? outTime = null)
        {
            In = inTime;
            _out = outTime;

            _isToday = inTime.Date == DateTime.Now.Date;
            _isPartialEntry = (outTime == null);
        }

        public SignInEntry(XElement entry)
            : this((DateTime)entry.Attribute("in"), (DateTime?)entry.Attribute("out")) { }

        public void SignOut(DateTime outTime)
        {
            if (_isPartialEntry)
            {
                _out = outTime;
                _isPartialEntry = false;
            }
        }

        public override string ToString()
        {
            return $"{In.ToShortTimeString()} - {Out.ToShortTimeString()} ({Duration})";
        }

        public XElement GetXml()
        {
            var entry = new XElement("SignInEntry", new XAttribute("in", In));

            if (!_isPartialEntry)
                entry.Attribute("out").SetValue(Out);

            return entry;
        }

        /// <summary>
        /// Get the CSV string representing the in and out time the SignInPair
        /// If a value doesn't exist, and empty string will be used
        /// </summary>
        public string GetCsvString()
        {
            return $"{In.ToShortTimeString()},{Out.ToShortTimeString()}";
        }


        //public static IDictionary<DayOfWeek, SignInPair[]> GetWeekInOutPairs(IEnumerable<Scan> timeStamps)
        //{
        //    if (timeStamps.First().Direction != Scan.LocationType.In)
        //        throw new ArgumentException("first timestamp MUST be an \"in\" scan", "timeStamps");

        //    var scans = timeStamps.GroupBy(x => x.ScanTime.DayOfWeek)
        //                          .Select(x => new { Day = x.Key, Pairs = GetDayPairs(x) })
        //                          .ToArray();

        //    var daysMissing = FirstWeek.Except(scans.Select(x => x.Day)).Select(x => new { Day = x, Pairs = Enumerable.Empty<SignInPair>().ToArray() }).ToArray();
        //    var max = scans.Max(x => x.Pairs.Count());

        //    return scans.Concat(daysMissing).ToDictionary(x => x.Day, x => x.Pairs.Concat(Enumerable.Repeat<SignInPair>(new SignInPair(), max))
        //                                                                         .Take(max)
        //                                                                         .ToArray());
        //}

        //public static readonly DayOfWeek[] FirstWeek = new[]
        //{
        //    DayOfWeek.Saturday,
        //    DayOfWeek.Sunday,
        //    DayOfWeek.Monday,
        //    DayOfWeek.Tuesday,
        //    DayOfWeek.Wednesday,
        //    DayOfWeek.Thursday,
        //    DayOfWeek.Friday
        //};

        private DateTime? _out;

        // Indicates the entry doesn't have an out
        private bool _isPartialEntry;

        // Indicates that the entry is from the current day
        private bool _isToday;

        private SignInEntry() { }
    }
}
