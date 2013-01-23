#undef NO_SAVE

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
    public partial class MainWindow : Window, IDisposable
    {
        // The current command to execute based on the scan data
        private enum ScanCommand { NoCommmand, In, Out, AllOutNow }

        private const string windowIconPath = @"/timeclock-icon.ico";
        private const string xmlDataFileName = @"ScanData.xml";

        private readonly ViewModel viewModel = new ViewModel();
        private readonly System.Timers.Timer clockTimer = new System.Timers.Timer(100);
        private readonly DateTime Kickoff = new DateTime(2013, 1, 5);

        private readonly TimeSpan ScanInOutWindow = new TimeSpan(0, 0, 10);
        private readonly TimeSpan ResetScanDataTime = new TimeSpan(0, 0, 3);

        private readonly string OutputFolder;
        private readonly string XmlDataFile;

        private readonly object syncObject = new object();

        // Dictionary for determining who is currently signed in
        private Dictionary<string, Person> People = new Dictionary<string, Person>();

        // The data that hsa been entered to the app so far, and needs a '\r' terminator
        private StringBuilder scanDataInProgress = new StringBuilder();

        // The current person selected by the last scan
        private Person currentScannedPerson;

        // Time that indicates when the current selected person will be reset
        private DateTime? resetCurrentPerson;

        // Time that indicagtes when to clear the scan data if garbage has been entered
        private DateTime? resetCurrentScan;


        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Set the window icon to the 
            Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri(windowIconPath, UriKind.Relative)).Stream);

            DataContext = viewModel;

            clockTimer.Elapsed += ClockTick;
            clockTimer.Enabled = true;

            OutputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDataFile = System.IO.Path.Combine(OutputFolder, xmlDataFileName);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("Exception.txt", e.ExceptionObject.ToString());
        }

        void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            viewModel.CurrentTime = DateTime.Now;

            if (currentScannedPerson != null && resetCurrentPerson != null)
            {
                if (resetCurrentPerson < DateTime.Now)
                {
                    currentScannedPerson = null;
                    resetCurrentPerson = null;
                    viewModel.ScanStatus = "You waited too long, please re-scan your name";
                }
            }

            if (resetCurrentScan != null)
                if (resetCurrentScan < DateTime.Now)
                    lock (syncObject)
                    {
                        resetCurrentScan = null;
                        scanDataInProgress = new StringBuilder();
                    }
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (resetCurrentScan == null)
                resetCurrentScan = DateTime.Now + ResetScanDataTime;

            lock (syncObject)
            {
                scanDataInProgress.Append(e.Text);

                if (e.Text == "\r")
                {
                    // Extract the current scan data
                    var scanData = scanDataInProgress.ToString().Trim();
                    scanDataInProgress = new StringBuilder();

                    // Disable reseting the current scan data
                    resetCurrentScan = null;

                    // Determine if a command was scanned
                    var command = ParseCommand(scanData);

                    switch (command)
                    {
                        case ScanCommand.In:
                        case ScanCommand.Out:
                            // If a person was already scanned (selected)
                            if (currentScannedPerson != null)
                            {
                                // Check the sign-in result
                                var result = currentScannedPerson.SignInOrOut(command == ScanCommand.In);
                                if (result.OperationSucceeded)
                                {
                                    currentScannedPerson = null;

                                    // Update the display of who's signed in
                                    viewModel.UpdateCheckedInLists(People.Values);

                                    // Save the current list
                                    Person.Save(People.Values, XmlDataFile);
                                }

                                // If they scanned a command, reset the timeout window
                                if (resetCurrentPerson != null)
                                    resetCurrentPerson = DateTime.Now + ScanInOutWindow;

                                viewModel.ScanStatus = result.Status;
                            }
                            else
                                viewModel.ScanStatus = "Please scan your name first";
                            break;

                        case ScanCommand.AllOutNow:
                            // Sign out all signed in users at the current time
                            if (ConfirmAllOutCommand())
                            {
                                var allOutResult = SignAllOut();
                                if (allOutResult.OperationSucceeded)
                                    viewModel.ScanStatus = allOutResult.Status;

                                viewModel.UpdateCheckedInLists(People.Values);
                            }
                            else
                                viewModel.ScanStatus = "Sign everyone out command cancelled";
                            break;

                        // Non-command scan, store the data in the current scan
                        case ScanCommand.NoCommmand:
                        default:
                            var newPerson = Person.Create(scanData);

                            // If the scan data fits the pattern of a person scan
                            if (newPerson != null)
                            {
                                var name = newPerson.FullName;
                                if (!People.ContainsKey(name))
                                    People[name] = newPerson;

                                currentScannedPerson = People[name];

                                viewModel.ScanStatus = currentScannedPerson.FirstName + ", sign in or out";
                                resetCurrentPerson = DateTime.Now + ScanInOutWindow;
                            }
                            break;
                    }
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
            SummaryFile.CreateAllFiles(OutputFolder, Kickoff, People.Values, Person.RoleType.Student);
            SummaryFile.CreateAllFiles(OutputFolder, Kickoff, People.Values, Person.RoleType.Mentor);
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

        private bool ConfirmAllOutCommand()
        {
            var message = "You are about to sign out all currently signed-in people" + Environment.NewLine +
                          "Please select 'Yes' to sign everyone out";
            var result = MessageBox.Show(message, "Confirm signing all out", MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning, MessageBoxResult.No);
            return result == MessageBoxResult.Yes;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    clockTimer.Dispose();
                }
            }
        }
    }
}