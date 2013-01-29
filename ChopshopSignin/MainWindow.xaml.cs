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
    sealed internal partial class MainWindow : Window, IDisposable
    {
        // The current command to execute based on the scan data
        private enum ScanCommand { NoCommmand, In, Out, AllOutNow }

        private readonly ViewModel viewModel = new ViewModel();
        private readonly System.Timers.Timer clockTimer = new System.Timers.Timer(100);
        private readonly System.Timers.Timer totalTimeTimer = new System.Timers.Timer(Settings.Instance.TotalTimeUpdateInterval * 1000);

        private readonly TimeSpan ScanInOutWindow = TimeSpan.FromSeconds(Settings.Instance.ScanInTimeoutWindow);
        private readonly TimeSpan ResetScanDataTime = TimeSpan.FromSeconds(Settings.Instance.ScanDataResetTime);

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

        // Time that indicates when to clear the scan data if garbage has been entered
        private DateTime? resetCurrentScan;

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            // Set the window icon to the 
            Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri(Properties.Resources.WindowIconPath, UriKind.Relative)).Stream);

            var sortDesc = new System.ComponentModel.SortDescription("FullName", System.ComponentModel.ListSortDirection.Ascending);

            ((CollectionViewSource)FindResource("CheckedInStudents")).SortDescriptions.Add(sortDesc);
            ((CollectionViewSource)FindResource("CheckedInMentors")).SortDescriptions.Add(sortDesc);

            viewModel.DisplayTime = Settings.Instance.ClearScanStatusTime;
            viewModel.ShipDate = Settings.Instance.Ship;
            viewModel.ShowTimeUntilShip = Settings.Instance.ShowTimeUntilShip;

            DataContext = viewModel;

            clockTimer.Elapsed += ClockTick;
            clockTimer.Elapsed += viewModel.ClockTick;
            clockTimer.Enabled = true;

            totalTimeTimer.Elapsed += UpdateTotalTime;
            totalTimeTimer.Enabled = true;

            // Set up the variables for future use
            OutputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDataFile = System.IO.Path.Combine(OutputFolder, Properties.Resources.ScanDataFileName);
        }

        /// <summary>
        /// Logs an unhandled exception to a file called Exception.txt
        /// </summary>
        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("Exception.txt", e.ExceptionObject.ToString());
        }

        /// <summary>
        /// Main timer, runs every 100 ms
        /// This updates the displayed time, and handles timed events
        /// </summary>
        void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update the currently displayed time
            viewModel.CurrentTime = e.SignalTime;

            // Check if there's someone waiting to scan in or out, and the reset person timer is active
            if (currentScannedPerson != null && resetCurrentPerson != null)
                // If the reset time has passed
                if (resetCurrentPerson < e.SignalTime)
                {
                    currentScannedPerson = null;
                    resetCurrentPerson = null;
                    viewModel.ScanStatus = "You waited too long, please re-scan your name";
                }

            // If the reset scan timer is active
            if (resetCurrentScan != null)
                // If the reset time has passed
                if (resetCurrentScan < e.SignalTime)
                    // Clear the current scan data
                    lock (syncObject)
                    {
                        resetCurrentScan = null;
                        scanDataInProgress = new StringBuilder();
                    }
        }

        /// <summary>
        /// Updates the current value of the total time contributed
        /// </summary>
        void UpdateTotalTime(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateTotalTime();
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            // If there isn't a scan already in progress, set up the timer to
            // reset the scan data (to clear anything accidently entered by keyboard)
            if (resetCurrentScan == null)
                resetCurrentScan = DateTime.Now + ResetScanDataTime;

            lock (syncObject)
            {
                scanDataInProgress.Append(e.Text);

                // If the termination character has been seen, start processing
                if (e.Text == "\r")
                {
                    // Extract the current scan data
                    var scanData = scanDataInProgress.ToString().Trim();
                    scanDataInProgress = new StringBuilder();

                    // Disable the reset scan timer
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
                                    // Clear the scanned person
                                    currentScannedPerson = null;

                                    // Update the display of who's signed in
                                    viewModel.UpdateCheckedInList(People.Values);

                                    // Save the current list
                                    Person.Save(People.Values, XmlDataFile);
                                }

                                // If they scanned a command, reset the timeout window
                                if (resetCurrentPerson != null)
                                    resetCurrentPerson = DateTime.Now + ScanInOutWindow;

                                // Display the result of the sign in/out operation
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

                                viewModel.UpdateCheckedInList(People.Values);
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

                                // If the person isn't already in the dictionary, add them
                                if (!People.ContainsKey(name))
                                    People[name] = newPerson;

                                // Set the person waiting for an in or out scan
                                currentScannedPerson = People[name];

                                // Update the display to display the person's name
                                viewModel.ScanStatus = currentScannedPerson.FirstName + ", sign in or out";

                                // Set the reset person timer
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

            // Update the displayed lists after loading all data
            viewModel.UpdateCheckedInList(People.Values);

            UpdateTotalTime();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            clockTimer.Enabled = false;

            // Save the scan data
            Person.Save(People.Values, XmlDataFile);

            // Generate the mentor and student summary files
            SummaryFile.CreateAllFiles(OutputFolder, Settings.Instance.Kickoff, People.Values, Person.RoleType.Student);
            SummaryFile.CreateAllFiles(OutputFolder, Settings.Instance.Kickoff, People.Values, Person.RoleType.Mentor);

            Dispose();
        }

        /// <summary>
        /// Find anyone signed in and sign them out
        /// </summary>
        /// <returns>Result with status and display string</returns>
        private SignInOutResult SignAllOut()
        {
            var remaining = People.Values.Where(x => x.CurrentLocation == Scan.LocationType.In);
            var status = string.Format("Signed out all {0} remaining at {1}", remaining.Count(), DateTime.Now.ToShortTimeString());

            foreach (var person in remaining)
                person.SignInOrOut(false);

            return new SignInOutResult(true, status);
        }

        /// <summary>
        /// Parse the string into the appropriate command
        /// </summary>
        /// <returns>The appropriate command for the string, or NoCommand</returns>
        private ScanCommand ParseCommand(string input)
        {
            ScanCommand result;
            if (!Enum.TryParse<ScanCommand>(input, true, out result))
                return ScanCommand.NoCommmand;

            return result;
        }

        /// <summary>
        /// Display a dialog to prevent accidentally signing everyone out 
        /// </summary>
        /// <returns>True if the user clicked Yes, False otherwise</returns>
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
            Dispose(true);
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
                    totalTimeTimer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }

        private void Mentor_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = AcceptPerson(e.Item as Person, Person.RoleType.Mentor);
        }

        private void Student_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = AcceptPerson(e.Item as Person, Person.RoleType.Student);
        }

        private bool AcceptPerson(Person candidate, Person.RoleType roleFilter)
        {
            if (candidate != null && candidate.Role == roleFilter)
                return true;

            return false;
        }

        private void UpdateTotalTime()
        {
            if (People.Any())
            {
                viewModel.OldestTime = People.Values.Where(x => x.Timestamps.Any()).SelectMany(x => x.Timestamps).Min(x => x.ScanTime);
                viewModel.TotalTime = People.Values.Aggregate(TimeSpan.Zero, (accumulate, x) => accumulate = accumulate.Add(x.GetTotalTimeSince(Settings.Instance.Kickoff)));
            }
        }
    }
}