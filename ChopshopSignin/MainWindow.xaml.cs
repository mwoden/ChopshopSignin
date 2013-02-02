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
        private readonly ViewModel viewModel;
        private readonly SignInManager signInManger;

        private readonly string OutputFolder;
        private readonly string XmlDataFile;

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            viewModel = new ViewModel();

            // Set up the variables for future use
            OutputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDataFile = System.IO.Path.Combine(OutputFolder, Properties.Resources.ScanDataFileName);

            signInManger = new SignInManager(viewModel, System.IO.Path.Combine(OutputFolder, Properties.Resources.ScanDataFileName));
            signInManger.AllOutConfirmation += ConfirmAllOutCommand;

            // Set the window icon to the 
            Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri(Properties.Resources.WindowIconPath, UriKind.Relative)).Stream);

            // Set up the sorting for the two collection views
            var sortDesc = new System.ComponentModel.SortDescription("FullName", System.ComponentModel.ListSortDirection.Ascending);
            ((CollectionViewSource)FindResource("CheckedInStudents")).SortDescriptions.Add(sortDesc);
            ((CollectionViewSource)FindResource("CheckedInMentors")).SortDescriptions.Add(sortDesc);

            viewModel.DisplayTime = Settings.Instance.ClearScanStatusTime;
            viewModel.ShipDate = Settings.Instance.Ship;
            viewModel.ShowTimeUntilShip = Settings.Instance.ShowTimeUntilShip;

            DataContext = viewModel;
        }

        /// <summary>
        /// Logs an unhandled exception to a file called Exception.txt
        /// </summary>
        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("Exception.txt", e.ExceptionObject.ToString());
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            signInManger.HandleScanData(e.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Update the displayed lists after loading all data
            viewModel.UpdateCheckedInList(signInManger.SignedInPeople);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            signInManger.Commit();

            //TODO Pull into SignInManager?
            // Generate the mentor and student summary files
            //SummaryFile.CreateAllFiles(OutputFolder, Settings.Instance.Kickoff, People.Values, Person.RoleType.Student);
            //SummaryFile.CreateAllFiles(OutputFolder, Settings.Instance.Kickoff, People.Values, Person.RoleType.Mentor);

            Dispose();
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
                    viewModel.Dispose();
                    signInManger.Dispose();
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
    }
}