using System;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;

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
        private bool stopping = false;
        private SerialPort sp;
        private string portName = "COM2";
        private string currentFile = "";
        private Queue<double> ldcellQueue = new Queue<double>();
        private Queue<double> probeQueue = new Queue<double>();
        private double ldcellZero = -1;

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
            sp.ReadTimeout = 10000;
            sp.WriteTimeout = 10000;
            sp.ReadBufferSize = 1048576;
            sp.NewLine = "\n";
            sp.DtrEnable = true;
            sp.RtsEnable = true;
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
                if (timeout_watcher > 6000)
                {
                    timeout_watcher = 0;
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value = 0; });
                    ConnectionLost();
                }
                try
                {
                    this.Dispatcher.Invoke(() => { progress = OpProgressBar.Value; });
                } catch { Trace.WriteLine("Error in UpdateProgressBar"); continue; }
                if ((!running && !resetting && progress == 0) || ((resetting || running) && progress >= 95)) { }
                else if (!running && !resetting && progress >= 1)
                {
                    this.Dispatcher.Invoke(() => { OpProgressBar.Value = 1200; });
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
        private int CalculateDepth()
        {
            double depth_double = double.Parse(DepthBox.Text);
            int depth_int = 0;
            //convert to cm
            switch (DepthUnit.Text)
            {
                case "in.":
                    depth_int = (int)(depth_double * 2.54);
                    break;
                case "cm":
                    depth_int = (int)(depth_double * 1);
                    break;
            }   
            return depth_int;
        }
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isNumber = double.TryParse(DepthBox.Text, out _);
            if (running || resetting)
            {
                string messageBoxText = "Please wait for the current operation to finish.";
                string caption = "Operation In-Progress";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            if (DepthUnit.Text == "Unit")
            {
                string messageBoxText = "Error: Please select a Probe Depth unit";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            else if (!isNumber)
            {
                string messageBoxText = "Error: Please enter a Probe Depth decimal number";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            } else
            {
                currentFile = DateTime.Now.ToString("yyyyMMMdd HHmm") + ".csv";
                int depth = CalculateDepth();
                running = true;
                try
                {
                    sp.WriteLine("%start " + depth.ToString());
                    //Trace.WriteLine("Sending {%start " + depth.ToString() + "}");
                }
                catch
                {
                    Trace.WriteLine("Error in Start Click");
                    ConnectionLost();
                }
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (running || resetting)
            {
                running = false;
                resetting = false;
                stopping = true;
                OpProgressBar.Value = 0;
                try
                {
                    sp.WriteLine("%stop");
                } catch
                {
                    Trace.WriteLine("Error in Stop Click");
                    ConnectionLost();
                }
                string messageBoxText = "Stopping the current operation.";
                string caption = "Stopping Operation";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            }
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
#if DEBUG
                        sp.PortName = "COM3";
#else
                        sp.PortName = ports[i];
#endif
                        sp.Open();
                        sp.DiscardInBuffer();
                        sp.DiscardOutBuffer();
                        sp.WriteLine("%handshake");
                        Thread.Sleep(1000);
                        if (connected)
                        {
                            Trace.WriteLine("PortName= " + sp.PortName);
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

        double movingAverage(double x, bool isLdCell) {
            double average = 0;
            if (isLdCell)
            {
                if (ldcellQueue.Count < 4)
                {
                    ldcellQueue.Enqueue(x);
                } else if (ldcellQueue.Count == 4)
                {
                    ldcellQueue.Enqueue(x);
                    double[] q = ldcellQueue.ToArray();
                    average = (q[0] + q[1] + q[2] + q[3] + q[4]) / 5;
                } else
                {
                    ldcellQueue.Dequeue();
                    ldcellQueue.Enqueue(x);
                    double[] q = ldcellQueue.ToArray();
                    average = (q[0] + q[1] + q[2] + q[3] + q[4]) / 5;
                }
            } else
            {
                if (probeQueue.Count < 4)
                {
                    probeQueue.Enqueue(x);
                }
                else if (probeQueue.Count == 4)
                {
                    probeQueue.Enqueue(x);
                    double[] q = probeQueue.ToArray();
                    average = (q[0] + q[1] + q[2] + q[3] + q[4]) / 5;
                }
                else
                {
                    probeQueue.Dequeue();
                    probeQueue.Enqueue(x);
                    double[] q = probeQueue.ToArray();
                    average = (q[0] + q[1] + q[2] + q[3] + q[4]) / 5;
                }
            }
            return average;
        }
        // This function brings me pain. I am sorry.
        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = sp.ReadLine();
                Debug.WriteLine("Received: " + line);
                if (running)
                {
                    if (!line.Equals("done"))
                        sp.WriteLine("c");   //lets the MCU know to continue so it does not wait for GUI jobs
                    string currentPath = Environment.CurrentDirectory + "\\ReceivedData\\" + currentFile;
                    if (!Directory.Exists(Environment.CurrentDirectory + "\\ReceivedData"))
                    {
                        Directory.CreateDirectory(Environment.CurrentDirectory + "\\ReceivedData");
                    }
                    if (!File.Exists(currentPath))
                    {
                        using (StreamWriter sw = File.CreateText(currentPath))
                        {
                            sw.WriteLine("CASP");
                            sw.WriteLine("Depth (mm),Penetration Force (psi),PF Moving Average (psi),Conductivity (Ohm), Cond Moving Average (Ohm)");
                        }
                    }
                    if (line.Equals("done"))
                    {
                        running = false;
                        ldcellQueue.Clear();
                        probeQueue.Clear();
                        ldcellZero = -1;
                        this.Dispatcher.Invoke(() =>
                        {
                            string messageBoxText = "The probe is done taking measurements.";
                            string caption = "Measurements Finished";
                            MessageBoxButton button = MessageBoxButton.OK;
                            MessageBoxImage icon = MessageBoxImage.Information;
                            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                        });
                    }
                    else
                    {
                        // (adc/16777216)*2.56 is the voltage received at the adc
                        string[] items = line.Split(',');
                        //Get numbers from strings and convert to their values
                        double i1 = long.Parse(items[1]);
                        if (ldcellZero == -1)
                        {
                            ldcellZero = i1;
                        } else
                        {
                            // The decimal is the surface area of the probes
                            i1 = (Math.Abs(i1 - ldcellZero) / 148415) / 0.1242524438187;
                            double i2 = ((long.Parse(items[2]) / 16777216.0) * 2.56) * 1000; //TODO: Convert to resistance
                            double i1Average = movingAverage(i1, true);
                            double i2Average = movingAverage(i2, false);
                            line = items[0] + "," + i1.ToString() + "," + i1Average.ToString() + "," + i2.ToString() + "," + i2Average.ToString();
                            File.AppendAllText(currentPath, line + "\n");
                        }
                    }
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
                }
                else if (resetting && !line.Equals("done"))
                {
                    Trace.WriteLine("Strange Error");
                    ConnectionLost();
                } 
                else if (stopping && line.Equals("done")) { 
                    stopping = false;
                    this.Dispatcher.Invoke(() =>
                    {
                        string messageBoxText = "The operation was stopped";
                        string caption = "Operation Stopped";
                        MessageBoxButton button = MessageBoxButton.OK;
                        MessageBoxImage icon = MessageBoxImage.Information;
                        MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                    });
                }
                else
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
                Trace.WriteLine(Environment.CurrentDirectory);
                ConnectionLost();
            }
            if (sp.IsOpen && sp.BytesToRead > 0)
            {
                sp_DataReceived(sender, e);
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
            try
            {
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                sp.Close();
            } catch
            {
                Trace.WriteLine("Error in ConnectionLost");
            }
            Thread connection_thread = new(CheckConnection);
            connection_thread.IsBackground = true;
            connection_thread.Start();
        }
    }
}
