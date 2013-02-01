using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ChopshopSignin
{
    /// <summary>
    /// Class to manager people signing in and out
    /// </summary>
    class SignInManager
    {
        public SignInManager(ViewModel externalModel, EventList externalEventList)
            : this()
        {
            model = externalModel;
            eventList = externalEventList;
            People = new Dictionary<string, Person>();
        }

        public Func<bool> AllOutConfirmation { get; set; }

        public void TextInputHandler(string scanText)
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
                                    // Clear the scanned person 
                                    currentPerson = null;

                                    // Remove the timer to reset the current person
                                    eventList.Clear(EventList.Event.ResetCurrentPerson);

                                    // Update the display of who's signed in
                                    model.UpdateCheckedInList(People.Values);

                                    //TODO Inject data file
                                    //// Save the current list
                                    //Person.Save(People.Values, XmlDataFile);
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
                                if (!People.ContainsKey(name))
                                    People[name] = newPerson;

                                // Set the person waiting for an in or out scan
                                currentPerson = People[name];

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
            currentScanData = new StringBuilder();
            ScanInOutTimeout = TimeSpan.FromSeconds(Settings.Instance.ScanInTimeoutWindow);
            ResetScanDataTimeout = TimeSpan.FromSeconds(Settings.Instance.ScanDataResetTime);
            UpdateTotalTimeTimeout = TimeSpan.FromSeconds(Settings.Instance.TotalTimeUpdateInterval);
        }

        private readonly ViewModel model;
        private readonly EventList eventList;
        private readonly TimeSpan ScanInOutTimeout;
        private readonly TimeSpan ResetScanDataTimeout;
        private readonly TimeSpan UpdateTotalTimeTimeout;
        // Dictionary for determining who is currently signed in
        private readonly Dictionary<string, Person> People;

        private StringBuilder currentScanData;
        private Person currentPerson;


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
                var remaining = People.Values.Where(x => x.CurrentLocation == Scan.LocationType.In);
                var status = string.Format("Signed out all {0} remaining at {1}", remaining.Count(), DateTime.Now.ToShortTimeString());

                foreach (var person in remaining)
                    person.SignInOrOut(false);

                model.ScanStatus = status;
                model.UpdateCheckedInList(People.Values);
            }
            else
                model.ScanStatus = "Sign everyone out command cancelled";
        }
    }
}