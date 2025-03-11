using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Windows.Resources;
using System.Windows.Threading;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для EmployeeWindow.xaml
    /// </summary>
    public class Appointment
    {
        public int PriemId { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string ClientName { get; set; }
        public string Service { get; set; }
        public string PetName { get; set; }
        public string Notes { get; set; }
        public decimal Cost { get; set; }
        public string Status { get; set; }
    }

    public class DaySchedule
    {
        public DateTime Date { get; set; }
        public string FormattedDate => Date.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
        public ObservableCollection<Appointment> Appointments { get; set; }
    }

    public class Employee
    {
        public int SotrudnikId { get; set; }
        public string FIO { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public string Specialization { get; set; }
        public string ImageUrl { get; set; }
        public string FullName => FIO;
    }

    public partial class EmployeeWindow : Window
    {
        private readonly string _connectionString;
        private Employee _currentEmployee;
        private ObservableCollection<DaySchedule> _schedule;
        private Dictionary<string, UIElement> _pages;
        private DispatcherTimer _autoRefreshTimer;

        public EmployeeWindow(int employeeId)
        {
            InitializeComponent();

            // Получаем строку подключения
            _connectionString = Properties.Settings.Default.connectionString;

            try
            {
                InitializeEmployee(employeeId);
                InitializePages();
                InitializeSchedule();
                SetupEventHandlers();
                SetupAutoRefresh();
                ShowPage("SchedulePage");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при инициализации: {ex.Message}\n\nДетали: {ex.StackTrace}",
                    "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupAutoRefresh()
        {
            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(5);
            _autoRefreshTimer.Tick += (s, e) => RefreshData();
            _autoRefreshTimer.Start();
        }

        private void RefreshData()
        {
            try
            {
                if (_pages["SchedulePage"].Visibility == Visibility.Visible)
                {
                    LoadAppointments();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю, чтобы не мешать работе
                System.Diagnostics.Debug.WriteLine($"Ошибка автообновления: {ex.Message}");
            }
        }

        #region Инициализация

        private void InitializePages()
        {
            _pages = new Dictionary<string, UIElement>
            {
                { "SchedulePage", SchedulePage },
                { "ProfilePage", ProfilePage },
                { "ServicesPage", ServicesPage },
                { "PatientsPage", PatientsPage },
                { "StatisticsPage", StatisticsPage }
            };
        }

        private void InitializeEmployee(int employeeId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            sotrudnik_id,
                            fio,
                            dolzhnost,
                            email,
                            data_rozhdeniya,
                            specializaciya,
                            url_izobrazhenija
                        FROM VC_Sotrudniki 
                        WHERE sotrudnik_id = @EmployeeId", connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _currentEmployee = new Employee
                                {
                                    SotrudnikId = employeeId,
                                    FIO = reader["fio"].ToString(),
                                    Position = reader["dolzhnost"].ToString(),
                                    Email = reader["email"].ToString(),
                                    BirthDate = reader["data_rozhdeniya"] == DBNull.Value ?
                                        DateTime.Now : Convert.ToDateTime(reader["data_rozhdeniya"]),
                                    Specialization = reader["specializaciya"].ToString(),
                                    ImageUrl = reader["url_izobrazhenija"].ToString()
                                };
                            }
                            else
                            {
                                MessageBox.Show("Сотрудник не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                Close();
                                return;
                            }
                        }
                    }
                }

                UpdateEmployeeUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Перебрасываем исключение для обработки на верхнем уровне
            }
        }

        private void UpdateEmployeeUI()
        {
            EmployeeName.Text = _currentEmployee.FullName;
            EmployeePosition.Text = $"{_currentEmployee.Position} - {_currentEmployee.Specialization}";

            // Обновление полей на странице профиля
            FullNameTextBox.Text = _currentEmployee.FullName;
            PositionTextBox.Text = _currentEmployee.Position;
            SpecializationTextBox.Text = _currentEmployee.Specialization;
            EmailTextBox.Text = _currentEmployee.Email;
            BirthDatePicker.SelectedDate = _currentEmployee.BirthDate;

            // Загрузка изображения профиля
            try
            {
                if (!string.IsNullOrEmpty(_currentEmployee.ImageUrl))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(_currentEmployee.ImageUrl, UriKind.RelativeOrAbsolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    ProfileImage.Source = image;
                    DefaultAvatar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ProfileImage.Source = null;
                    DefaultAvatar.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                ProfileImage.Source = null;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        private void InitializeSchedule()
        {
            _schedule = new ObservableCollection<DaySchedule>();
            LoadAppointments();
            AppointmentsControl.ItemsSource = _schedule;
        }

        private void LoadAppointments()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            p.priem_id,
                            p.data_priema,
                            p.vremja_priema,
                            k.fio AS ClientName,
                            u.nazvanie_uslugi AS ServiceName,
                            ISNULL(z.klichka, 'Не указано') AS PetName,
                            p.primechanie AS Notes,
                            u.stoimost AS Cost,
                            ISNULL(p.status, 'Запланирован') AS Status
                        FROM VC_Priemy p
                        JOIN VC_Klienty k ON p.id_klienta = k.klient_id
                        JOIN VC_Uslugi u ON p.id_uslugi = u.usluga_id
                        LEFT JOIN VC_MedKarty m ON p.zapisi_id = m.zapisi_id
                        LEFT JOIN VC_Zhivotnye z ON m.zhivotnoe_id = z.zhivotnoe_id
                        WHERE p.id_sotrudnika = @EmployeeId
                        AND p.data_priema >= @StartDate
                        ORDER BY p.data_priema, p.vremja_priema", connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", _currentEmployee.SotrudnikId);
                        command.Parameters.AddWithValue("@StartDate", DateTime.Today.AddDays(-1)); // Показываем вчерашние записи тоже

                        var appointments = new List<Appointment>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                appointments.Add(new Appointment
                                {
                                    PriemId = Convert.ToInt32(reader["priem_id"]),
                                    Date = Convert.ToDateTime(reader["data_priema"]),
                                    Time = reader["vremja_priema"].ToString(),
                                    ClientName = reader["ClientName"].ToString(),
                                    Service = reader["ServiceName"].ToString(),
                                    PetName = reader["PetName"].ToString(),
                                    Notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : "",
                                    Cost = reader["Cost"] != DBNull.Value ? Convert.ToDecimal(reader["Cost"]) : 0,
                                    Status = reader["Status"].ToString()
                                });
                            }
                        }

                        var groupedAppointments = appointments
                            .GroupBy(a => a.Date.Date)
                            .Select(g => new DaySchedule
                            {
                                Date = g.Key,
                                Appointments = new ObservableCollection<Appointment>(g.OrderBy(a => a.Time))
                            })
                            .OrderBy(d => d.Date);

                        Application.Current.Dispatcher.Invoke(() => {
                            _schedule.Clear();
                            foreach (var day in groupedAppointments)
                            {
                                _schedule.Add(day);
                            }

                            // Обновляем заголовок, показывая количество записей
                            UpdateScheduleHeader(appointments.Count);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке расписания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateScheduleHeader(int count)
        {
            AppointmentsHeader.Text = $"Расписание приёмов ({count})";
        }

        #endregion

        #region Обработчики событий

        private void SetupEventHandlers()
        {
            // Обработчики для кнопок меню
            ScheduleButton.Click += (s, e) => ShowPage("SchedulePage");
            ProfileButton.Click += (s, e) => ShowPage("ProfilePage");
            ServicesButton.Click += (s, e) => ShowPage("ServicesPage");
            PatientsButton.Click += (s, e) => ShowPage("PatientsPage");
            StatisticsButton.Click += (s, e) => ShowPage("StatisticsPage");

            // Обработчик поиска
            SearchBox.TextChanged += (s, e) => FilterAppointments();
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("SchedulePage");
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("ProfilePage");
        }

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("ServicesPage");
            LoadServices();
        }

        private void PatientsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("PatientsPage");
            LoadPatients();
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("StatisticsPage");
            LoadStatistics();
        }

        private void ShowPage(string pageName)
        {
            foreach (var page in _pages)
            {
                page.Value.Visibility = page.Key == pageName ? Visibility.Visible : Visibility.Collapsed;
            }

            // Подсветка активной кнопки меню
            HighlightActiveButton(pageName);
        }

        private void HighlightActiveButton(string pageName)
        {
            // Сбрасываем стили для всех кнопок
            ScheduleButton.ClearValue(Button.FontWeightProperty);
            ProfileButton.ClearValue(Button.FontWeightProperty);
            ServicesButton.ClearValue(Button.FontWeightProperty);
            PatientsButton.ClearValue(Button.FontWeightProperty);
            StatisticsButton.ClearValue(Button.FontWeightProperty);

            // Выделяем активную кнопку
            switch (pageName)
            {
                case "SchedulePage":
                    ScheduleButton.FontWeight = FontWeights.Bold;
                    break;
                case "ProfilePage":
                    ProfileButton.FontWeight = FontWeights.Bold;
                    break;
                case "ServicesPage":
                    ServicesButton.FontWeight = FontWeights.Bold;
                    break;
                case "PatientsPage":
                    PatientsButton.FontWeight = FontWeights.Bold;
                    break;
                case "StatisticsPage":
                    StatisticsButton.FontWeight = FontWeights.Bold;
                    break;
            }
        }

        private void FilterAppointments()
        {
            var searchText = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                LoadAppointments();
                return;
            }

            var filteredSchedule = _schedule
                .Select(day => new DaySchedule
                {
                    Date = day.Date,
                    Appointments = new ObservableCollection<Appointment>(
                        day.Appointments.Where(a =>
                            a.ClientName.ToLower().Contains(searchText) ||
                            a.Service.ToLower().Contains(searchText) ||
                            a.PetName.ToLower().Contains(searchText) ||
                            a.Notes?.ToLower().Contains(searchText) == true
                        ))
                })
                .Where(day => day.Appointments.Any())
                .OrderBy(day => day.Date);

            _schedule.Clear();
            foreach (var day in filteredSchedule)
            {
                _schedule.Add(day);
            }

            // Обновляем количество найденных записей
            int totalCount = filteredSchedule.Sum(d => d.Appointments.Count);
            UpdateScheduleHeader(totalCount);
        }

        private void AppointmentDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var appointment = (Appointment)button.DataContext;
            MessageBox.Show(
                $"Информация о приеме:\n\n" +
                $"Дата: {appointment.Date.ToShortDateString()}\n" +
                $"Время: {appointment.Time}\n" +
                $"Клиент: {appointment.ClientName}\n" +
                $"Питомец: {appointment.PetName}\n" +
                $"Услуга: {appointment.Service}\n" +
                $"Стоимость: {appointment.Cost:C}\n" +
                $"Статус: {appointment.Status}\n\n" +
                $"Примечания: {appointment.Notes}",
                "Детали приема",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void CancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var appointment = (Appointment)button.DataContext;

            var result = MessageBox.Show(
                $"Вы действительно хотите отменить прием?\n\n" +
                $"Клиент: {appointment.ClientName}\n" +
                $"Дата: {appointment.Date.ToShortDateString()} {appointment.Time}",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        using (var command = new SqlCommand(
                            "UPDATE VC_Priemy SET status = 'Отменен' WHERE priem_id = @PriemId",
                            connection))
                        {
                            command.Parameters.AddWithValue("@PriemId", appointment.PriemId);
                            command.ExecuteNonQuery();
                        }
                    }

                    LoadAppointments();
                    MessageBox.Show("Прием успешно отменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отмене приема: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                        UPDATE VC_Sotrudniki 
                        SET 
                            email = @Email,
                            data_rozhdeniya = @BirthDate
                        WHERE sotrudnik_id = @EmployeeId", connection))
                    {
                        command.Parameters.AddWithValue("@Email", EmailTextBox.Text);
                        command.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate);
                        command.Parameters.AddWithValue("@EmployeeId", _currentEmployee.SotrudnikId);
                        command.ExecuteNonQuery();
                    }
                }

                _currentEmployee.Email = EmailTextBox.Text;
                _currentEmployee.BirthDate = BirthDatePicker.SelectedDate.Value;

                MessageBox.Show("Профиль успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Окно с расширенными фильтрами
            var filterWindow = new FilterWindow(_schedule);
            if (filterWindow.ShowDialog() == true)
            {
                // Применяем фильтр из диалога
                _schedule.Clear();
                foreach (var day in filterWindow.FilteredSchedule)
                {
                    _schedule.Add(day);
                }

                // Обновляем заголовок с количеством записей
                int totalCount = filterWindow.FilteredSchedule.Sum(d => d.Appointments.Count);
                UpdateScheduleHeader(totalCount);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                var authWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.GetType().Name == "AuthWindow");
                if (authWindow != null)
                {
                    authWindow.Show();
                }
                Close();
            }
        }

        #endregion

        #region Дополнительные страницы

        private void LoadServices()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            usluga_id,
                            nazvanie_uslugi,
                            stoimost,
                            url_izobrazhenija
                        FROM VC_Uslugi
                        ORDER BY nazvanie_uslugi", connection))
                    {
                        var servicesData = new List<object>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                servicesData.Add(new
                                {
                                    Id = Convert.ToInt32(reader["usluga_id"]),
                                    Name = reader["nazvanie_uslugi"].ToString(),
                                    Cost = reader["stoimost"] != DBNull.Value ? Convert.ToDecimal(reader["stoimost"]) : 0,
                                    ImageUrl = reader["url_izobrazhenija"].ToString()
                                });
                            }
                        }

                        ServicesListView.ItemsSource = servicesData;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatients()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            z.zhivotnoe_id,
                            z.klichka,
                            t.nazvanie AS TipZhivotnogo,
                            z.poroda,
                            z.vozrast,
                            k.fio AS OwnerName
                        FROM VC_Zhivotnye z
                        JOIN VC_Tipy_Zhivotnyh t ON z.id_tipa = t.tip_id
                        JOIN VC_Klienty k ON z.id_klienta = k.klient_id
                        JOIN VC_MedKarty m ON z.zhivotnoe_id = m.zhivotnoe_id
                        JOIN VC_Priemy p ON m.zapisi_id = p.zapisi_id
                        WHERE p.id_sotrudnika = @EmployeeId
                        GROUP BY z.zhivotnoe_id, z.klichka, t.nazvanie, z.poroda, z.vozrast, k.fio
                        ORDER BY k.fio, z.klichka", connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", _currentEmployee.SotrudnikId);

                        var patientsData = new List<object>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                patientsData.Add(new
                                {
                                    Id = Convert.ToInt32(reader["zhivotnoe_id"]),
                                    Name = reader["klichka"].ToString(),
                                    Type = reader["TipZhivotnogo"].ToString(),
                                    Breed = reader["poroda"].ToString(),
                                    Age = reader["vozrast"] != DBNull.Value ? Convert.ToInt32(reader["vozrast"]) : 0,
                                    Owner = reader["OwnerName"].ToString()
                                });
                            }
                        }

                        PatientsListView.ItemsSource = patientsData;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пациентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Статистика по месяцам
                    using (var command = new SqlCommand(@"
                        SELECT 
                            MONTH(data_priema) AS Month,
                            YEAR(data_priema) AS Year,
                            COUNT(*) AS AppointmentCount,
                            SUM(u.stoimost) AS TotalRevenue
                        FROM VC_Priemy p
                        JOIN VC_Uslugi u ON p.id_uslugi = u.usluga_id
                        WHERE p.id_sotrudnika = @EmployeeId 
                        AND p.data_priema >= DATEADD(month, -6, GETDATE())
                        GROUP BY MONTH(data_priema), YEAR(data_priema)
                        ORDER BY Year, Month", connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", _currentEmployee.SotrudnikId);

                        var monthlyStats = new List<object>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int month = Convert.ToInt32(reader["Month"]);
                                int year = Convert.ToInt32(reader["Year"]);
                                string monthName = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);

                                monthlyStats.Add(new
                                {
                                    MonthYear = $"{monthName} {year}",
                                    Count = Convert.ToInt32(reader["AppointmentCount"]),
                                    Revenue = reader["TotalRevenue"] != DBNull.Value ? Convert.ToDecimal(reader["TotalRevenue"]) : 0
                                });
                            }
                        }

                        MonthlyStatsListView.ItemsSource = monthlyStats;
                    }

                    // Статистика по услугам
                    using (var command = new SqlCommand(@"
                        SELECT 
                            u.nazvanie_uslugi,
                            COUNT(*) AS ServiceCount,
                            SUM(u.stoimost) AS TotalRevenue
                        FROM VC_Priemy p
                        JOIN VC_Uslugi u ON p.id_uslugi = u.usluga_id
                        WHERE p.id_sotrudnika = @EmployeeId
                        GROUP BY u.nazvanie_uslugi
                        ORDER BY ServiceCount DESC", connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", _currentEmployee.SotrudnikId);

                        var serviceStats = new List<object>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                serviceStats.Add(new
                                {
                                    ServiceName = reader["nazvanie_uslugi"].ToString(),
                                    Count = Convert.ToInt32(reader["ServiceCount"]),
                                    Revenue = reader["TotalRevenue"] != DBNull.Value ? Convert.ToDecimal(reader["TotalRevenue"]) : 0
                                });
                            }
                        }

                        ServiceStatsListView.ItemsSource = serviceStats;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Управление окном

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

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
            Close();
        }

        #endregion
    }

    public class FilterWindow : Window
    {
        public ObservableCollection<DaySchedule> FilteredSchedule { get; private set; }

        public FilterWindow(ObservableCollection<DaySchedule> originalSchedule)
        {
            // Здесь должна быть инициализация окна фильтров
            // В реальном приложении здесь был бы XAML-код окна
        }
    }
}
