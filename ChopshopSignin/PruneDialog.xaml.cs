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
    /// Interaction logic for PruneDialog.xaml
    /// </summary>
    internal partial class PruneDialog : Window
    {
        private SignInManager signInManager;

        public PruneDialog(SignInManager currentManager)
        {
            InitializeComponent();
            signInManager = currentManager;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            signInManager.Prune(PruneDate.SelectedDate ?? DateTime.MinValue);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
