using System.Windows;

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
            if (IsOpPage) { MainFrame.Navigate(ResPage); }
            else { MainFrame.Navigate(OpPage); }
        }
    }
}
