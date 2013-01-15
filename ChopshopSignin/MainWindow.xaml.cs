using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace ChopshopSignin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel viewModel = new ViewModel();

        private StringBuilder scanDataInProgress = new StringBuilder();

        private ConcurrentBag<Scan> nameList = new ConcurrentBag<Scan>();
        private List<Scan> scans = new List<Scan>();

        private System.Timers.Timer saveTimer;
        private System.Timers.Timer clockTimer;

        private const string XmlDataFileName = @"ScanData.xml";

        private readonly string OutputFolder;
        private readonly string XmlDataFile;

        private DateTime Kickoff = new DateTime(2013, 1, 5);

        private string scannedName;

        private readonly TimeSpan ScanInOutWindow = new TimeSpan(0, 0, 10);
        private DateTime? resetScanTime;

        private enum Location { Out, In }

        private Dictionary<string, SignInEntry> People = new Dictionary<string, SignInEntry>();
        private Scan.RoleType currentUser;

        private class SignInEntry
        {
            public Location Location { get; set; }
            public Scan.RoleType Role { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;

            saveTimer = new System.Timers.Timer(50);
            saveTimer.Elapsed += saveTimer_Elapsed;
            saveTimer.Enabled = true;

            clockTimer = new System.Timers.Timer(100);
            clockTimer.Elapsed += clockTimer_Elapsed;
            clockTimer.Enabled = true;

            OutputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDataFile = System.IO.Path.Combine(OutputFolder, XmlDataFileName);
        }

        void clockTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            viewModel.CurrentTime = DateTime.Now;
            if (scannedName != null && resetScanTime != null)
            {
                if (resetScanTime < DateTime.Now)
                {
                    scannedName = null;
                    resetScanTime = null;
                    viewModel.LastScan = "You waited too long, please re-scan your name";
                }
            }
        }

        void saveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            saveTimer.Enabled = false;
            DumpNames();
            saveTimer.Enabled = true;
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            scanDataInProgress.Append(e.Text);

            if (e.Text == "\r")
            {
                var scanData = scanDataInProgress.ToString().Trim();

                if (scanData.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
                    scanData.Equals("OUT", StringComparison.OrdinalIgnoreCase))
                {
                    if (scannedName == null)
                    {
                        viewModel.LastScan = "Please scan your name first";
                    }
                    else
                    {
                        bool scanningIn = scanData.Equals("IN", StringComparison.OrdinalIgnoreCase);

                        // Someone is scanning IN or OUT
                        if (scannedName.Contains(','))
                        {
                            var currentStudent = new Scan(scannedName, scanningIn, currentUser);

                            // If the student exists in the table...
                            if (People.ContainsKey(currentStudent.FullName))
                            {
                                if (People[currentStudent.FullName].Location == Location.In)
                                {
                                    // They can't scan in again
                                    if (scanningIn)
                                    {
                                        viewModel.LastScan = "You're already scanned in, scan \"OUT\"";
                                    }
                                    else
                                    {
                                        nameList.Add(currentStudent);

                                        viewModel.LastScan = currentStudent.FullName + " OUT at " + DateTime.Now.ToLongTimeString();

                                        scannedName = null;

                                        People[currentStudent.FullName] = new SignInEntry { Location = scanningIn ? Location.In : Location.Out, Role = currentUser };
                                        UpdateSignedInList();
                                    }
                                }
                                else
                                {
                                    // They can't scan out again
                                    if (!scanningIn)
                                    {
                                        viewModel.LastScan = "You're already scanned out, scan \"IN\"";
                                    }
                                    else
                                    {
                                        nameList.Add(currentStudent);

                                        viewModel.LastScan = currentStudent.FullName + " IN at " + DateTime.Now.ToLongTimeString();

                                        scannedName = null;
                                        People[currentStudent.FullName] = new SignInEntry { Location = scanningIn ? Location.In : Location.Out, Role = currentUser };
                                        UpdateSignedInList();
                                    }
                                }
                            }
                            else
                            {
                                if (!scanningIn)
                                {
                                    viewModel.LastScan = "You're already scanned out, scan \"IN\"";
                                }
                                else
                                {
                                    // First scan...
                                    nameList.Add(currentStudent);

                                    viewModel.LastScan = currentStudent.FullName + " " +
                                      (scanningIn ? "IN" : "OUT") + " at " + DateTime.Now.ToLongTimeString();

                                    scannedName = null;

                                    People[currentStudent.FullName] = new SignInEntry { Location = scanningIn ? Location.In : Location.Out, Role = currentUser };
                                    UpdateSignedInList();
                                }
                            }
                        }
                    }
                }
                else if (scanData.Contains(','))
                {
                    // Mentor scans have "MENTOR - " prefixed to the name
                    if (scanData.Contains('-'))
                    {
                        scannedName = scanData.Split('-').Last().Trim();
                        currentUser = Scan.RoleType.Mentor;
                    }
                    // Student scans don't
                    else
                    {
                        scannedName = scanData;
                        currentUser = Scan.RoleType.Student;
                    }

                    resetScanTime = DateTime.Now + ScanInOutWindow;
                    viewModel.LastScan = "Scan \"IN\" or \"OUT\"";
                }
                else
                {
                    viewModel.LastScan = "You didn't scan a valid name - \"" + scanData + "\"";
                }

                scanDataInProgress = new StringBuilder();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNames(XmlDataFile);
            UpdateSignedInList();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            clockTimer.Enabled = false;
            saveTimer.Enabled = false;
            DumpNames();
            SummaryFile.CreateAllFiles(OutputFolder, Kickoff, scans);
        }

        private void DumpNames()
        {
            var names = new List<Scan>();

            Scan result;

            while (nameList.TryTake(out result))
                names.Add(result);

            if (names.Any())
            {
                scans.AddRange(names);
                Scan.SaveScans(scans, XmlDataFile);
            }
        }

        private void LoadNames(string file)
        {
            if (System.IO.File.Exists(XmlDataFile))
            {
                scans = Scan.LoadScans(XmlDataFile).ToList();
                var t = scans.ToLookup(x => x.FullName, x => new { ScanTime = x.ScanTime, InOrOut = x.Direction, Role = x.Role });

                foreach (var name in t)
                {
                    var max = name.Max(x => x.ScanTime);

                    var direction = name.Where(x => x.ScanTime == max)
                                        .Single()
                                        .InOrOut == Scan.ScanDirection.In ? Location.In : Location.Out;

                    var role = name.First().Role;

                    People[name.Key] = new SignInEntry { Location = direction, Role = role };
                }
            }
        }

        private void UpdateSignedInList()
        {
            var studentsIn = People.Select(x => new { Name = x.Key, Location = x.Value.Location, Role = x.Value.Role })
                                   .Where(x => x.Role == Scan.RoleType.Student)
                                   .Where(x => x.Location == Location.In)
                                   .Select(x => x.Name)
                                   .OrderBy(x => x);

            viewModel.StudentsIn = new System.Collections.ObjectModel.ObservableCollection<string>(studentsIn);

        }
    }
}