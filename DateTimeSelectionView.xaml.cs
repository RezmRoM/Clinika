using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для DateTimeSelectionView.xaml
    /// </summary>
    public partial class DateTimeSelectionView : UserControl
    {
        private int _serviceId;
        private string _serviceName;
        private DateTime _selectedDate;
        private string _selectedTime;
        private TimeSpan _selectedTimeSpan;
        private List<string> _bookedTimes;
        
        public event EventHandler<DateTime_TimeSelectedEventArgs> DateTimeSelected;
        public event EventHandler BackToServices;

        public DateTimeSelectionView(int serviceId, string serviceName)
        {
            InitializeComponent();
            
            _serviceId = serviceId;
            _serviceName = serviceName;
            _bookedTimes = new List<string>();
            
            // Устанавливаем текущую дату как выбранную по умолчанию
            _selectedDate = DateTime.Today;
            AppointmentCalendar.SelectedDate = _selectedDate;
            
            // Блокируем прошедшие даты
            AppointmentCalendar.BlackoutDates.AddDatesInPast();
            
            // Загружаем доступное время для выбранной даты
            LoadAvailableTimes();
        }
        
        private void LoadAvailableTimes()
        {
            try
            {
                _bookedTimes.Clear();
                
                // Очищаем списки времени
                MorningTimeSlots.Children.Clear();
                DayTimeSlots.Children.Clear();
                EveningTimeSlots.Children.Clear();
                
                // Получаем занятое время из базы данных
                GetBookedTimes();
                
                // Генерируем доступное время с 9:00 до 20:00 с интервалом в 30 минут
                DateTime startTime = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day, 9, 0, 0);
                DateTime endTime = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day, 20, 0, 0);
                
                while (startTime <= endTime)
                {
                    string timeString = startTime.ToString("HH:mm");
                    
                    // Проверяем, не занято ли это время
                    if (!_bookedTimes.Contains(timeString))
                    {
                        // Создаем кнопку для выбора времени
                        Button timeButton = new Button
                        {
                            Content = timeString,
                            Style = (Style)FindResource("TimeSlotButton"),
                            Tag = timeString
                        };
                        
                        timeButton.Click += TimeButton_Click;
                        
                        // Распределяем по временным блокам
                        if (startTime.Hour < 12)
                        {
                            MorningTimeSlots.Children.Add(timeButton);
                        }
                        else if (startTime.Hour < 18)
                        {
                            DayTimeSlots.Children.Add(timeButton);
                        }
                        else
                        {
                            EveningTimeSlots.Children.Add(timeButton);
                        }
                    }
                    
                    startTime = startTime.AddMinutes(30);
                }
                
                // Показываем или скрываем блоки времени в зависимости от наличия доступного времени
                MorningExpander.Visibility = MorningTimeSlots.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                DayExpander.Visibility = DayTimeSlots.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                EveningExpander.Visibility = EveningTimeSlots.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                
                // Показываем сообщение, если нет доступного времени
                if (MorningTimeSlots.Children.Count == 0 && 
                    DayTimeSlots.Children.Count == 0 && 
                    EveningTimeSlots.Children.Count == 0)
                {
                    MessageBox.Show("На выбранную дату нет доступного времени для записи.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // Сбрасываем выбранное время
                _selectedTime = null;
                _selectedTimeSpan = TimeSpan.Zero;
                NextButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке доступного времени: {ex.Message}", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void GetBookedTimes()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    // Получаем информацию о столбцах таблицы VC_Priemy
                    string tableInfoQuery = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'VC_Priemy'";
                    
                    List<string> columns = new List<string>();
                    using (SqlCommand infoCommand = new SqlCommand(tableInfoQuery, connection))
                    {
                        using (SqlDataReader infoReader = infoCommand.ExecuteReader())
                        {
                            while (infoReader.Read())
                            {
                                columns.Add(infoReader.GetString(0).ToLower());
                            }
                        }
                    }
                    
                    // Определяем имя поля для времени приема
                    string timeFieldName = columns.Contains("vremia_priema") ? "vremia_priema" : 
                                          (columns.Contains("vremia_prriema") ? "vremia_prriema" : "vremia_priema");
                    
                    // Логируем найденные столбцы для отладки
                    System.Diagnostics.Debug.WriteLine($"Найденное имя поля для времени: {timeFieldName}");
                    System.Diagnostics.Debug.WriteLine($"Все столбцы таблицы VC_Priemy: {string.Join(", ", columns)}");
                    
                    // Основной запрос для получения занятого времени
                    string query = $@"
                    SELECT {timeFieldName} 
                    FROM VC_Priemy 
                    WHERE CONVERT(date, data_priema) = @SelectedDate";
                    
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@SelectedDate", _selectedDate);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string bookedTime = reader.GetString(0);
                            _bookedTimes.Add(bookedTime);
                            System.Diagnostics.Debug.WriteLine($"Добавлено забронированное время: {bookedTime}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении занятого времени: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AppointmentCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentCalendar.SelectedDate.HasValue)
            {
                _selectedDate = AppointmentCalendar.SelectedDate.Value;
                LoadAvailableTimes();
            }
        }
        
        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string time)
            {
                // Сбрасываем стиль всех кнопок
                foreach (UIElement element in MorningTimeSlots.Children)
                {
                    if (element is Button btn)
                    {
                        btn.Background = null;
                        btn.Foreground = null;
                    }
                }
                
                foreach (UIElement element in DayTimeSlots.Children)
                {
                    if (element is Button btn)
                    {
                        btn.Background = null;
                        btn.Foreground = null;
                    }
                }
                
                foreach (UIElement element in EveningTimeSlots.Children)
                {
                    if (element is Button btn)
                    {
                        btn.Background = null;
                        btn.Foreground = null;
                    }
                }
                
                // Выделяем выбранную кнопку
                button.Background = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
                button.Foreground = System.Windows.Media.Brushes.White;
                
                // Сохраняем выбранное время
                _selectedTime = time;
                
                // Преобразуем строку времени в TimeSpan
                string[] timeParts = time.Split(':');
                _selectedTimeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                
                // Активируем кнопку продолжения
                NextButton.IsEnabled = true;
            }
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToServices?.Invoke(this, EventArgs.Empty);
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedTime))
            {
                DateTime_TimeSelectedEventArgs args = new DateTime_TimeSelectedEventArgs
                {
                    SelectedDate = _selectedDate,
                    SelectedTime = _selectedTime,
                    SelectedTimeSpan = _selectedTimeSpan,
                    ServiceId = _serviceId,
                    ServiceName = _serviceName
                };
                
                DateTimeSelected?.Invoke(this, args);
            }
        }
    }
} 