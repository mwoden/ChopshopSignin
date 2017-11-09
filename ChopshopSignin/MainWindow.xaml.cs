using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;

namespace ChopshopSignin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    sealed internal partial class MainWindow : Window, IDisposable
    {
        private readonly ViewModel viewModel;
        private readonly SignInManager signInManger;
        private readonly System.Timers.Timer saveTimer;
        private readonly Capture camera;
        private readonly ZXing.BarcodeReader reader;
        private readonly System.Timers.Timer captureTimer;

        const int VideoWidth = 640;         // Depends on video device caps
        const int VideoHeight = 480;        // Depends on video device caps
        const int VideoBitsPerPixel = 24;   // BitsPerPixel values dicatated by device

        private bool disposed = false;

        public static RoutedCommand CreateSummaryCommand = new RoutedCommand("Create Summary Data Files", typeof(MainWindow));
        public static RoutedCommand CleanCurrentFileCommand = new RoutedCommand("Clean Current File", typeof(MainWindow));
        public static RoutedCommand ExitCommand = new RoutedCommand("Exit", typeof(MainWindow));
        public static RoutedCommand SettingsCommand = new RoutedCommand("Settings", typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            viewModel = new ViewModel();

            signInManger = new SignInManager(viewModel, Utility.DataFile);

            // Set the window icon to the 
            Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri(Properties.Settings.Default.WindowIconPath, UriKind.Relative)).Stream);

            // Set up the sorting for the two collection views
            var sortDesc = new System.ComponentModel.SortDescription("FullName", System.ComponentModel.ListSortDirection.Ascending);
            ((CollectionViewSource)FindResource("CheckedInStudents")).SortDescriptions.Add(sortDesc);
            ((CollectionViewSource)FindResource("CheckedInMentors")).SortDescriptions.Add(sortDesc);

            // Add the shortcut keys for the various commands
            CreateSummaryCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            ExitCommand.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
            SettingsCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));

            DataContext = viewModel;

            LoadBackgroundImage();

            saveTimer = new System.Timers.Timer(15 * 60 * 1000);    // 15 minutes
            captureTimer = new System.Timers.Timer(100);            // 0.1 second

            camera = new Capture(Properties.Settings.Default.CameraDeviceNumber, VideoWidth, VideoHeight, VideoBitsPerPixel);
            reader = new ZXing.BarcodeReader();
            reader.Options.PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39 };
        }

        /// <summary>
        /// Logs an unhandled exception to a file called Exception.txt
        /// </summary>
        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("Exception.txt", e.ExceptionObject.ToString());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Update the displayed lists after loading all data
            viewModel.UpdateCheckedInList(signInManger.SignedInPeople);

            // Set up the save timer
            saveTimer.Elapsed += PeriodicSave;
            saveTimer.Enabled = true;

            // Set up camera capture timer
            captureTimer.Elapsed += PeriodicCapture;
            captureTimer.AutoReset = false;
            captureTimer.Enabled = true;
        }

        private void PeriodicSave(object sender, System.Timers.ElapsedEventArgs e)
        {
            signInManger.Commit();
        }

        private void PeriodicCapture(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => signInManger.HandleScanData(ScanBarcode()));

            // Restart the time for the next scan
            captureTimer.Enabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            signInManger.Commit();
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;

                    captureTimer.Dispose();
                    saveTimer.Dispose();
                    viewModel.Dispose();
                    signInManger.Dispose();
                    camera.Dispose();

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

        /// <summary>
        /// Handler for signing out all signed-in people
        /// </summary>
        private void SignAllOut_Click(object sender, RoutedEventArgs e)
        {
            signInManger.SignAllOut();
        }

        private void SignOutAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = signInManger.AnySignedIn;
        }

        private void SignOutAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            signInManger.SignAllOut();
        }

        private void CreateSummaryCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            signInManger.CreateSummaryFiles();
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !signInManger.AnySignedIn;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void SettingsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void CleanCurrentFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            new PruneDialog(signInManger).ShowDialog();
        }

        private void LoadBackgroundImage()
        {
            var imageName = Properties.Settings.Default.BackgroundImage;
            var file = System.IO.Path.Combine(Utility.OutputFolder, imageName);

            if (System.IO.File.Exists(file))
                using (var imageStream = System.IO.File.OpenRead(file))
                {
                    var decoder = JpegBitmapDecoder.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    viewModel.Background = decoder.Frames.FirstOrDefault();
                }
        }

        private string ScanBarcode()
        {
            // capture image
            var rawData = camera.Click();

            using (var bitmap = new Bitmap(camera.Width, camera.Height, camera.Stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, rawData))
            {
                var decodeResult = reader.Decode(bitmap);

                // Release the buffer
                if (rawData != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(rawData);

                return decodeResult?.Text;
            }
        }
    }
}