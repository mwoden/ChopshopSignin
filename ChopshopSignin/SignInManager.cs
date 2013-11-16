using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Input;

namespace ChopshopSignin
{
    /// <summary>
    /// Class to manager people signing in and out
    /// </summary>
    sealed class SignInManager : IDisposable
    {
        public SignInManager(ViewModel externalModel, string dataFile)
            : this(externalModel)
        {
            xmlDataFile = dataFile;
            people = Person.Load(xmlDataFile).ToDictionary(x => x.FullName, x => x);
            UpdateTotalTime();
        }

        public Func<bool> AllOutConfirmation { get; set; }

        public IList<Person> SignedInPeople { get { return SignedIn.ToArray(); } }

        /// <summary>
        /// True if there is at least one person signed in
        /// </summary>
        public bool AnySignedIn { get { return SignedIn.Any(); } }

        /// <summary>
        /// Determine if any changes have occurred in the scan data file, and if so,
        /// write the changes to the file
        /// </summary>
        public void Commit()
        {
            if (changeCount > 0)
            {
                Person.Save(people.Values, xmlDataFile);
                changeCount = 0;
            }
        }

        /// <summary>
        /// Create CSV files for summarize hours
        /// </summary>
        public void CreateSummaryFiles()
        {
            SummaryFile.CreateSummaryFiles(Utility.OutputFolder, people.Values);
        }

