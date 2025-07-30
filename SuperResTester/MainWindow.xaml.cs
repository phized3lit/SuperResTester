using OpenCvSharp;
using SuperResTester.Globals;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SuperResTester
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    BitmapSource? bitmapSource = Clipboard.GetImage();
                    BitmapImage? bitmapImage = ConverterFacade.BitmapSourceToBitmapImage(bitmapSource);
                    vm.OriginalImage = bitmapImage;
                }
            }
        }
    }
}