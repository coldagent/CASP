using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CASP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OperationPage OpPage = new();
        private ResultPage ResPage = new();
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(OpPage);
        }
        public void SwitchPages(bool IsOpPage)
        {
            if (IsOpPage){ MainFrame.Navigate(ResPage); } 
            else { MainFrame.Navigate(OpPage); }
        }
    }
}
