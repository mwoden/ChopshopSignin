using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    class WeekRecord
    {
        public IEnumerable<StudentWeekEntry> Student { get; private set; }

        public WeekRecord()
        {
            Student = Enumerable.Empty<StudentWeekEntry>();
        }

        public WeekRecord(IEnumerable<StudentWeekEntry> studentWeek)
            : this()
        {
            Student = studentWeek.ToArray();
        }

        public IEnumerable<string> GetFile()
        {
            return GetCsvHeader().Concat(Student.OrderBy(x => x.StudentName)
                                                .Select(x => x.ToCsvLines())
                                                .SelectMany(x => x));
        }

        private IEnumerable<string> GetCsvHeader()
        {
            return new[]
                {
                    ",,Saturday,,Sunday,,Monday,,Tuesday,,Wednesday,,Thursday,,Friday",
                    ",,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out"
                };
        }

    }
}
