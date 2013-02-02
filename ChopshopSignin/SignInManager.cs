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
        }

        public Func<bool> AllOutConfirmation { get; set; }

        public IList<Person> SignedInPeople { get { return people.Values.Where(x => x.CurrentLocation == Scan.LocationType.In).ToArray(); } }

        public void Commit()
        {
            if (changeCount > 0)
            {
                Person.Save(people.Values, xmlDataFile);
                changeCount = 0;
            }
        }

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
                    currentScanData = new StringBuilder();

                    // Disable the reset scan timer
                    eventList.Clear(EventList.Event.ResetCurrentScan);

                    // Determine if a command was scanned
                    var command = ParseCommand(scanData);

                    switch (command)
                    {
                        case ScanCommand.In:
                        case ScanCommand.Out:
                            // If a person was already scanned (selected)
                            if (currentPerson != null)
                            {
                                // Reset the scan in/out timeout window
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

        private SignInManager()
        {
            eventList = new EventList();

            currentScanData = new StringBuilder();
            ScanInOutTimeout = TimeSpan.FromSeconds(Settings.Instance.ScanInTimeoutWindow);
            ResetScanDataTimeout = TimeSpan.FromSeconds(Settings.Instance.ScanDataResetTime);
            UpdateTotalTimeTimeout = TimeSpan.FromSeconds(Settings.Instance.TotalTimeUpdateInterval);

            timer = new Timer(timerInterval);
            timer.Elapsed += ClockTick;
            timer.Enabled = true;
        }

        private SignInManager(ViewModel externalModel)
            : this()
        {
            model = externalModel;
            people = new Dictionary<string, Person>();
        }

        private readonly ViewModel model;
        private readonly EventList eventList;
        private readonly TimeSpan ScanInOutTimeout;
        private readonly TimeSpan ResetScanDataTimeout;
        private readonly TimeSpan UpdateTotalTimeTimeout;
        // Dictionary for determining who is currently signed in
        private readonly Dictionary<string, Person> people;

        private const int timerInterval = 200;
        private readonly Timer timer;

        // Used to track if the currently loaded file has been changed
        private int changeCount = 0;

        private StringBuilder currentScanData;
        private Person currentPerson;
        private string xmlDataFile;

        private readonly object syncObject = new object();

        /// <summary>
        /// The current command to execute based on the scan data
        /// </summary>
        private enum ScanCommand { NoCommmand, In, Out, AllOutNow }

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
        /// Find anyone signed in and sign them out
        /// </summary>
        /// <returns>Result with status and display string</returns>
        private void SignAllOut()
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

        private void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            // If the reset current person timer is active and expired
            if (eventList.HasExpired(EventList.Event.ResetCurrentPerson, e.SignalTime))
                // This shouldn't be needed, since the timer should only be set if a person was scanned
                if (currentPerson != null)
                {
                    currentPerson = null;
                    model.ScanStatus = "You waited too long, please re-scan your name";
                }

            // If the reset scan timer has expired
            if (eventList.HasExpired(EventList.Event.ResetCurrentScan, e.SignalTime))
                // Clear the current scan data
                lock (syncObject)
                    currentScanData = new StringBuilder();

            // If the update total time timer has expired
            if (eventList.HasExpired(EventList.Event.UpdateTotalTime, e.SignalTime))
            {
                // Update the total time displayed
                UpdateTotalTime();

                // Schedule another update
                eventList.Set(EventList.Event.UpdateTotalTime, UpdateTotalTimeTimeout);
            }
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
                model.TotalTime = people.Values.Aggregate(TimeSpan.Zero, (accumulate, x) => accumulate = accumulate.Add(x.GetTotalTimeSince(Settings.Instance.Kickoff)));
            }
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
                    timer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}