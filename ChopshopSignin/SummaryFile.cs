using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    class WeekSummary
    {
        public int Week { get; private set; }
        public string FullName { get; private set; }
        public IEnumerable<string> SignInTimes { get; private set; }

        public WeekSummary(int weekNumber, string name, IEnumerable<string> times)
        {
            Week = weekNumber;
            FullName = name;
            SignInTimes = times;
        }
    }

    class SummaryFile
    {
        public static void CreateAllFiles(string outputFolder, DateTime kickoff, IEnumerable<Person> people, Person.RoleType role)
        {
            var weekData = people.Where(x => x.Role == role)
                                 .Select(x => x.GetWeekSummaries(kickoff))
                                 .SelectMany(x => x)
                                 .Select(x => x.SignInTimes.Select(y => new { FullName = x.FullName, Week = x.Week, Line = y }))
                                 .SelectMany(x => x)
                                 .GroupBy(x => x.Week)
                                 .Select(x => new { Week = x.Key, Lines = x });

            foreach (var week in weekData.OrderBy(_x => _x.Week))
            {
                var outputFile = string.Format("{0} - Week {1}.csv", role.ToString(), week.Week);
                var outputPath = System.IO.Path.Combine(outputFolder, outputFile);

                var fileData = week.Lines.OrderBy(x => x.FullName).Select(x => x.Line).ToArray();

                var temp = GetCsvHeader().Concat(fileData).ToArray();
                System.IO.File.WriteAllLines(outputPath, temp);
            }
        }

        private static IEnumerable<string> GetCsvHeader()
        {
            return new[]
                {
                    ",,Saturday,,Sunday,,Monday,,Tuesday,,Wednesday,,Thursday,,Friday",
                    ",,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out"
                };
        }
    }
}
