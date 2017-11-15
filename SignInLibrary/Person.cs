﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SignInLibrary
{
    public sealed class Person
    {
        public enum RoleType { Invalid, Student, Mentor }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName => $"{LastName}, {FirstName}";

        public Guid Id { get; }

        public bool IsSignedIn => GetLatestTodayEntry()?.IsIn ?? false;
        public bool IsSignedOut => !IsSignedIn;


        ///// <summary>
        ///// Returns the most recent location from today's timestamps
        ///// </summary>
        //public Scan.LocationType CurrentLocation
        //{
        //    get
        //    {
        //        var today = Timestamps.Where(x => x.ScanTime.Date == DateTime.Today);
        //        if (today.Count() == 0)
        //            return Scan.LocationType.Out;

        //        return today.Last().Direction;
        //    }
        //}

        

        /// <summary>
        /// Returns the most recent time in for the current day
        /// </summary>
        public DateTime? TimeIn => GetLatestTodayEntry()?.In;

        public RoleType Role { get; private set; }

        public IList<SignInEntry> Entries { get; private set; } = new List<SignInEntry>();

        private Person() { }

        private Person(string lastName, string firstName, RoleType role)
            : this()
        {
            Role = role;
            LastName = lastName.Trim();
            FirstName = firstName.Trim();
        }

        private Person(string lastName, string firstName, bool isMentor)
            : this(lastName, firstName, isMentor ? RoleType.Mentor : RoleType.Student) { }

        public Person(XElement personXml)
        {
            FirstName = (string)personXml.Attribute("firstName");
            LastName = (string)personXml.Attribute("lastName");

            RoleType result;

            if (!Enum.TryParse<RoleType>((string)personXml.Attribute("role"), true, out result))
                result = RoleType.Student;

            Role = result;

            Entries = personXml.Element("Scans")
                               .Elements()
                               .Select(x => new SignInEntry(x))
                               .OrderBy(x => x.In)
                               .ToList();
        }

        //public static Person Create(string scanData)
        //{
        //    if (string.IsNullOrWhiteSpace(scanData) || !scanData.Contains(','))
        //        return null;

        //    RoleType role = Person.RoleType.Student;

        //    // Check for a mentor scan
        //    if (IsMentor(scanData))
        //    {
        //        role = Person.RoleType.Mentor;
        //        scanData = GetMentorName(scanData);
        //    }

        //    return new Person(scanData.Split(',').First(), scanData.Split(',').Last(), role);
        //}

        public bool Equals(Person other)
        {
            if (other == null)
                return false;

            return FullName.Equals(other.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Person);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public static bool operator ==(Person a, Person b)
        {
            if (object.ReferenceEquals(a, null))
                return object.ReferenceEquals(a, b);

            return a.Equals(b);
        }

        public static bool operator !=(Person a, Person b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"{FullName} : {Role.ToString()} : CURRENT_LOCATION";
        }

        /// <summary>
        /// Toggle the person in or out
        /// </summary>
        /// <returns></returns>
        public SignInOutResult Toggle()
        {
            return SignInOrOut(!IsSignedIn);
        }

        /// <summary>
        /// Signs a person in or out, and returns an corresponding sign in/out result
        /// </summary>
        /// <param name="signingIn">If true, indicates the person is signing in</param>
        /// <returns>The result of the operatoin</returns>
        public SignInOutResult SignInOrOut(bool signingIn)
        {
            if (signingIn)
                return SignIn();

            return SignOut();
        }

        /// <summary>
        /// Serialize the person to XML
        /// </summary>
        public XElement ToXml()
        {
            return new XElement("Person",
                    new XAttribute("lastName", LastName),
                    new XAttribute("firstName", FirstName),
                    new XAttribute("role", Role),
                    new XElement("Scans", Entries.Select(x => x.ToXml())));
        }

        /// <summary>
        /// Saves a list of people to the specified files
        /// Only people with at least 1 timestamp will be saved,
        /// sorted by Student/Mentor, then by name (last name first)
        /// </summary>
        public static void Save(IEnumerable<Person> people, string filePath)
        {
            new XElement("SignInList", people.Where(x => x.Entries.Any())
                                             .OrderBy(x => x.Role)
                                             .ThenBy(x => x.FullName)
                                             .Select(x => x.ToXml())).Save(filePath);
        }

        /// <summary>
        /// Load all the people from the file
        /// This will make a backup copy in the BackupFolder folder first
        /// </summary>
        public static IEnumerable<Person> Load(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                BackupDataFile(filePath);
                return XElement.Load(filePath).Elements().Select(x => new Person(x));
            }

            return Enumerable.Empty<Person>();
        }

        /// <summary>
        /// Removes all entries prior to the date specified by the cut-off parameter
        /// </summary>
        /// <param name="cutoff">Date which indicates the oldest scan that will be kept. Time will be ignored, only date is used.</param>
        public void Prune(DateTime cutoff)
        {
            Entries = Entries.Where(x => x.In >= cutoff.Date).ToList();
        }

        public IDictionary<DateTime, TimeSpan> GetTimeSummary()
        {
            // Get the person's timestamps and group them by week
            return Entries.OrderBy(x => x.In)
                          .GroupBy(x => x.In.Date)
                          .ToDictionary(x => x.Key, x => GetDayTotalTime(x));
        }

        /// <summary>
        /// Get the total amount of time spent by the person since startTime
        /// </summary>
        public TimeSpan GetTotalTimeSince(DateTime startTime)
        {
            // Returns the total time spent since the start time
            return Entries.Where(x => x.In > startTime)
                          .GroupBy(x => x.In)
                          .Select(x => GetDayTotalTime(x.OrderBy(y => y.In)))
                          .Aggregate(TimeSpan.Zero, (accumulator, x) => accumulator.Add(x));
        }

        /// <summary>
        /// Get the total time spent for a given day
        /// </summary>
        /// <param name="scanTimes">A single day's worth of scans</param>
        /// <returns>The time spent that day</returns>
        private TimeSpan GetDayTotalTime(IEnumerable<SignInEntry> signInEntries)
        {
            return TimeSpan.FromTicks(signInEntries.Sum(x => x.Duration.Ticks));
        }

        /// <summary>
        /// Signs a person in, and returns an corresponding sign in/out result
        /// </summary>
        private SignInOutResult SignIn()
        {
            if (IsSignedOut)
            {
                Entries.Add(new SignInEntry(DateTime.Now));

                var statusMessage = $"{FirstName} {LastName} in at {GetLatestTodayEntry().In.ToShortTimeString()}";
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed in, scan \"OUT\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

        /// <summary>
        /// Signs a person out, and returns an corresponding sign in/out result
        /// </summary>
        /// <returns></returns>
        private SignInOutResult SignOut()
        {
            if (IsSignedIn)
            {
                GetLatestTodayEntry().SignOut(DateTime.Now);

                var statusMessage = $"{FirstName} {LastName} out at {GetLatestTodayEntry().Out.ToShortTimeString()}";
                return new SignInOutResult(true, statusMessage);
            }
            else
            {
                var statusMessage = "You are already signed out, scan \"IN\" instead";
                return new SignInOutResult(false, statusMessage);
            }
        }

        /// <summary>
        /// Get the latest (by in time) entry for today
        /// </summary>
        /// <returns></returns>
        private SignInEntry GetLatestTodayEntry()
        {
            return Entries.Where(x => x.In.Date == DateTime.Today).OrderBy(x => x.In).LastOrDefault();
        }

        /// <summary>
        /// Make a backup copy of the specified file a 'Backup' subdirectory
        /// The backup file will be prefixed with yyyy-MM-dd HH_mm_ss
        /// </summary>
        private static void BackupDataFile(string originalFilePath)
        {
            if (System.IO.File.Exists(originalFilePath))
            {
                //var backupFolder = System.IO.Path.Combine(Utility.OutputFolder, Properties.Settings.Default.BackupFolder);

                //        if (!System.IO.Directory.Exists(backupFolder))
                //            System.IO.Directory.CreateDirectory(backupFolder);

                //        var backupFile = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + " " + System.IO.Path.GetFileName(originalFilePath);
                //        var backupFilePath = System.IO.Path.Combine(backupFolder, backupFile);

                //        System.IO.File.Copy(originalFilePath, backupFilePath);

                //        var archiveFolder = ChopshopSignin.Properties.Settings.Default.ArchiveFolder;


                //        // Check if the archive location is set (not empty) and the path is available
                //        if (!string.IsNullOrWhiteSpace(archiveFolder) && System.IO.Directory.Exists(archiveFolder))
                //        {
                //            var archiveFile = System.IO.Path.Combine(archiveFolder, backupFile);

                //            // Copy the latest backup file to archive
                //            System.IO.File.Copy(backupFilePath, archiveFile);
                //        }

                //        // Clean out the backup folder
                //        ManageBackupFiles(backupFolder, Properties.Settings.Default.MaxBackupFilesToKeep);
            }
        }

        /// <summary>
        /// Manage the backup folder and only keep a certain number of the most recent files. Older ones will be deleted
        /// </summary>
        /// <param name="maxFilesKept">The maximum number of files that will be kept. The the newer files will be kept</param>
        private static void ManageBackupFiles(string backupFolder, int maxFilesKept)
        {
            var filesToDelete = System.IO.Directory.EnumerateFiles(backupFolder)
                                                   .Select(x => new System.IO.FileInfo(x))
                                                   .OrderByDescending(x => x.CreationTime)
                                                   .Skip(maxFilesKept);

            foreach (var file in filesToDelete)
                file.Delete();
        }

        /// <summary>
        /// Detects a mentor scan
        /// </summary>
        private static bool IsMentor(string rawScanData)
        {
            return Regex.IsMatch(rawScanData, MentorIdPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns the mentor name only, stripping out the mentor ID string
        /// </summary>
        private static string GetMentorName(string rawScanData)
        {
            var match = Regex.Match(rawScanData, MentorIdPattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new ArgumentException("should have a mentor prefix but doesn't", "rawScanData");

            return rawScanData.Substring(match.Value.Length);
        }

        ///// <summary>
        ///// Contains the week defintion, by FIRST season standards
        ///// </summary>
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

        /// <summary>
        /// The regex pattern to detect a mentor scan
        /// </summary>
        private const string MentorIdPattern = @"\Amentor[^a-z]+";
    }
}
