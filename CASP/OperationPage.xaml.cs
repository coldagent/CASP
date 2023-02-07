using System;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Diagnostics.Eventing.Reader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Drawing;

namespace CASP
{
    /// <summary>
    /// Interaction logic for OperationPage.xaml
    /// </summary>
    public partial class OperationPage : Page
    {
        private bool running = false;
        private bool resetting = false;
        private bool connected = false;
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
            Thread progressbar_thread = new(UpdateProgressBar);
            progressbar_thread.IsBackground = true;
            progressbar_thread.Start();

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

        private void UpdateProgressBar()
        {
            double progress = 0;
            int timeout_watcher = 0;
            while (true)
            {
                if (resetting || running) { timeout_watcher++; }
                if (timeout_watcher > 300)
                {
                    timeout_watcher = 0;
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value = 0; });
                    ConnectionLost();
                }
                try
                {
                    this.Dispatcher.Invoke(() => { progress = OpProgressBar.Value; });
                } catch { Trace.WriteLine("Error in UpdateProgressBar"); continue; }
                if ((!running && !resetting && progress == 0) || (resetting && progress >= 95)) { }
                else if (!running && !resetting && progress >= 1)
                {
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value = 100; });
                    Thread.Sleep(2000);
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value = 0; });
                    timeout_watcher = 0;
                }
                else
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value++; });
                
                Thread.Sleep(100);
            }
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
            if (running || resetting)
            {
                messageBoxText = "Please wait for the current operation to finish.";
                caption = "Operation In-Progress";
                button = MessageBoxButton.OK;
                icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
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
            if (!connected)
            {
                string messageBoxText = "Please connect the Controller to the Windows Computer";
                string caption = "Controller Not Connected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            if (resetting || running)
            {
                string messageBoxText = "Please wait for the current operation to finish.";
                string caption = "Operation In-Progress";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            resetting = true;
            try
            {
                sp.WriteLine("%reset");
            } catch
            {
                Trace.WriteLine("Error in ResetBtn_Click");
                ConnectionLost();
            }

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
                        Thread.Sleep(5000);
                        if (connected)
                        {
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
            }
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = sp.ReadLine();
                if (running)
                {

                }
                else if (resetting && line.Equals("done"))
                {
                    resetting = false;
                    this.Dispatcher.Invoke(() =>
                    {
                        string messageBoxText = "The probe was reset";
                        string caption = "Reset Probe";
                        MessageBoxButton button = MessageBoxButton.OK;
                        MessageBoxImage icon = MessageBoxImage.Information;
                        MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                    });
                } else if (resetting && !line.Equals("done"))
                {
                    ConnectionLost();
                } else
                {
                    if (line.Equals("connected"))
                    {
                        connected = true;
                        this.Dispatcher.Invoke(() => {
                            Checkmark.Visibility = Visibility.Visible;
                            Xmark.Visibility = Visibility.Hidden;
                            ConnectionLabel.Content = "Connected";
                        });
                    } 
                }
            } catch
            {
                Trace.WriteLine("Error in DataReceived");
                ConnectionLost();
            }
        }

        private void RaiseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                string messageBoxText = "Please connect the Controller to the Windows Computer";
                string caption = "Controller Not Connected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            if (running || resetting)
            {
                string messageBoxText = "Please wait for the current operation to finish.";
                string caption = "Operation In-Progress";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            try
            {
                if (!sp.IsOpen)
                {
                    sp.Open();
                }
                sp.WriteLine("%raise");
            } catch
            {
                Trace.WriteLine("Error in Raise Click");
                ConnectionLost();
            }
            
        }
        private void LowerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                string messageBoxText = "Please connect the Controller to the Windows Computer";
                string caption = "Controller Not Connected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            if (running || resetting)
            {
                string messageBoxText = "Please wait for the current operation to finish.";
                string caption = "Operation In-Progress";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            try
            {
                if (!sp.IsOpen)
                {
                    sp.Open();
                }
                sp.WriteLine("%lower");
            } catch
            {
                Trace.WriteLine("Error in Lower Click");
                ConnectionLost();
            }
            
        }
        private void ConnectionLost()
        {
            resetting = false;
            running = false;
            connected = false;
            // Update UI elements
            this.Dispatcher.Invoke(() => {
                Checkmark.Visibility = Visibility.Hidden;
                Xmark.Visibility = Visibility.Visible;
                ConnectionLabel.Content = "Disconnected";
                string messageBoxText = "Controller Connection was lost. Please reconnect the controller and try again";
                string caption = "Controller Disconnected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            });
            //Run connection function again
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            sp.Close();
            Thread connection_thread = new(CheckConnection);
            connection_thread.IsBackground = true;
            connection_thread.Start();
        }
    }
}
