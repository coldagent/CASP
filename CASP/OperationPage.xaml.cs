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
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new();
        private int runtime = -1;

        public OperationPage()
        {
            InitializeComponent();

            // Initialize Event.
            this.SizeChanged += OperationPage_SizeChanged;
            this.DepthBox.GotFocus += DepthBox_GotFocus;
            this.DepthBox.LostFocus += DepthBox_LostFocus;
            Checkmark.Visibility = Visibility.Hidden;
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            if (runtime < 0)
                return;
            else
                runtime++;
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
                DepthBox.Text = "Enter the probe depth";
            }
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            bool valid = false;

            bool isNumber = double.TryParse(DepthBox.Text, out _);
            string messageBoxText = "Error: Invalid Probe Depth Input";
            string caption = "Error";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;
            if (isNumber && DepthUnit.Text != "Unit")
            {
                messageBoxText = "Probe Depth Entered: " + DepthBox.Text + " " + DepthUnit.Text;
                caption = "Starting Probe";
                icon = MessageBoxImage.Information;
                valid = true;
            } else if (isNumber && DepthUnit.Text == "Unit")
            {
                messageBoxText = "Error: Please select a Probe Depth unit";
            } else if (!isNumber)
            {
                messageBoxText = "Error: Please enter a Probe Depth decimal number";
            }

            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (valid)
            {
                runtime++;
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "The Stop Button was pressed";
            if (runtime >= 0)
                messageBoxText += "\nRan for " + runtime.ToString() + " seconds";
            runtime = -1;
            string caption = "Stopping Probe";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "The Reset Button was pressed";
            string caption = "Resetting Probe";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }
    }
}
