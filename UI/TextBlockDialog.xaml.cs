using System.Windows;
using System.Windows.Input;

namespace AndroidDeviceManager.UI
{
    /// <summary>
    /// Logica di interazione per TextBlockDialog.xaml
    /// </summary>
    public partial class TextBlockDialog : Window
    {
        public TextBlockDialog(string rawOutput)
        {
            InitializeComponent();
            textBlock_RawOutput.Text = rawOutput;
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void border_Upper_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
