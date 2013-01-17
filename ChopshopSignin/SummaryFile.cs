using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    class WeekSummary
    {
        public int Week { get; private set; }
        public IEnumerable<string> SignInTimes { get; private set; }

        public WeekSummary(int weekNumber, IEnumerable<string> times)
        {
            Week = weekNumber;
            SignInTimes = times;
        }
    }

    class SummaryFile
    {
        public static void CreateAllFiles(string outputFolder, DateTime kickoff, IEnumerable<Person> people)
        {
            // generate a single person's scan data for each week..
            var test = people.First().GetWeekSummaries(kickoff);
            //var peopleWeeks = people.Select(x => new { Week = x. });


            var temp2 = test.Select(x => new { FileName = string.Format("Week {0}.csv", x.Week), Data = x.SignInTimes.ToArray() }).ToList();

            foreach (var file in temp2)
            {
                var outputPath = System.IO.Path.Combine(outputFolder, file.FileName);
                System.IO.File.WriteAllLines(outputPath, file.Data);
            }









            //
            //  OLD CODE
            //

            //var weeks = scans.Where(x => x.Role == Person.RoleType.Student)
            //                 .Select(x => new { Week = (((x.ScanTime.Date - kickoff).Days) / 7) + 1, ScanData = x })
            //                 .GroupBy(x => x.Week, x => x.ScanData)
            //                 .Select(x => new { Week = x.Key, Data = x });

            //var csvFiles = weeks.Select(x => new { Week = x.Week, FileData = GenerateWeekCsvFile(x.Data) }).ToList();

            //if (!System.IO.Directory.Exists(outputFolder))
            //    System.IO.Directory.CreateDirectory(outputFolder);

            //foreach (var existingFile in System.IO.Directory.GetFiles(outputFolder, "Week*.csv"))
            //    System.IO.File.Delete(existingFile);

            //foreach (var file in csvFiles)
            //{
            //    var fileName = string.Format("Week {0}.csv", file.Week);
            //    var outputPath = System.IO.Path.Combine(outputFolder, fileName);
            //    System.IO.File.WriteAllText(outputPath, file.FileData);
            //}
        }


        private static string GenerateWeekCsvFile(IEnumerable<Scan> weekData)
        {
            return string.Empty;
            //var weekSummary = weekData.GroupBy(x => x.FullName)
            //                          .Select(x => new StudentWeekEntry(x.First().FullName, x.ToLookup(y => y.ScanTime.DayOfWeek, y => y)));

            //return string.Join(Environment.NewLine, new WeekRecord(weekSummary).GetFile());
        }
    }
}
