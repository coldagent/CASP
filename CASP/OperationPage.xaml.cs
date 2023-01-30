using System;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace CASP
{
    /// <summary>
    /// Interaction logic for OperationPage.xaml
    /// </summary>
    public partial class OperationPage : Page
    {
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new();
        private bool running = false;
        private SerialPort sp;
        private string portName = "COM2";

        public OperationPage()
        {
            InitializeComponent();

            // Initialize Event.
            this.SizeChanged += OperationPage_SizeChanged;
            this.DepthBox.GotFocus += DepthBox_GotFocus;
            this.DepthBox.LostFocus += DepthBox_LostFocus;
            Checkmark.Visibility = Visibility.Hidden;
            Xmark.Visibility = Visibility.Visible;
            ConnectionLabel.Content = "Disconnected";
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            // Serial Port Initialization
            sp = new(portName);
            sp.BaudRate = 9600;
            sp.ReadTimeout = 5000;
            sp.WriteTimeout = 5000;
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            Thread connection_thread = new(CheckConnection);
            connection_thread.IsBackground = true;
            connection_thread.Start();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            if (!running)
                return;
            else if (OpProgressBar.Value >= 300) 
            {
                string messageBoxText = "Ran for " + (OpProgressBar.Value/10).ToString() + " seconds";
                running = false;
                OpProgressBar.Value = 0;
                MessageBox.Show(messageBoxText, "Stopping Probe", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
            }
            else
                OpProgressBar.Value++;
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
            bool isNumber = double.TryParse(DepthBox.Text, out _);
            string messageBoxText = "Error: Invalid Probe Depth Input";
            string caption = "Error";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;
            if (running)
            {
                return;
            }
            else if (isNumber && DepthUnit.Text != "Unit")
            {
                messageBoxText = "Probe Depth Entered: " + DepthBox.Text + " " + DepthUnit.Text;
                caption = "Starting Probe";
                icon = MessageBoxImage.Information;
                running = true;
            }
            else if (isNumber && DepthUnit.Text == "Unit")
            {
                messageBoxText = "Error: Please select a Probe Depth unit";
            }
            else if (!isNumber)
            {
                messageBoxText = "Error: Please enter a Probe Depth decimal number";
            }

            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "The Stop Button was pressed";
            if (running)
            {
                messageBoxText += "\nRan for " + (OpProgressBar.Value/10).ToString() + " seconds";
                running = false;
                OpProgressBar.Value = 0;
            }
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
            sp.Close();
        }
        private void CheckConnection()
        {
            while (true)
            {
                string[] ports = SerialPort.GetPortNames();
                for (int i = 0; i < ports.Length; i++)
                {
                    Trace.WriteLine("Trying Port: " + ports[i]);
                    try
                    {
                        sp.PortName = ports[i];
                        sp.Open();
                        sp.WriteLine("%handshake");
                        if (sp.ReadLine().Equals("%connected"))
                        {
                            sp.Close();
                            this.Dispatcher.Invoke(() => {
                                Checkmark.Visibility = Visibility.Visible;
                                Xmark.Visibility = Visibility.Hidden;
                                ConnectionLabel.Content = "Connected";
                            });

                            Trace.WriteLine("PortName= " + ports[i]);
                            return;
                        }
                        sp.Close();
                    }
                    catch (Exception e)
                    {
                        if (e.GetType() == typeof(TimeoutException))
                        {
                            sp.Close();
                            Trace.WriteLine("Timeout for " + ports[i]);
                        } else if (e.GetType() == typeof(UnauthorizedAccessException))
                        {
                            Trace.WriteLine("Port in use: " + ports[i]);
                        }
                        else
                        {
                            Trace.WriteLine("Invalid Port: " + ports[i]);
                        }
                    }
                }
                Thread.Sleep(10500);
            }
            
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

        }
    }
}
