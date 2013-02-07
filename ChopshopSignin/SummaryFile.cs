using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    sealed internal class SummaryFile
    {
        private static readonly double ExcelSecondsDivsor = new TimeSpan(23, 59, 59).TotalSeconds;
        public static void CreateSummaryFiles(string outputFolder, IEnumerable<Person> people)
        {
            foreach (var group in people.GroupBy(x => x.Role))
                CreateSummaryFile(outputFolder, group.Key, group);
        }


        private static void CreateSummaryFile(string outputFolder, Person.RoleType role, IEnumerable<Person> people)
        {
            var fileName = System.IO.Path.Combine(outputFolder, string.Format("Hour Summary - {0}s.csv", role.ToString()));

            var hourSummaries = people.ToDictionary(x => x.FullName, x => x.GetTimeSummary());

            var days = hourSummaries.SelectMany(x => x.Value).Select(x => x.Key).Distinct().OrderBy(x => x).ToList();

            // Create the CSV header
            var header = string.Join(",", new[] { string.Empty, string.Empty }.Concat(days.Select(x => x.ToShortDateString())).Concat(new[] { "Total" }));

            var fileLines = new List<string>(new[] { header });

            foreach (var person in people)
            {
                var line = new StringBuilder(person.FullName + ",");

                foreach (var day in days)
                {
                    if (hourSummaries[person.FullName].ContainsKey(day))
                    {
                        // Compute the hours spent, using tenths of an hour
                        var hours = hourSummaries[person.FullName][day].Hours + (hourSummaries[person.FullName][day].Minutes / 60.0);
                        line.AppendFormat("{0:F1},", hours);
                    }
                    else
                        line.Append("0,");
                }

                // Compute the hours spent, using tenths of an hour
                var totalTime = hourSummaries[person.FullName].Values.Select(x => x.Hours + (x.Minutes / 60.0)).Sum();

                line.AppendFormat("{0:F1},", totalTime);

                fileLines.Add(line.ToString());
            }

            System.IO.File.WriteAllLines(fileName, fileLines);
        }


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