using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    class StudentWeekEntry
    {
        public string StudentName { get; private set; }
        public IDictionary<DayOfWeek, DayEntry[]> Days { get; private set; }

        static readonly DayOfWeek[] FirstWeek = { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        public StudentWeekEntry()
        {
            Days = new Dictionary<DayOfWeek, DayEntry[]>();
        }

        public StudentWeekEntry(string fullName, ILookup<DayOfWeek, Scan> weekScans)
            : this()
        {
            StudentName = fullName;

            Days = Enum.GetValues(typeof(DayOfWeek))
                          .Cast<DayOfWeek>()
                          .Select(weekday => new { Weekday = weekday, DayEntries = DayEntry.GetDayEntries(weekScans[weekday]) })
                          .Where(x => x.DayEntries.Count() > 0)
                          .ToDictionary(x => x.Weekday, x => x.DayEntries);
        }

        public string[] ToCsvLines()
        {
            var list = new List<string> { StudentName };
            var entries = new Dictionary<DayOfWeek, List<string>>
            {
                { DayOfWeek.Saturday, new List<string>() },
                { DayOfWeek.Sunday, new List<string>() },
                { DayOfWeek.Monday, new List<string>() },
                { DayOfWeek.Tuesday, new List<string>() },
                { DayOfWeek.Wednesday, new List<string>() },
                { DayOfWeek.Thursday, new List<string>() },
                { DayOfWeek.Friday, new List<string>() },
            };

            foreach (var entry in Days)
                entries[entry.Key].AddRange(entry.Value.Select(x => x.ToCsv()));


            var lineCount = entries.Max(x => x.Value.Count());

            foreach (var each in entries)
                each.Value.AddRange(Enumerable.Repeat(",", lineCount - each.Value.Count()));


            var lines = new List<string>();

            foreach (var index in Enumerable.Range(0, lineCount))
            {
                var parts = new[] {StudentName, entries[DayOfWeek.Saturday][index], entries[DayOfWeek.Sunday][index], entries[DayOfWeek.Monday][index],
                                entries[DayOfWeek.Tuesday] [index], entries[DayOfWeek.Wednesday] [index], entries[DayOfWeek.Thursday] [index],
                                entries[DayOfWeek.Friday] [index], };

                lines.Add(string.Join(",", parts));
            }

            return lines.ToArray();
        }

        public class DayEntry
        {
            public DateTime? Arrive { get; private set; }
            public DateTime? Leave { get; private set; }

            /// <summary>
            /// Creates 2 CSV cells with the in and out times for a day
            /// </summary>
            public string ToCsv()
            {
                return (Arrive != null ? ((DateTime)Arrive).ToShortTimeString() : "") +
                       "," +
                       (Leave != null ? ((DateTime)Leave).ToShortTimeString() : "");
            }

            public DayEntry(IEnumerable<Scan> dayScans)
            {
                if (dayScans.Any())
                {
                    Arrive = dayScans.Select(x => x.ScanTime).Min();
                    Leave = dayScans.Select(x => x.ScanTime).Max();

                    if (Arrive == Leave)
                        Leave = null;
                }
            }

            public static DayEntry[] GetDayEntries(IEnumerable<Scan> dayScans)
            {
                var times = dayScans.OrderBy(x => x.ScanTime).ToArray();

                var sets = Enumerable.Range(0, times.Count()).GroupBy(x => x / 2, x => times[x]);

                return sets.Select(x => new DayEntry(x)).ToArray();
            }

            public override string ToString()
            {
                return Arrive != null ? ((DateTime)Arrive).ToShortDateString() : "" +
                       Arrive != null ? ((DateTime)Arrive).ToShortTimeString() : "" +
                       "," +
                       Leave != null ? ((DateTime)Leave).ToShortTimeString() : "";
            }
        }
    }
}
