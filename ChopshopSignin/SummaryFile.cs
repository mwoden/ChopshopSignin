using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    sealed internal class SummaryFile
    {
        public static void CreateSummaryFiles(string outputFolder, IEnumerable<Person> people)
        {
            foreach (var group in people.GroupBy(x => x.Role))
                CreateSummaryFile(outputFolder, group.Key, group);
        }


        private static void CreateSummaryFile(string outputFolder, Person.RoleType role, IEnumerable<Person> people)
        {
            var fileName = System.IO.Path.Combine(outputFolder, string.Format("Hour Summary - {0}s.csv", role.ToString()));

            var hourSummaries = people.ToDictionary(x => x.FullName, x => x.GetTimeSummary());

            var fileLines = people.Select(x => x.GetTimeSummary()
                                                .Select(y => new { Name = x.LastName + " " + x.FirstName, Day = y.Key, Time = y.Value }))
                                  .SelectMany(x => x)
                                  .Select(x => new
                                               {
                                                   Name = x.Name,
                                                   Date = x.Day.ToShortDateString(),
                                                   Time = (x.Time.Days * 24 + x.Time.Hours) + x.Time.Minutes / 60.0,
                                                   Week = (((x.Day - Utility.Kickoff).Days) / 7) + 1
                                               })
                                  .Select(x => string.Format("{0},{1},{2:F1},{3}", x.Name, x.Date, x.Time, x.Week));

            System.IO.File.WriteAllLines(fileName, new[] { "Name,Date,Hours,Week" }.Concat(fileLines));
        }
    }
}