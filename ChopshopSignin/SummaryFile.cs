using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    class SummaryFile
    {
        public static void CreateAllFiles(string outputFolder, DateTime kickoff, IEnumerable<Scan> scans)
        {
            var weeks = scans.Where(x => x.Role == Scan.RoleType.Student)
                             .Select(x => new { Week = (((x.ScanTime.Date - kickoff).Days) / 7) + 1, ScanData = x })
                             .GroupBy(x => x.Week, x => x.ScanData)
                             .Select(x => new { Week = x.Key, Data = x });

            var csvFiles = weeks.Select(x => new { Week = x.Week, FileData = GenerateWeekCsvFile(x.Data) }).ToList();

            if (!System.IO.Directory.Exists(outputFolder))
                System.IO.Directory.CreateDirectory(outputFolder);

            foreach (var existingFile in System.IO.Directory.GetFiles(outputFolder, "Week*.csv"))
            {
                System.IO.File.Delete(existingFile);
            }

            foreach (var file in csvFiles)
            {
                var fileName = string.Format("Week {0}.csv", file.Week);
                var outputPath = System.IO.Path.Combine(outputFolder, fileName);

                System.IO.File.WriteAllText(outputPath, file.FileData);
            }
        }

        private static string GenerateWeekCsvFile(IEnumerable<Scan> weekData)
        {
            var weekSummary = weekData.GroupBy(x => x.FullName)
                                      .Select(x => new StudentWeekEntry(x.First().FullName, x.ToLookup(y => y.ScanTime.DayOfWeek, y => y)));

            return string.Join(Environment.NewLine, new WeekRecord(weekSummary).GetFile());
        }
    }
}
