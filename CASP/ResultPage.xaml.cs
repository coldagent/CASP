using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
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

            // Initialize Event.
            fileSelector.InitialDirectory = Directory.GetCurrentDirectory();
            fileSelector.RestoreDirectory = true;
            ResultPlot.Plot.Title("Force vs. Depth");
            ResultPlot.Plot.XLabel("Depth (m)");
            ResultPlot.Plot.YLabel("Force (psi)");
            this.SizeChanged += ResultPage_SizeChanged;
        }
        private void ResultPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navbar_Background.Width = ActualWidth;
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
            var vals = line.Split(',');
            if (vals[0] != "CASP")
            {
                MessageBox.Show("Invalid File Given", "Bad File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return false;
            }
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                vals = line.Split(',');
                values[0].Add(double.Parse(vals[0]));
                values[1].Add(double.Parse(vals[1]));
                values[2].Add(double.Parse(vals[2]));
            }
            data.Add(filename, values);
            return true;
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
                ResultPlot.Plot.AddScatter(data[names[0]][0].ToArray(), data[names[0]][1].ToArray());
                ResultPlot.Refresh();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileNames.SelectedItem != null)
            {
                string selectedName = (string)FileNames.SelectedItem;
                FileNames.Items.Remove(FileNames.SelectedItem);
                files.Remove(selectedName);
            }
        }
    }
}
