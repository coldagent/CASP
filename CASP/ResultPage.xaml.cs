using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Application = System.Windows.Application;

namespace CASP
{
    /// <summary>
    /// Interaction logic for ResultPage.xaml
    /// </summary>
    public partial class ResultPage : Page
    {
        private OpenFileDialog fileSelector = new();
        private Dictionary<string, string> files = new();
        public ResultPage()
        {
            InitializeComponent();

            // Initialize Event.
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
            }
        }
    }
}
