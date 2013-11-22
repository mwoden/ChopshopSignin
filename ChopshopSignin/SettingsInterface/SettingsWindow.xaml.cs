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
using System.Windows.Shapes;

namespace ChopshopSignin
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    internal sealed partial class SettingsWindow : Window
    {
        private SettingsWindowViewModel viewModel = new SettingsWindowViewModel(Properties.Settings.Default);

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
            Close();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {


        }
    }
}