        /// <summary>
        /// Handles data passed in from the input, in order to determine what action to take
        /// </summary>
        /// <param name="scanText"></param>
        public void HandleScanData(string scanText)
        {
            // If there isn't a scan already in progress, set up the timer to
            // reset the scan data (to clear anything accidently entered by keyboard)
            if (!eventList.IsEnabled(EventList.Event.ResetCurrentScan))
                eventList.Set(EventList.Event.ResetCurrentScan, ResetScanDataTimeout);

            lock (syncObject)
            {
                currentScanData.Append(scanText);

                // If the termination character has been seen, start processing
                if (scanText == "\r")
                {
                    // Extract the current scan data
                    var scanData = currentScanData.ToString().Trim();

                    // Disable the reset scan timer and reset the incoming
                    // scan data for the next scan
                    eventList.Clear(EventList.Event.ResetCurrentScan);
                    currentScanData = new StringBuilder();

                    // Determine if a command was scanned
                    var command = ParseCommand(scanData);

                    switch (command)
                    {
                        case ScanCommand.In:
                        case ScanCommand.Out:
                            // If a person was already scanned (selected)
                            if (currentPerson != null)
                            {
                                // Set up the scan in/out timeout window
                                eventList.Set(EventList.Event.ResetCurrentPerson, ScanInOutTimeout);

                                // Check the sign-in result
                                var result = currentPerson.SignInOrOut(command == ScanCommand.In);
                                if (result.OperationSucceeded)
                                {
                                    // Increment the change count
                                    changeCount++;

                                    // Clear the scanned person 
                                    currentPerson = null;

                                    // Remove the timer to reset the current person
                                    eventList.Clear(EventList.Event.ResetCurrentPerson);

                                    // Update the display of who's signed in
                                    model.UpdateCheckedInList(people.Values);

                                    // Save the current list
                                    Commit();
                                }

                                // Display the result of the sign in/out operation
                                model.ScanStatus = result.Status;
                            }
                            else
                                model.ScanStatus = "Please scan your name first";
                            break;

                        case ScanCommand.AllOutNow:
                            SignAllOut();
                            break;

                        // Non-command scan, store the data in the current scan
                        // This depends on the name pattern matching to detect
                        // if the scan is garbage, or a person
                        case ScanCommand.NoCommmand:
                        default:
                            var newPerson = Person.Create(scanData);

                            // If the scan data fits the pattern of a person scan
                            if (newPerson != null)
                            {
                                var name = newPerson.FullName;

                                // If the person isn't already in the dictionary, add them
                                if (!people.ContainsKey(name))
                                    people[name] = newPerson;

                                // Set the person waiting for an in or out scan
                                currentPerson = people[name];

                                // Update the display to display the person's name
                                model.ScanStatus = currentPerson.FirstName + ", sign in or out";

                                // Set the reset person timer
                                eventList.Set(EventList.Event.ResetCurrentPerson, ScanInOutTimeout);
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Find anyone signed in and sign them out
        /// </summary>
        public void SignAllOut()
        {
            var confirmAllOutCmd = AllOutConfirmation;

            if (confirmAllOutCmd == null)
                throw new NullReferenceException("AllOutConfirmation never set to confirmation function");

            // Sign out all signed in users at the current time
            if (confirmAllOutCmd())
            {
                var remaining = people.Values.Where(x => x.CurrentLocation == Scan.LocationType.In);
                var status = string.Format("Signed out all {0} remaining at {1}", remaining.Count(), DateTime.Now.ToShortTimeString());

                changeCount += remaining.Count();

                foreach (var person in remaining)
                    person.SignInOrOut(false);

                model.ScanStatus = status;
                model.UpdateCheckedInList(people.Values);
            }
            else
                model.ScanStatus = "Sign everyone out command cancelled";
        }

        private SignInManager()
        {
            // Events not defined in this dictionary will result in ignoring that event
            eventHandler = new Dictionary<EventList.Event, Action>()
            { 
                { EventList.Event.ResetCurrentPerson, ResetCurrentPersonEventTimeout },
                { EventList.Event.ResetCurrentScan, ResetCurrentScanEventTimeout },
                { EventList.Event.UpdateTotalTime, UpdateTotalTimeEventTimeout },
                // ClearDisplayStatus not used in SignInManager
            };

            eventList = new EventList();

            currentScanData = new StringBuilder();
            ScanInOutTimeout = TimeSpan.FromSeconds(Properties.Settings.Default.ScanInTimeoutWindow);
            ResetScanDataTimeout = TimeSpan.FromSeconds(Properties.Settings.Default.ScanDataResetTime);
            UpdateTotalTimeTimeout = TimeSpan.FromSeconds(Properties.Settings.Default.TotalTimeUpdateInterval);

            Properties.Settings.Default.PropertyChanged += SettingChanged;

            timer = new Timer(timerInterval);
            timer.Elapsed += ClockTick;
            timer.Enabled = true;
        }

        void SettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var settings = (ChopshopSignin.Properties.Settings)sender;
            switch (e.PropertyName)
            {
                case "ScanInTimeoutWindow":
                    ScanInOutTimeout = TimeSpan.FromDays(settings.ScanInTimeoutWindow);
                    break;

                case "ScanDataResetTime":
                    ResetScanDataTimeout = TimeSpan.FromSeconds(settings.ScanDataResetTime);
                    break;

                case "TotalTimeUpdateInterval":
                    UpdateTotalTimeTimeout = TimeSpan.FromSeconds(settings.TotalTimeUpdateInterval);
                    // For this case, clear the event scheduled and update the time now, rescheduling it also
                    eventList.Clear(EventList.Event.UpdateTotalTime);
                    UpdateTotalTime();
                    break;
            }
        }

        private SignInManager(ViewModel externalModel)
            : this()
        {
            model = externalModel;
            people = new Dictionary<string, Person>();
        }

        private readonly ViewModel model;
        private readonly EventList eventList;
        private TimeSpan ScanInOutTimeout;
        private TimeSpan ResetScanDataTimeout;
        private TimeSpan UpdateTotalTimeTimeout;
        // Dictionary for determining who is currently signed in
        private readonly Dictionary<string, Person> people;

        // Dictionary to handle events
        private readonly Dictionary<EventList.Event, Action> eventHandler;

        private const int timerInterval = 200;
        private readonly Timer timer;

        // Used to track if the currently loaded file has been changed
        private int changeCount = 0;

        private StringBuilder currentScanData;
        private Person currentPerson;
        private string xmlDataFile;

        // Indicates that the object has already been disposed
        private bool disposed = false;

        private readonly object syncObject = new object();

        /// <summary>
        /// The current command to execute based on the scan data
        /// </summary>
        private enum ScanCommand { NoCommmand, In, Out, AllOutNow }

        /// <summary>
        /// People currently signed in
        /// </summary>
        private IEnumerable<Person> SignedIn { get { return people.Values.Where(x => x.CurrentLocation == Scan.LocationType.In); } }

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
        /// Every time the timer fires, the evenHandler list will be enumerated and
        /// each event will be checked to see if it expired. If it did expire, the
        /// event handler for that event will be run
        /// </summary>
        private void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var currentEvent in eventHandler.Keys)
                if (eventList.HasExpired(currentEvent, e.SignalTime))
                    eventHandler[currentEvent]();
        }

        /// <summary>
        /// Calculates the total time spent by all people, then sets the timer to update the total again
        /// </summary>
        private void UpdateTotalTime()
        {
            // Queue up the next update
            eventList.Set(EventList.Event.UpdateTotalTime, UpdateTotalTimeTimeout);

            // Ensure that there are some people
            if (people.Any())
            {
                // Find the oldest time for the display
                model.OldestTime = people.Values.Where(x => x.Timestamps.Any()).SelectMany(x => x.Timestamps).Min(x => x.ScanTime);

                // Total up all the time
                model.TotalTime = people.Values.Aggregate(TimeSpan.Zero, (accumulate, x) => accumulate = accumulate.Add(x.GetTotalTimeSince(Utility.Kickoff)));
            }
        }

        /// <summary>
        /// Handles when the ResetCurrentPerson even expires and the currently selected
        /// person needs to be reset
        /// </summary>
        private void ResetCurrentPersonEventTimeout()
        {
            currentPerson = null;
            model.ScanStatus = "You waited too long, please re-scan your name";
        }

        /// <summary>
        /// Handles when the ResetCurrentScan event expires and the current
        /// scan data needs to be reset
        /// </summary>
        private void ResetCurrentScanEventTimeout()
        {
            lock (syncObject)
                currentScanData = new StringBuilder();
        }

        /// <summary>
        /// Handles when the UpdateTotalTime event expires and the total
        /// time displayed has to be updated
        /// </summary>
        private void UpdateTotalTimeEventTimeout()
        {
            // Update the total time displayed
            UpdateTotalTime();

            // Schedule another update
            eventList.Set(EventList.Event.UpdateTotalTime, UpdateTotalTimeTimeout);
        }

        /// <summary>
        /// Allows the release of the system resources used by the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Internal dispose function to handle cleaning up the object
        /// </summary>
        /// <param name="disposing">Indicates that the dispose operation is called from a user dispose</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    timer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}