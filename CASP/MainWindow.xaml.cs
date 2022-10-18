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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CASP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize Event.
            this.SizeChanged += MainWindow_SizeChanged;
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navbar_Background.Width = ActualWidth;
        }

        private void Operation_Click_1(object sender, RoutedEventArgs e)
        {
            Thickness temp = Result.BorderThickness;
            temp.Top = 0.0;
            temp.Bottom = 0.0;
            Operation.BorderThickness = temp;
            Operation.Background = Brushes.Gray;

            temp.Top = 1.0;
            temp.Bottom = 1.0;
            Result.BorderThickness = temp;
            Result.Background = Brushes.LightGray;
        }

        private void Result_Click(object sender, RoutedEventArgs e)
        {
            Thickness temp = Result.BorderThickness;
            temp.Top = 0.0;
            temp.Bottom = 0.0;
            Result.BorderThickness = temp;
            Result.Background = Brushes.Gray;

            temp.Top = 1.0;
            temp.Bottom = 1.0;
            Operation.BorderThickness = temp;
            Operation.Background = Brushes.LightGray;
        }
    }
}
