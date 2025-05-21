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

namespace PlayerApp
{
    /// <summary>
    /// Логика взаимодействия для PlaylistNameDialog.xaml
    /// </summary>
    // PlaylistNameDialog.xaml.cs
    public partial class PlaylistNameDialog : Window
    {
        public string PlaylistName { get; private set; }

        public PlaylistNameDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistName = txtName.Text;
            DialogResult = true;
        }
    }
}
