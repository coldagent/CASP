using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CASP
{
    /// <summary>
    /// Interaction logic for OperationPage.xaml
    /// </summary>
    public partial class OperationPage : Page
    {
        public OperationPage()
        {
            InitializeComponent();

            // Initialize Event.
            this.SizeChanged += OperationPage_SizeChanged;
            this.DepthBox.GotFocus += DepthBox_GotFocus;
            this.DepthBox.LostFocus += DepthBox_LostFocus;
        }

        private void OperationPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navbar_Background.Width = ActualWidth;
        }

        private void Result_Click(object sender, RoutedEventArgs e)
        {
            Window window = Application.Current.MainWindow;
            MainWindow? window2 = window as MainWindow;
            window2?.SwitchPages(true);
        }

        private void DepthBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DepthBox.Text == "Enter the probe depth")
            {
                DepthBox.Text = "";
            }
        }
        private void DepthBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DepthBox.Text == "")
            {
                DepthBox.Text = "Enter the Probe Depth";
            }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
