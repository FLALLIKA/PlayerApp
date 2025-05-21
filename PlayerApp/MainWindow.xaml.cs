using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayerApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Плейлист со списком путей к трекам
        private List<string> playlist = new List<string>();
        // Поток для чтения аудиофайла
        private AudioFileReader audioFileReader;
        // Устройство вывода звука
        private WaveOutEvent outputDevice;
        // Индекс текущего воспроизводимого трека
        private int currentTrackIndex = -1;
        // Флаг состояния "без звука"
        private bool isMuted = false;
        // Последнее значение громкости (до отключения звука)
        private float lastVolume = 0.5f;
        // Флаг для отслеживания, перетаскивает ли пользователь ползунок прогресса
        private bool isDraggingSlider = false;
        // Таймер обновления положения ползунка прогресса
        private DispatcherTimer progressTimer = new DispatcherTimer();

        private bool isManualTrackChange = false;

        private bool isStopRequested = false;
        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();

            // Устанавливаем громкость из настроек
            sliderVolume.Value = Properties.Settings.Default.Volume * 100;
            lastVolume = Properties.Settings.Default.Volume;

            // Настраиваем таймер
            progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            progressTimer.Tick += ProgressTimer_Tick;

            // Загружаем треки из предыдущей сессии
            LoadPlaylistFromLastSession();
        }

        // Открытие и загрузка файлов
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Аудиофайлы|*.mp3;*.wav;*.aac;*.wma"
            };

            if (ofd.ShowDialog() == true)
            {
                playlist = ofd.FileNames.ToList();
                listBoxTracks.Items.Clear();

                foreach (var file in playlist)
                    listBoxTracks.Items.Add(System.IO.Path.GetFileName(file));

                listBoxTracks.SelectedIndex = 0;
                currentTrackIndex = 0;

                SavePlaylistToSession();
            }
        }

        // Кнопка воспроизведения
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null)
            {
                if (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    // Защита от повторного воспроизведения
                    return;
                }
                else if (outputDevice.PlaybackState == PlaybackState.Paused)
                {
                    // Возобновляем с паузы
                    outputDevice.Play();
                    progressTimer.Start();
                    return;
                }
            }

            if (currentTrackIndex == -1 || playlist.Count == 0)
            {
                MessageBox.Show("Выберите трек для воспроизведения.");
                return;
            }

            try
            {
                StopPlayback();

                audioFileReader = new AudioFileReader(playlist[currentTrackIndex]);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFileReader);
                outputDevice.PlaybackStopped += OnPlaybackStopped;

                SetVolume();
                outputDevice.Play();
                progressTimer.Start();

                lblNowPlaying.Text = "Сейчас играет: " + System.IO.Path.GetFileName(playlist[currentTrackIndex]);
                SavePlaybackState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка воспроизведения: " + ex.Message);
            }
        }

        // Пауза
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();
                progressTimer.Stop();
            }
        }


        // Остановка
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null && audioFileReader != null)
            {
                isStopRequested = true;

                outputDevice.Stop();
                audioFileReader.CurrentTime = TimeSpan.Zero;
                sliderProgress.Value = 0;

                progressTimer.Stop();
                lblNowPlaying.Text = "Сейчас играет: -";
            }
        }

        // Очистить плейлист
        private void btnClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();

            playlist.Clear();
            listBoxTracks.Items.Clear();
            currentTrackIndex = -1;
            lblNowPlaying.Text = "Сейчас играет: -";

            // Удаляем сохранённый плейлист и последний трек
            if (File.Exists("lastPlaylist.txt"))
                File.Delete("lastPlaylist.txt");

            Properties.Settings.Default.LastTrack = "";
            Properties.Settings.Default.Save();
        }

        // Изменение громкости
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lastVolume = (float)(sliderVolume.Value / 100.0);
            Properties.Settings.Default.Volume = lastVolume;
            Properties.Settings.Default.Save();

            if (!isMuted)
                SetVolume();
        }

        // Включение/отключение звука
        private void chkMute_Checked(object sender, RoutedEventArgs e)
        {
            isMuted = chkMute.IsChecked == true;
            SetVolume();
        }

        // Применение текущей громкости к потоку
        private void SetVolume()
        {
            if (audioFileReader != null)
                audioFileReader.Volume = isMuted ? 0.0f : lastVolume;
        }

        // Смена выбранного трека
        private void listBoxTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxTracks.SelectedIndex >= 0)
                currentTrackIndex = listBoxTracks.SelectedIndex;
        }

        // Остановка текущего воспроизведения и освобождение ресурсов
        private void StopPlayback()
        {
            // Отписываемся от события, чтобы избежать рекурсии
            if (outputDevice != null)
            {
                outputDevice.PlaybackStopped -= OnPlaybackStopped;
            }

            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;

            audioFileReader?.Dispose();
            audioFileReader = null;

            progressTimer.Stop();
            sliderProgress.Value = 0;

            // Снова подписываемся на событие
            if (outputDevice != null)
            {
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
        }

        // Обработка окончания воспроизведения
        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Если остановка была вызвана вручную (кнопкой Стоп или переключением трека) - ничего не делаем
                if (isStopRequested || isManualTrackChange)
                {
                    isStopRequested = false;
                    return;
                }

                // Автоматический переход
                if (chkRepeat.IsChecked == true)
                {
                    RestartCurrentTrack();
                }
                else
                {
                    btnNext_Click(null, null);
                }
            });
        }

        // Обновление ползунка прогресса
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (audioFileReader != null && !isDraggingSlider)
            {
                sliderProgress.Maximum = audioFileReader.TotalTime.TotalSeconds;
                sliderProgress.Value = audioFileReader.CurrentTime.TotalSeconds;
            }
        }

        // Изменение прогресса пользователем
        private void sliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isDraggingSlider && audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(sliderProgress.Value);
            }
        }

        // Начало перетаскивания ползунка
        private void sliderProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void sliderProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (audioFileReader != null)
            {
                var slider = sender as Slider;
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(slider.Value);
            }
            isDraggingSlider = false;
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count == 0) return;

            // Получаем индекс предыдущего трека
            int newIndex = currentTrackIndex - 1;
            if (newIndex < 0) newIndex = playlist.Count - 1;

            // Переключаемся только если это другой трек
            if (newIndex != currentTrackIndex)
            {
                ChangeTrack(newIndex);
            }
            else
            {
                // Если это тот же трек (в списке 1 трек), просто перезапускаем
                RestartCurrentTrack();
            }
        }

        // Воспроизведение следующего трека
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count == 0) return;

            // Получаем индекс следующего трека
            int newIndex = currentTrackIndex + 1;
            if (newIndex >= playlist.Count) newIndex = 0;

            ChangeTrack(newIndex);
        }

        private void ChangeTrack(int newIndex)
        {
            // Сохраняем состояние воспроизведения
            bool wasPlaying = (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing);

            StopPlayback();
            currentTrackIndex = newIndex;
            listBoxTracks.SelectedIndex = newIndex;

            if (wasPlaying)
            {
                btnPlay_Click(null, null);
            }
        }

        private void RestartCurrentTrack()
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                // Просто перематываем в начало и продолжаем воспроизведение
                audioFileReader.CurrentTime = TimeSpan.Zero;
            }
            else
            {
                // Запускаем трек заново
                StopPlayback();
                btnPlay_Click(null, null);
            }
        }

        // Сохранение текущего трека в настройки
        private void SavePlaybackState()
        {
            if (currentTrackIndex >= 0 && currentTrackIndex < playlist.Count)
            {
                Properties.Settings.Default.LastTrack = playlist[currentTrackIndex];
                Properties.Settings.Default.Save();
            }
        }

        // Сохранение плейлиста между сессиями в файл
        private void SavePlaylistToSession()
        {
            File.WriteAllLines("lastPlaylist.txt", playlist);
        }

        // Загрузка плейлиста и восстановление последнего трека при запуске
        private void LoadPlaylistFromLastSession()
        {
            try
            {
                if (File.Exists("lastPlaylist.txt"))
                {
                    playlist = File.ReadAllLines("lastPlaylist.txt").ToList();
                    listBoxTracks.Items.Clear();

                    foreach (var file in playlist)
                        listBoxTracks.Items.Add(System.IO.Path.GetFileName(file));

                    string lastTrack = Properties.Settings.Default.LastTrack;
                    int index = playlist.IndexOf(lastTrack);
                    if (index >= 0)
                    {
                        currentTrackIndex = index;
                        listBoxTracks.SelectedIndex = index;
                    }
                }
            }
            catch
            {
                // Защита от ошибок загрузки
            }
        }

        // Сохранение в БД
        private void btnSaveToDb_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count == 0)
            {
                MessageBox.Show("Плейлист пуст. Нечего сохранять.");
                return;
            }

            // Сначала выбираем категорию
            var categories = DatabaseHelper.GetAllCategories();
            var categoryDialog = new CategorySelectorDialog(categories);

            if (categoryDialog.ShowDialog() == true)
            {
                int categoryId;

                if (categoryDialog.AddNewCategorySelected)
                {
                    // Диалог для ввода имени новой категории
                    var inputDialog = new InputDialog("Введите название новой категории:");
                    if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.InputText))
                    {
                        categoryId = DatabaseHelper.AddCategory(inputDialog.InputText);
                    }
                    else
                    {
                        return; // Пользователь отменил ввод
                    }
                }
                else
                {
                    categoryId = categoryDialog.SelectedCategory?.Id ?? 0;
                    if (categoryId == 0) return;
                }

                // Затем вводим имя плейлиста
                var nameDialog = new PlaylistNameDialog();
                if (nameDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(nameDialog.PlaylistName))
                {
                    try
                    {
                        DatabaseHelper.SavePlaylist(nameDialog.PlaylistName, categoryId, playlist);
                        MessageBox.Show("Плейлист успешно сохранён!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                    }
                }
            }
        }

        // Загрузка из БД
        private void btnLoadFromDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сначала выбираем категорию
                var categories = DatabaseHelper.GetAllCategories();
                if (categories.Count == 0)
                {
                    MessageBox.Show("Нет доступных категорий.");
                    return;
                }

                var categoryDialog = new CategorySelectorDialog(categories);
                if (categoryDialog.ShowDialog() == true && categoryDialog.SelectedCategory != null)
                {
                    // Затем выбираем плейлист из выбранной категории
                    var playlists = DatabaseHelper.GetPlaylistsByCategory(categoryDialog.SelectedCategory.Id);
                    if (playlists.Count == 0)
                    {
                        MessageBox.Show("В выбранной категории нет плейлистов.");
                        return;
                    }

                    var playlistDialog = new PlaylistSelectorDialog(playlists);
                    if (playlistDialog.ShowDialog() == true && playlistDialog.SelectedPlaylist != null)
                    {
                        // Загружаем выбранный плейлист
                        playlist = playlistDialog.SelectedPlaylist.Tracks;
                        listBoxTracks.Items.Clear();

                        foreach (var file in playlist)
                            listBoxTracks.Items.Add(Path.GetFileName(file));

                        if (playlist.Count > 0)
                        {
                            listBoxTracks.SelectedIndex = 0;
                            currentTrackIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}");
            }
        }

    }
}
