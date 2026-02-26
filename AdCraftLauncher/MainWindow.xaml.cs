using CmlLib.Core;
using CmlLib.Core.Auth;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace AdCraftLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
        public MainWindow()
        {
            InitializeComponent();
        }
        // Этот метод позволяет перетаскивать окно мышкой за любую пустую область
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Этот метод закрывает лаунчер при нажатии на наш кастомный крестик в правом верхнем углу
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверка никнейма
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                StatusText.Text = "Введите никнейм!";
                return;
            }

            // Блокируем интерфейс
            LaunchButton.IsEnabled = false;
            UsernameBox.IsEnabled = false;

            try
            {
                StatusText.Text = "Инициализация...";

                // 2. Настраиваем путь к игре
                // BasePath - это папка, где лежит exe. 
                // Так как папки libraries/versions лежат рядом с exe, используем этот путь.
                var path = new MinecraftPath(BasePath);
                var launcher = new CMLauncher(path);

                // Подписка на события (для полоски загрузки)
                launcher.FileChanged += (args) =>
                    Dispatcher.Invoke(() => StatusText.Text = $"Загрузка: {args.FileName}");

                launcher.ProgressChanged += (s, args) =>
                    Dispatcher.Invoke(() => DownloadProgress.Value = args.ProgressPercentage);

                // 3. Ищем нашу портативную Java
                // Она должна лежать в папке "java" рядом с exe
                string customJavaPath = System.IO.Path.Combine(BasePath, "java", "bin", "javaw.exe");

                var launchOption = new MLaunchOption
                {
                    MaximumRamMb = 4096, // Выделяем 4 ГБ памяти
                    Session = MSession.CreateOfflineSession(UsernameBox.Text),
                };

                // Если нашли нашу встроенную Java - используем её
                if (File.Exists(customJavaPath))
                {
                    launchOption.JavaPath = customJavaPath;
                }
                else
                {
                    // Если папки java нет (вдруг удалили), CmlLib попытается найти системную Java
                    // Но лучше предупредить в логах
                    StatusText.Text = "Встроенная Java не найдена, ищем в системе...";
                }

                // 4. ВЕРСИЯ ДЛЯ ЗАПУСКА
                // Вставь сюда ТОЧНОЕ название папки из AdCraft_Release/versions/
                string versionToLaunch = "1.20.1-forge-47.4.10";

                StatusText.Text = "Проверка файлов...";

                // Создаем процесс игры
                var process = await launcher.CreateProcessAsync(versionToLaunch, launchOption);

                StatusText.Text = "Запуск игры...";

                // Старт!
                process.Start();

                // Сворачиваем лаунчер
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска:\n{ex.Message}\n\nПроверьте, что все папки (libraries, versions, java) на месте.",
                                "AdCraft Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка запуска";
            }
            finally
            {
                // Разблокируем интерфейс (если игра упала или закрылась)
                LaunchButton.IsEnabled = true;
                UsernameBox.IsEnabled = true;
                DownloadProgress.Value = 0;
            }
        }
    }
}
