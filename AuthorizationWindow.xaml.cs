using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationWindow.xaml
    /// </summary>
    public partial class AuthorizationWindow : Window
    {
        private readonly Random random = new Random();
        private readonly List<Line> gridLines = new List<Line>();
        private readonly DispatcherTimer animationTimer;

        public AuthorizationWindow()
        {
            InitializeComponent();
            CreateAnimatedGrid();

            // Настройка таймера для анимации
            animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Обработчики для кнопок управления окном
            MinimizeButton.MouseEnter += Button_MouseEnter;
            MaximizeButton.MouseEnter += Button_MouseEnter;
            CloseButton.MouseEnter += Button_MouseEnter;
        }

        private void CreateAnimatedGrid()
        {
            // Создаем сетку линий с оттенками основного цвета клиники
            for (int i = 0; i < 20; i++)
            {
                var line = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 78, 205, 196)), // Полупрозрачный основной цвет
                    StrokeThickness = 1,
                    X1 = random.Next(0, (int)AnimatedGrid.ActualWidth),
                    Y1 = random.Next(0, (int)AnimatedGrid.ActualHeight),
                    X2 = random.Next(0, (int)AnimatedGrid.ActualWidth),
                    Y2 = random.Next(0, (int)AnimatedGrid.ActualHeight)
                };

                AnimatedGrid.Children.Add(line);
                gridLines.Add(line);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Анимируем каждую линию в сетке
            foreach (var line in gridLines)
            {
                // Анимация конечной точки X
                var animation = new DoubleAnimation
                {
                    To = random.Next(0, Math.Max(1, (int)AnimatedGrid.ActualWidth)),
                    Duration = TimeSpan.FromSeconds(2),
                    EasingFunction = new QuadraticEase()
                };
                line.BeginAnimation(Line.X2Property, animation);

                // Анимация конечной точки Y
                animation = new DoubleAnimation
                {
                    To = random.Next(0, Math.Max(1, (int)AnimatedGrid.ActualHeight)),
                    Duration = TimeSpan.FromSeconds(2),
                    EasingFunction = new QuadraticEase()
                };
                line.BeginAnimation(Line.Y2Property, animation);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            // Анимация при наведении на кнопку управления окном
            var button = (Button)sender;
            var animation = new ColorAnimation
            {
                To = button == CloseButton ? Colors.Red : (Color)FindResource("AccentColor"),
                Duration = TimeSpan.FromSeconds(0.3)
            };

            if (button.Background is SolidColorBrush brush)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }

        // Обработчик для перетаскивания окна
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Обработчики кнопок управления окном
        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Обработчик входа в систему
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    // Пытаемся найти пользователя среди клиентов
                    string queryKlient = @"SELECT k.klient_id, r.nazvaniye, k.parol 
                                         FROM VC_Klienty k 
                                         JOIN VC_Roli r ON k.id_roli = r.rol_id 
                                         WHERE (k.login = @Login OR k.email = @Login OR k.telefon = @Login)";

                    // Проверяем в таблице клиентов
                    using (SqlCommand command = new SqlCommand(queryKlient, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string role = reader.GetString(1);

                                // Проверяем, существует ли поле пароля и не является ли оно NULL
                                string dbPassword = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                                // Проверяем пароль
                                if (password == dbPassword)
                                {
                                    try
                                    {
                                        // Открываем окно клиента
                                        ClientWindow clientWindow = new ClientWindow(userId);
                                        clientWindow.Show();
                                        this.Close();
                                        return;
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Ошибка при открытии окна клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    // Попытка входа для сотрудников пока отключена
                    
                    // Если клиент не найден или пароль неверный, ищем среди сотрудников
                    string querySotrudnik = @"SELECT s.sotrudnik_id, r.nazvaniye, s.parol 
                                            FROM VC_Sotrudniki s 
                                            JOIN VC_Roli r ON s.id_roli = r.rol_id 
                                            WHERE (s.login = @Login OR s.email = @Login)";
                                            
                    using (SqlCommand command = new SqlCommand(querySotrudnik, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string role = reader.GetString(1);
                                string dbPassword = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                                
                                // Проверяем пароль
                                if (password == dbPassword)
                                {
                                    // Открываем окно сотрудника
                                    EmployeeWindow employeeWindow = new EmployeeWindow(userId);
                                    employeeWindow.Show();
                                    this.Close();
                                    return;
                                }
                            }
                        }
                    }
                    

                    // Если пользователь не найден или пароль неверный
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Открытие окна регистрации
        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }
    }
}
