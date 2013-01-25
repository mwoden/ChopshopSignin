using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    /// <summary>
    /// Holds the in/out scan pair
    /// </summary>
    sealed internal class SignInPair
    {
        public DateTime? In { get; private set; }
        public DateTime? Out { get; private set; }

        private SignInPair() { }

        public SignInPair(IEnumerable<Scan> timeStamps)
        {
            var scanCount = timeStamps.Count();
            if (scanCount != 1 && scanCount != 2)
                throw new ArgumentException("needs to have 1 or 2 elements", "timeStamps");

            In = timeStamps.First().ScanTime;
            if (scanCount == 2)
                Out = timeStamps.Last().ScanTime;
            else
            {
                // If 'in' date is before today, then generate a default 'out' time for the same day
                // So don't generate a default out for today's date
                if (((DateTime)In).Date < DateTime.Today)
                {
                    // Except Sat & Sun, default out time is 9 PM
                    switch (timeStamps.First().ScanTime.DayOfWeek)
                    {
                        case DayOfWeek.Saturday:
                        case DayOfWeek.Sunday:
                            Out = ((DateTime)In).Date.AddHours(18);
                            break;

                        case DayOfWeek.Monday:
                        case DayOfWeek.Tuesday:
                        case DayOfWeek.Wednesday:
                        case DayOfWeek.Thursday:
                        case DayOfWeek.Friday:
                            Out = ((DateTime)In).Date.AddHours(21);
                            break;
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}",
                In == null ? string.Empty : ((DateTime)In).ToShortTimeString(),
                Out == null ? string.Empty : ((DateTime)Out).ToShortTimeString());
        }

        /// <summary>
        /// Get the CSV string representing the in and out time the SignInPair
        /// If a value doesn't exist, and empty string will be used
        /// </summary>
        public string GetCsvString()
        {
            return string.Format("{0},{1}",
                In == null ? string.Empty : ((DateTime)In).ToShortTimeString(),
                Out == null ? string.Empty : ((DateTime)Out).ToShortTimeString());
        }

        /// <summary>
        /// Get the total time that the pair represents
        /// If there is no Out time, the current time will be used
        /// </summary>
        public TimeSpan TotalTime()
        {
            if (In == null)
                return TimeSpan.Zero;

            return (Out ?? DateTime.Now) - (DateTime)In;
        }

        public static IDictionary<DayOfWeek, SignInPair[]> GetWeekInOutPairs(IEnumerable<Scan> timeStamps)
        {
            if (timeStamps.First().Direction != Scan.LocationType.In)
                throw new ArgumentException("first timestamp MUST be an \"in\" scan", "timeStamps");

            var scans = timeStamps.GroupBy(x => x.ScanTime.DayOfWeek)
                                  .Select(x => new { Day = x.Key, Pairs = GetDayPairs(x) })
                                  .ToArray();

            var daysMissing = FirstWeek.Except(scans.Select(x => x.Day)).Select(x => new { Day = x, Pairs = Enumerable.Empty<SignInPair>().ToArray() }).ToArray();
            var max = scans.Max(x => x.Pairs.Count());

            return scans.Concat(daysMissing).ToDictionary(x => x.Day, x => x.Pairs.Concat(Enumerable.Repeat<SignInPair>(new SignInPair(), max))
                                                                                 .Take(max)
                                                                                 .ToArray());
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
