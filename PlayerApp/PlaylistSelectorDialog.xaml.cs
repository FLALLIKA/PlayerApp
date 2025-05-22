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
    /// Логика взаимодействия для PlaylistSelectorDialog.xaml
    /// </summary>
    public partial class PlaylistSelectorDialog : Window
    {
        public Playlist SelectedPlaylist { get; private set; }
        private List<Playlist> _originalPlaylists; // Объявляем поле класса

        public PlaylistSelectorDialog(List<Playlist> playlists)
        {
            InitializeComponent();
            _originalPlaylists = playlists; // Сохраняем оригинальный список
            UpdateListView();
        }

        private void UpdateListView()
        {
            listBoxPlaylists.ItemsSource = _originalPlaylists
                .OrderBy(p => p.Category?.Name)
                .ThenBy(p => p.Name)
                .ToList();
            listBoxPlaylists.DisplayMemberPath = "Name";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlaylist = listBoxPlaylists.SelectedItem as Playlist;
            DialogResult = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var playlist = listBoxPlaylists.SelectedItem as Playlist;
            if (playlist != null)
            {
                if (MessageBox.Show($"Удалить плейлист '{playlist.Name}'?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DatabaseHelper.DeletePlaylist(playlist.Id);
                    _originalPlaylists.Remove(playlist); // Удаляем из оригинального списка
                    UpdateListView(); // Обновляем отображение
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
