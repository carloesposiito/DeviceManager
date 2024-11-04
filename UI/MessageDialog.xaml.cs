using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace AndroidDeviceManager.UI
{
    /// <summary>
    /// Logica di interazione per MessageDialog.xaml
    /// </summary>
    public partial class MessageDialog : Window
    {
        public MessageDialog(string message)
        {
            InitializeComponent();

            textBlock_Message.Text = message;
        }

        private void button_Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
