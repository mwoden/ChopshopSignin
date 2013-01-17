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

        private static IEnumerable<string> GetCsvHeader()
        {
            return new[]
                {
                    ",,Saturday,,Sunday,,Monday,,Tuesday,,Wednesday,,Thursday,,Friday",
                    ",,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out,In,Out"
                };
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
