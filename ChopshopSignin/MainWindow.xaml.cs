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
        private enum ScanCommand { NoCommmand, In, Out, AllOutNow }

        private const string XmlDataFileName = @"ScanData.xml";

        private readonly ViewModel viewModel = new ViewModel();
        private readonly System.Timers.Timer clockTimer = new System.Timers.Timer(100);
        private readonly DateTime Kickoff = new DateTime(2013, 1, 5);
        private readonly TimeSpan ScanInOutWindow = new TimeSpan(0, 0, 10);
        private readonly string OutputFolder;
        private readonly string XmlDataFile;

        private DateTime? resetScanTime;
        private Dictionary<string, Person> People = new Dictionary<string, Person>();
        private Person currentScannedPerson;
        private StringBuilder scanDataInProgress = new StringBuilder();

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            DataContext = viewModel;

            clockTimer.Elapsed += ClockTick;
            clockTimer.Enabled = true;

            OutputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDataFile = System.IO.Path.Combine(OutputFolder, XmlDataFileName);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("Exception.txt", e.ExceptionObject.ToString());
        }

        void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            viewModel.CurrentTime = DateTime.Now;
            if (currentScannedPerson != null && resetScanTime != null)
            {
                if (resetScanTime < DateTime.Now)
                {
                    currentScannedPerson = null;
                    resetScanTime = null;
                    viewModel.ScanStatus = "You waited too long, please re-scan your name";
                }
            }
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            scanDataInProgress.Append(e.Text);

            if (e.Text == "\r")
            {
                var scanData = scanDataInProgress.ToString().Trim();
                scanDataInProgress = new StringBuilder();

                var command = ParseCommand(scanData);

                switch (command)
                {
                    case ScanCommand.In:
                    case ScanCommand.Out:
                        if (currentScannedPerson != null)
                        {
                            var result = currentScannedPerson.SignInOrOut(command == ScanCommand.In);
                            if (result.OperationSucceeded)
                            {
                                currentScannedPerson = null;

                                viewModel.UpdateCheckedInLists(People.Values);

                                Person.Save(People.Values, XmlDataFile);
                            }

                            // If they scanned a command, reset the timeout window
                            if (resetScanTime != null)
                                resetScanTime = DateTime.Now + ScanInOutWindow;

                            viewModel.ScanStatus = result.Status;
                        }
                        else
                            viewModel.ScanStatus = "Please scan your name first";
                        break;

                    case ScanCommand.AllOutNow:
                        // Sign out all signed in users at the current time
                        var allOutResult = SignAllOut();
                        if (allOutResult.OperationSucceeded)
                            viewModel.ScanStatus = allOutResult.Status;
                        
                        viewModel.UpdateCheckedInLists(People.Values);
                        break;

                    // Non-command scan, store the data in the current scan
                    case ScanCommand.NoCommmand:
                    // Default is do nothing
                    default:
                        var newPerson = Person.Create(scanData);
                        if (newPerson != null)
                        {
                            var name = newPerson.FullName;
                            if (!People.ContainsKey(name))
                                People[name] = newPerson;

                            currentScannedPerson = People[name];

                            viewModel.ScanStatus = currentScannedPerson.FirstName + ", sign in or out";
                            resetScanTime = DateTime.Now + ScanInOutWindow;
                        }
                        break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            People = Person.Load(XmlDataFile).ToDictionary(x => x.FullName, x => x);

            viewModel.UpdateCheckedInLists(People.Values);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            clockTimer.Enabled = false;

            Person.Save(People.Values, XmlDataFile);
            SummaryFile.CreateAllFiles(OutputFolder, Kickoff, People.Values);
        }

        private SignInOutResult SignAllOut()
        {
            var remaining = People.Values.Where(x => x.CurrentLocation == Scan.LocationType.In);
            var status = string.Format("Signed out all {0} remaining at {1}", remaining.Count(), DateTime.Now.ToShortTimeString());

            foreach (var person in remaining)
                person.SignInOrOut(false);

            return new SignInOutResult(true, status);
        }

        private ScanCommand ParseCommand(string input)
        {
            ScanCommand result;
            if (!Enum.TryParse<ScanCommand>(input, true, out result))
                return ScanCommand.NoCommmand;

            return result;
        }

        private void Student_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = false;

            var person = e.Item as Person;
            if (person != null && person.Role == Person.RoleType.Student)
                e.Accepted = true;
        }
    }
}