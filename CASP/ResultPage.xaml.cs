using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CASP
{
    /// <summary>
    /// Interaction logic for ResultPage.xaml
    /// </summary>
    public partial class ResultPage : Page
    {
        private OpenFileDialog fileSelector = new();
        private Dictionary<string, string> files = new();
        private Dictionary<string, List<double>[]> data = new();
        public ResultPage()
        {
            InitializeComponent();

            // Initialize OpenFileDialog Directory (Goes to TestData Folder)
            string[] split = Directory.GetCurrentDirectory().Split("\\");
            string directory = "";            
            for (int i = 0; i < split.Length - 3; i++)
            {
                directory += split[i] + '\\';
            }
            directory += "TestData";
            fileSelector.InitialDirectory = directory;
            fileSelector.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            fileSelector.Multiselect = true;
            // Initialize Plot
            ResultPlot.Plot.Title("Force and Moisture vs. Depth");
            ResultPlot.Plot.XLabel("Depth (m)");
            ResultPlot.Plot.YLabel("Force (psi)");
            ResultPlot.Plot.YAxis.Color(ResultPlot.Plot.Palette.GetColor(0));
            ResultPlot.Plot.YAxis2.Label("Moisture (%)");
            ResultPlot.Plot.YAxis2.Color(ResultPlot.Plot.Palette.GetColor(1));
            ResultPlot.Plot.YAxis2.Ticks(true);
            // Initialize Event
            this.SizeChanged += ResultPage_SizeChanged;
        }
        private void ResultPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navbar_Background.Width = ActualWidth;
            ResultPlot.Width = ActualWidth - 300;
            ResultPlot.Height = ActualHeight - 70;
        }
        private void Operation_Click_1(object sender, RoutedEventArgs e)
        {
            Window window = Application.Current.MainWindow;
            MainWindow? window2 = window as MainWindow;
            window2?.SwitchPages(false);
        }

        private bool GetCSVData(string filename)
        {
            List<double>[] values = new List<double>[3];
            for (int i = 0; i < values.Length; i++)     //Must initilize lists
            {
                values[i] = new List<double>();
            }
            StreamReader reader = new StreamReader(files[filename]);
            var line = reader.ReadLine();
            if (line == null)
                goto badFile;
            var vals = line!.Split(',');
            if (vals[0] != "CASP")
                goto badFile;
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                vals = line!.Split(',');
                values[0].Add(double.Parse(vals[0]));
                values[1].Add(double.Parse(vals[1]));
                values[2].Add(double.Parse(vals[2]));
            }
            data.Add(filename, values);
            return true;
        badFile:
            MessageBox.Show("Invalid File Given", "Bad File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
            return false;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileSelector.ShowDialog() == DialogResult.OK)
            {
                string[] names = fileSelector.SafeFileNames;
                string[] paths = fileSelector.FileNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (files.ContainsKey(names[i]))
                        continue;
                    files.Add(names[i], paths[i]);
                    if (!GetCSVData(names[i]))      //unsuccessful CSV generation
                    {
                        files.Remove(names[i]);
                        return;
                    }
                }
                FileNames.Items.Clear();
                foreach(string name in files.Keys)
                {
                    FileNames.Items.Add(name);
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileNames.SelectedItem != null)
            {
                string selectedName = (string)FileNames.SelectedItem;
                FileNames.Items.Remove(FileNames.SelectedItem);
                files.Remove(selectedName);
                data.Remove(selectedName);
                ResultPlot.Plot.Clear();
                ResultPlot.Refresh();
            }
        }

        private void FileNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedName = (string)FileNames.SelectedItem;
            ResultPlot.Plot.Clear();
            if (selectedName == null)
                return;
            ResultPlot.Plot.AddScatter(data[selectedName][0].ToArray(), data[selectedName][1].ToArray());
            var moisture_plot = ResultPlot.Plot.AddScatter(data[selectedName][0].ToArray(), data[selectedName][2].ToArray());
            moisture_plot.YAxisIndex = 1;
            ResultPlot.Refresh();
        }
    }
}
