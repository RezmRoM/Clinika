using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Veterinary_Clinic
{
    public partial class AppointmentsView : UserControl
    {
        public event EventHandler NewAppointmentRequested;
        
        private int _clientId;
        private List<AppointmentModel> _appointments = new List<AppointmentModel>();
        private AppointmentFilterType _currentFilter = AppointmentFilterType.All;
        
        public AppointmentsView(int clientId)
        {
            InitializeComponent();
            
            _clientId = clientId;
            
            // Загрузка записей из базы данных
            LoadAppointments();
            
            // Применение фильтра (по умолчанию - все записи)
            ApplyFilter(_currentFilter);
        }
        
        private void LoadAppointments()
        {
            try
            {
                _appointments.Clear();
                
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
                    
                    string query = $@"
                    SELECT p.priem_id AS zapis_id, p.id_uslugi AS uslugi_id, u.nazvaniye_uslugi AS service_name, 
                           p.data_priema AS date_priema, p.{timeFieldName}, 
                           p.id_sotrudnika AS sotrudnika_id, s.fio AS specialist_name,
                           u.stoimost AS tsena,
                           CASE 
                                WHEN CONVERT(date, p.data_priema) < CONVERT(date, GETDATE()) THEN 2 -- Завершен
                                WHEN CONVERT(date, p.data_priema) = CONVERT(date, GETDATE()) AND 
                                     CONVERT(time, p.{timeFieldName}) < CONVERT(time, GETDATE()) THEN 2 -- Завершен
                                ELSE 1 -- Предстоит
                           END AS status,
                           mk.zhivotnoe_id AS zhivotnoe_id,
                           mk.klichka_zhivotnogo AS pet_name
                    FROM VC_Priemy p
                    LEFT JOIN VC_Uslugi u ON p.id_uslugi = u.usluga_id
                    LEFT JOIN VC_Sotrudniki s ON p.id_sotrudnika = s.sotrudnik_id
                    LEFT JOIN VC_MedKarty mk ON p.priem_id = mk.id_priema
                    WHERE p.id_klienta = @ClientId
                    ORDER BY 
                        CASE WHEN p.data_priema > GETDATE() THEN 0 ELSE 1 END, -- сначала будущие записи
                        p.data_priema, p.{timeFieldName}";
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", _clientId);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AppointmentModel appointment = new AppointmentModel
                                {
                                    AppointmentId = reader.GetInt32(0),
                                    ServiceId = reader.GetInt32(1),
                                    ServiceName = reader.GetString(2),
                                    AppointmentDate = reader.GetDateTime(3),
                                    AppointmentTime = reader.GetString(4),
                                    VeterinarianId = reader.GetInt32(5),
                                    VeterinarianName = reader.GetString(6),
                                    Price = reader.GetDecimal(7),
                                    Status = (AppointmentStatus)reader.GetInt32(8),
                                    PetId = !reader.IsDBNull(9) ? reader.GetInt32(9) : 0,
                                    PetName = !reader.IsDBNull(10) ? reader.GetString(10) : "Не указан"
                                };
                                
                                _appointments.Add(appointment);
                            }
                        }
                    }
                }
                
                // Проверяем, есть ли записи
                if (_appointments.Count == 0)
                {
                    NoAppointmentsText.Visibility = Visibility.Visible;
                }
                else
                {
                    NoAppointmentsText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке записей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ApplyFilter(AppointmentFilterType filter)
        {
            // Сохраняем текущий фильтр
            _currentFilter = filter;
            
            // Очищаем панель записей
            AppointmentsPanel.Children.Clear();
            
            // Фильтруем записи
            List<AppointmentModel> filteredAppointments = new List<AppointmentModel>();
            
            switch (filter)
            {
                case AppointmentFilterType.All:
                    filteredAppointments = _appointments;
                    break;
                    
                case AppointmentFilterType.Upcoming:
                    filteredAppointments = _appointments.FindAll(a => a.Status == AppointmentStatus.Upcoming);
                    break;
                    
                case AppointmentFilterType.Past:
                    filteredAppointments = _appointments.FindAll(a => a.Status == AppointmentStatus.Completed);
                    break;
            }
            
            // Обновляем видимость сообщения об отсутствии записей
            if (filteredAppointments.Count == 0)
            {
                string message = "У вас пока нет ";
                
                switch (filter)
                {
                    case AppointmentFilterType.All:
                        message += "записей на приём.";
                        break;
                        
                    case AppointmentFilterType.Upcoming:
                        message += "предстоящих записей на приём.";
                        break;
                        
                    case AppointmentFilterType.Past:
                        message += "прошедших записей на приём.";
                        break;
                }
                
                message += " Запишитесь на приём прямо сейчас!";
                
                NoAppointmentsText.Text = message;
                NoAppointmentsText.Visibility = Visibility.Visible;
            }
            else
            {
                NoAppointmentsText.Visibility = Visibility.Collapsed;
                
                // Создаем карточки для отфильтрованных записей
                foreach (var appointment in filteredAppointments)
                {
                    AppointmentsPanel.Children.Add(CreateAppointmentCard(appointment));
                }
            }
            
            // Обновляем UI сегментов
            UpdateSegmentsUI(filter);
        }
        
        private void UpdateSegmentsUI(AppointmentFilterType activeFilter)
        {
            // Сбрасываем все сегменты
            AllAppointmentsSegment.Background = (SolidColorBrush)FindResource("SurfaceBrush");
            ((TextBlock)AllAppointmentsSegment.Child).Foreground = (SolidColorBrush)FindResource("TextBrush");
            ((TextBlock)AllAppointmentsSegment.Child).FontWeight = FontWeights.Normal;
            
            UpcomingAppointmentsSegment.Background = (SolidColorBrush)FindResource("SurfaceBrush");
            ((TextBlock)UpcomingAppointmentsSegment.Child).Foreground = (SolidColorBrush)FindResource("TextBrush");
            ((TextBlock)UpcomingAppointmentsSegment.Child).FontWeight = FontWeights.Normal;
            
            PastAppointmentsSegment.Background = (SolidColorBrush)FindResource("SurfaceBrush");
            ((TextBlock)PastAppointmentsSegment.Child).Foreground = (SolidColorBrush)FindResource("TextBrush");
            ((TextBlock)PastAppointmentsSegment.Child).FontWeight = FontWeights.Normal;
            
            // Выделяем активный сегмент
            Border activeSegment;
            
            switch (activeFilter)
            {
                case AppointmentFilterType.All:
                    activeSegment = AllAppointmentsSegment;
                    break;
                    
                case AppointmentFilterType.Upcoming:
                    activeSegment = UpcomingAppointmentsSegment;
                    break;
                    
                case AppointmentFilterType.Past:
                    activeSegment = PastAppointmentsSegment;
                    break;
                    
                default:
                    activeSegment = AllAppointmentsSegment;
                    break;
            }
            
            activeSegment.Background = (SolidColorBrush)FindResource("PrimaryBrush");
            ((TextBlock)activeSegment.Child).Foreground = Brushes.White;
            ((TextBlock)activeSegment.Child).FontWeight = FontWeights.SemiBold;
        }
        
        private UIElement CreateAppointmentCard(AppointmentModel appointment)
        {
            // Создаем карточку записи
            Border card = new Border();
            card.Style = (Style)FindResource("AppointmentCardStyle");
            
            // Создаем содержимое карточки
            Grid grid = new Grid();
            grid.Margin = new Thickness(20);
            
            // Определяем строки
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Верхняя строка - Дата и статус
            Grid topRow = new Grid();
            topRow.Margin = new Thickness(0, 0, 0, 15);
            
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Дата и время
            TextBlock dateTimeText = new TextBlock();
            dateTimeText.Text = $"{appointment.AppointmentDate.ToString("d MMMM yyyy", new CultureInfo("ru-RU"))} в {appointment.AppointmentTime}";
            dateTimeText.FontSize = 16;
            dateTimeText.FontWeight = FontWeights.SemiBold;
            dateTimeText.Foreground = (SolidColorBrush)FindResource("TextBrush");
            dateTimeText.VerticalAlignment = VerticalAlignment.Center;
            
            Grid.SetColumn(dateTimeText, 0);
            topRow.Children.Add(dateTimeText);
            
            // Статус
            Border statusBorder = new Border();
            statusBorder.Style = (Style)FindResource("AppointmentStatusStyle");
            
            switch (appointment.Status)
            {
                case AppointmentStatus.Upcoming:
                    statusBorder.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Зеленый
                    break;
                    
                case AppointmentStatus.Completed:
                    statusBorder.Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Серый
                    break;
                    
                default:
                    statusBorder.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Синий
                    break;
            }
            
            TextBlock statusText = new TextBlock();
            statusText.Text = appointment.Status == AppointmentStatus.Upcoming ? "Предстоит" : "Завершен";
            statusText.Foreground = Brushes.White;
            statusText.FontWeight = FontWeights.SemiBold;
            statusText.FontSize = 12;
            
            statusBorder.Child = statusText;
            
            Grid.SetColumn(statusBorder, 1);
            topRow.Children.Add(statusBorder);
            
            Grid.SetRow(topRow, 0);
            grid.Children.Add(topRow);
            
            // Средняя строка - Услуга и специалист
            StackPanel middleRow = new StackPanel();
            middleRow.Margin = new Thickness(0, 0, 0, 15);
            
            // Услуга
            TextBlock serviceLabel = new TextBlock();
            serviceLabel.Text = "Услуга";
            serviceLabel.FontSize = 14;
            serviceLabel.Opacity = 0.7;
            serviceLabel.Foreground = (SolidColorBrush)FindResource("TextBrush");
            serviceLabel.Margin = new Thickness(0, 0, 0, 3);
            
            middleRow.Children.Add(serviceLabel);
            
            TextBlock serviceText = new TextBlock();
            serviceText.Text = appointment.ServiceName;
            serviceText.FontSize = 16;
            serviceText.FontWeight = FontWeights.Medium;
            serviceText.Foreground = (SolidColorBrush)FindResource("TextBrush");
            serviceText.Margin = new Thickness(0, 0, 0, 10);
            
            middleRow.Children.Add(serviceText);
            
            // Специалист
            TextBlock specialistLabel = new TextBlock();
            specialistLabel.Text = "Специалист";
            specialistLabel.FontSize = 14;
            specialistLabel.Opacity = 0.7;
            specialistLabel.Foreground = (SolidColorBrush)FindResource("TextBrush");
            specialistLabel.Margin = new Thickness(0, 0, 0, 3);
            
            middleRow.Children.Add(specialistLabel);
            
            TextBlock specialistText = new TextBlock();
            specialistText.Text = appointment.VeterinarianName;
            specialistText.FontSize = 16;
            specialistText.FontWeight = FontWeights.Medium;
            specialistText.Foreground = (SolidColorBrush)FindResource("TextBrush");
            
            middleRow.Children.Add(specialistText);
            
            Grid.SetRow(middleRow, 1);
            grid.Children.Add(middleRow);
            
            // Нижняя строка - Питомец и стоимость
            Grid bottomRow = new Grid();
            bottomRow.Margin = new Thickness(0, 0, 0, 10);
            
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Питомец
            StackPanel petPanel = new StackPanel();
            petPanel.Orientation = Orientation.Horizontal;
            petPanel.VerticalAlignment = VerticalAlignment.Center;
            
            TextBlock petLabel = new TextBlock();
            petLabel.Text = "Питомец:";
            petLabel.FontSize = 14;
            petLabel.Opacity = 0.7;
            petLabel.Foreground = (SolidColorBrush)FindResource("TextBrush");
            petLabel.Margin = new Thickness(0, 0, 5, 0);
            
            petPanel.Children.Add(petLabel);
            
            TextBlock petText = new TextBlock();
            petText.Text = appointment.PetName;
            petText.FontSize = 14;
            petText.FontWeight = FontWeights.Medium;
            petText.Foreground = (SolidColorBrush)FindResource("TextBrush");
            
            petPanel.Children.Add(petText);
            
            Grid.SetColumn(petPanel, 0);
            bottomRow.Children.Add(petPanel);
            
            // Стоимость
            TextBlock priceText = new TextBlock();
            priceText.Text = $"{appointment.Price:N0} ₽";
            priceText.FontSize = 16;
            priceText.FontWeight = FontWeights.Bold;
            priceText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
            priceText.HorizontalAlignment = HorizontalAlignment.Right;
            
            Grid.SetColumn(priceText, 1);
            bottomRow.Children.Add(priceText);
            
            Grid.SetRow(bottomRow, 2);
            grid.Children.Add(bottomRow);
            
            // Кнопка отмены (только для предстоящих записей)
            if (appointment.Status == AppointmentStatus.Upcoming)
            {
                Button cancelButton = new Button();
                cancelButton.Content = "Отменить запись";
                cancelButton.Style = (Style)FindResource("CancelAppointmentButtonStyle");
                cancelButton.Tag = appointment.AppointmentId;
                cancelButton.Click += CancelAppointmentButton_Click;
                
                Grid.SetRow(cancelButton, 3);
                grid.Children.Add(cancelButton);
            }
            
            card.Child = grid;
            
            return card;
        }
        
        private void CancelAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int appointmentId)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы уверены, что хотите отменить эту запись на приём?",
                    "Подтверждение отмены",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Отмена записи
                    CancelAppointment(appointmentId);
                    
                    // Перезагрузка списка записей
                    LoadAppointments();
                    ApplyFilter(_currentFilter);
                }
            }
        }
        
        private void CancelAppointment(int appointmentId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    // Получаем информацию о записи
                    string getAppointmentQuery = @"
                    SELECT data_priema, vremia_priema 
                    FROM VC_Priemy 
                    WHERE priem_id = @AppointmentId AND id_klienta = @ClientId";
                    
                    SqlCommand getAppointmentCommand = new SqlCommand(getAppointmentQuery, connection);
                    getAppointmentCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    getAppointmentCommand.Parameters.AddWithValue("@ClientId", _clientId);
                    
                    DateTime appointmentDateTime = DateTime.Now;
                    bool canCancel = false;
                    
                    using (SqlDataReader reader = getAppointmentCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DateTime appointmentDate = reader.GetDateTime(0);
                            string appointmentTime = reader.GetString(1);
                            
                            // Парсим время в формате HH:MM
                            string[] timeParts = appointmentTime.Split(':');
                            int hours = int.Parse(timeParts[0]);
                            int minutes = int.Parse(timeParts[1]);
                            
                            appointmentDateTime = appointmentDate.Date.AddHours(hours).AddMinutes(minutes);
                            
                            // Проверяем, можно ли отменить запись (не менее чем за 2 часа)
                            canCancel = (appointmentDateTime - DateTime.Now).TotalHours >= 2;
                        }
                        else
                        {
                            MessageBox.Show(
                                "Запись не найдена или принадлежит другому клиенту.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            
                            return;
                        }
                    }
                    
                    if (!canCancel)
                    {
                        MessageBox.Show(
                            "Невозможно отменить запись менее чем за 2 часа до приема.",
                            "Отмена невозможна",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        
                        return;
                    }
                    
                    // Удаляем связанные медкарты
                    string deleteMedCardQuery = "DELETE FROM VC_MedKarty WHERE id_priema = @AppointmentId";
                    
                    SqlCommand deleteMedCardCommand = new SqlCommand(deleteMedCardQuery, connection);
                    deleteMedCardCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    deleteMedCardCommand.ExecuteNonQuery();
                    
                    // Удаляем запись на прием
                    string deleteAppointmentQuery = "DELETE FROM VC_Priemy WHERE priem_id = @AppointmentId AND id_klienta = @ClientId";
                    
                    SqlCommand deleteAppointmentCommand = new SqlCommand(deleteAppointmentQuery, connection);
                    deleteAppointmentCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    deleteAppointmentCommand.Parameters.AddWithValue("@ClientId", _clientId);
                    deleteAppointmentCommand.ExecuteNonQuery();
                    
                    MessageBox.Show(
                        "Запись успешно отменена.",
                        "Отмена записи",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при отмене записи: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        #region Обработчики событий фильтрации
        
        private void AllAppointmentsSegment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ApplyFilter(AppointmentFilterType.All);
        }
        
        private void UpcomingAppointmentsSegment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ApplyFilter(AppointmentFilterType.Upcoming);
        }
        
        private void PastAppointmentsSegment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ApplyFilter(AppointmentFilterType.Past);
        }
        
        private void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            NewAppointmentRequested?.Invoke(this, EventArgs.Empty);
        }
        
        #endregion
    }
} 