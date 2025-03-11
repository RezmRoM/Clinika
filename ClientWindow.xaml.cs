using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        // Текущий клиент
        private int currentClientId;
        private ClientModel currentClient;

        // Данные для процесса записи на прием
        private ServiceModel selectedService;
        private DateTime selectedDate;
        private string selectedTime;
        private int selectedVeterinarianId;
        private string selectedVeterinarianName;

        #region Инициализация

        public ClientWindow(int clientId)
        {
            InitializeComponent();

            currentClientId = clientId;

            // Загрузка данных клиента
            LoadClientData();

            // Отображение экрана услуг по умолчанию
            ShowServicesView();
        }

        private void LoadClientData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT klient_id, fio, telefon, email, login, url_izobrazhenija 
                                     FROM VC_Klienty 
                                     WHERE klient_id = @ClientId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", currentClientId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Получаем данные из существующих столбцов с проверкой на null
                                currentClient = new ClientModel
                                {
                                    ClientId = reader.GetInt32(0),
                                    FullName = !reader.IsDBNull(1) ? reader.GetString(1) : "Клиент",
                                    Phone = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                    Email = !reader.IsDBNull(3) ? reader.GetString(3) : "",
                                    Login = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                    ImageUrl = !reader.IsDBNull(5) ? reader.GetString(5) : ""
                                };

                                // Обновляем информацию в интерфейсе
                                if (UserNameText != null)
                                {
                                    UserNameText.Text = currentClient.FullName;
                                }

                                // Загрузка изображения профиля, если доступно
                                if (!string.IsNullOrEmpty(currentClient.ImageUrl) && ProfileImage != null)
                                {
                                    try
                                    {
                                        ProfileImage.Source = new BitmapImage(new Uri(currentClient.ImageUrl));
                                    }
                                    catch (Exception ex)
                                    {
                                        // Если не удалось загрузить изображение, используем стандартное
                                        // и пишем ошибку в лог
                                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Клиент с ID {currentClientId} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Навигация

        public void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowServicesView();
        }

        public void AppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAppointmentsView();
        }

        public void MedicalCardsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMedicalCardsView();
        }

        public void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ShowProfileView();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Выход из системы
            AuthorizationWindow authWindow = new AuthorizationWindow();
            authWindow.Show();
            this.Close();
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
            Application.Current.Shutdown();
        }

        #endregion

        #region Отображение представлений

        private void ShowServicesView()
        {
            try
            {
                // Сбрасываем состояние процесса записи
                selectedService = null;
                selectedVeterinarianId = 0;

                // Проверяем, что MainContent существует
                if (MainContent == null)
                {
                    MessageBox.Show("Ошибка инициализации интерфейса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем новый view для услуг
                ServicesView servicesView = new ServicesView();
                servicesView.ServiceSelected += OnServiceSelected;

                // Отображаем view в основном контенте
                MainContent.Content = servicesView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении списка услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDateTimeSelectionView()
        {
            if (selectedService == null)
            {
                ShowServicesView();
                return;
            }

            DateTimeSelectionView dateTimeView = new DateTimeSelectionView(selectedService.ServiceId, selectedService.Name);
            dateTimeView.DateTimeSelected += OnDateTimeSelected;
            dateTimeView.BackToServices += (s, e) => ShowServicesView();
            MainContent.Content = dateTimeView;
        }

        private void ShowVeterinarianSelectionView()
        {
            if (selectedService == null || selectedDate == DateTime.MinValue)
            {
                ShowDateTimeSelectionView();
                return;
            }

            VeterinarianSelectionView veterinarianView = new VeterinarianSelectionView(
                selectedService.ServiceId,
                selectedService.Name,
                selectedDate,
                selectedTime);

            veterinarianView.VeterinarianSelected += OnVeterinarianSelected;
            veterinarianView.BackToDateTimeSelection += (s, e) => ShowDateTimeSelectionView();

            MainContent.Content = veterinarianView;
        }

        private void ShowMedicalCardFormView()
        {
            if (selectedService == null || selectedDate == DateTime.MinValue || selectedVeterinarianId == 0)
            {
                ShowVeterinarianSelectionView();
                return;
            }

            MedicalCardFormView medicalCardView = new MedicalCardFormView(
                currentClientId,
                selectedService.ServiceId,
                selectedService.Name,
                selectedDate,
                selectedTime,
                selectedVeterinarianId,
                selectedVeterinarianName);

            medicalCardView.MedicalCardCompleted += OnMedicalCardFormSubmitted;
            medicalCardView.BackToVeterinarianSelection += (s, e) => ShowVeterinarianSelectionView();

            MainContent.Content = medicalCardView;
        }

        private void ShowAppointmentConfirmationView(MedicalCardData medicalCardData)
        {
            if (medicalCardData == null)
            {
                ShowMedicalCardFormView();
                return;
            }

            AppointmentConfirmationView confirmationView = new AppointmentConfirmationView(medicalCardData);
            confirmationView.AppointmentConfirmed += OnAppointmentConfirmed;
            confirmationView.BackToMedicalCardForm += (s, e) => ShowMedicalCardFormView();

            MainContent.Content = confirmationView;
        }

        private void ShowAppointmentSuccessView(AppointmentConfirmationResult result)
        {
            // Создаем временное представление для успешной записи
            Border successCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(20),
                Padding = new Thickness(30),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 1,
                    Opacity = 0.2,
                    Color = Colors.Black
                }
            };

            StackPanel contentPanel = new StackPanel();

            // Иконка успеха
            Ellipse successIcon = new Ellipse
            {
                Width = 80,
                Height = 80,
                Fill = (Brush)FindResource("AccentBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            TextBlock iconText = new TextBlock
            {
                Text = "✓",
                FontSize = 48,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid iconGrid = new Grid();
            iconGrid.Children.Add(successIcon);
            iconGrid.Children.Add(iconText);

            contentPanel.Children.Add(iconGrid);

            // Заголовок
            TextBlock titleText = new TextBlock
            {
                Text = "Запись успешно создана!",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };

            contentPanel.Children.Add(titleText);

            // Детали записи
            TextBlock detailsTitle = new TextBlock
            {
                Text = "Детали вашей записи:",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextBrush"),
                Margin = new Thickness(0, 0, 0, 15)
            };

            contentPanel.Children.Add(detailsTitle);

            // Услуга
            StackPanel servicePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock serviceLabelText = new TextBlock
            {
                Text = "Услуга:",
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Width = 120
            };

            TextBlock serviceValueText = new TextBlock
            {
                Text = result.ServiceName,
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush")
            };

            servicePanel.Children.Add(serviceLabelText);
            servicePanel.Children.Add(serviceValueText);
            contentPanel.Children.Add(servicePanel);

            // Дата и время
            StackPanel dateTimePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock dateTimeLabelText = new TextBlock
            {
                Text = "Дата и время:",
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Width = 120
            };

            TextBlock dateTimeValueText = new TextBlock
            {
                Text = $"{result.AppointmentDate:dd.MM.yyyy}, {result.AppointmentTime}",
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush")
            };

            dateTimePanel.Children.Add(dateTimeLabelText);
            dateTimePanel.Children.Add(dateTimeValueText);
            contentPanel.Children.Add(dateTimePanel);

            // Ветеринар
            StackPanel veterinarianPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock veterinarianLabelText = new TextBlock
            {
                Text = "Специалист:",
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Width = 120
            };

            TextBlock veterinarianValueText = new TextBlock
            {
                Text = result.VeterinarianName,
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush")
            };

            veterinarianPanel.Children.Add(veterinarianLabelText);
            veterinarianPanel.Children.Add(veterinarianValueText);
            contentPanel.Children.Add(veterinarianPanel);

            // Питомец
            StackPanel petPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock petLabelText = new TextBlock
            {
                Text = "Питомец:",
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Width = 120
            };

            TextBlock petValueText = new TextBlock
            {
                Text = result.PetName,
                FontSize = 15,
                Foreground = (Brush)FindResource("TextBrush")
            };

            petPanel.Children.Add(petLabelText);
            petPanel.Children.Add(petValueText);
            contentPanel.Children.Add(petPanel);

            // Информационный текст
            TextBlock infoText = new TextBlock
            {
                Text = "Все детали записи также будут отправлены на вашу электронную почту.",
                FontSize = 14,
                Foreground = (Brush)FindResource("TextBrush"),
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 20, 0, 30),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            contentPanel.Children.Add(infoText);

            // Кнопки навигации
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button viewAppointmentsButton = new Button
            {
                Content = "Мои записи",
                Style = (Style)FindResource("ModernButton"),
                Width = 150,
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0)
            };
            viewAppointmentsButton.Click += (s, e) => ShowAppointmentsView();

            Button newAppointmentButton = new Button
            {
                Content = "Новая запись",
                Style = (Style)FindResource("ModernButton"),
                Width = 150,
                Height = 40,
                Margin = new Thickness(10, 0, 0, 0)
            };
            newAppointmentButton.Click += (s, e) => ShowServicesView();

            buttonsPanel.Children.Add(viewAppointmentsButton);
            buttonsPanel.Children.Add(newAppointmentButton);
            contentPanel.Children.Add(buttonsPanel);

            successCard.Child = contentPanel;

            // Устанавливаем содержимое
            MainContent.Content = successCard;
        }

        private void ShowAppointmentsView()
        {
            try
            {
                if (MainContent == null)
                {
                    MessageBox.Show("Ошибка инициализации интерфейса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем новый view для записей
                AppointmentsView appointmentsView = new AppointmentsView(currentClientId);
                appointmentsView.NewAppointmentRequested += (s, e) => ShowServicesView();

                // Отображаем view в основном контенте
                MainContent.Content = appointmentsView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении списка записей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMedicalCardsView()
        {
            try
            {
                if (MainContent == null)
                {
                    MessageBox.Show("Ошибка инициализации интерфейса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем новый view для медицинских карт
                MedicalCardsView medicalCardsView = new MedicalCardsView(currentClientId);

                // Отображаем view в основном контенте
                MainContent.Content = medicalCardsView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении медицинских карт: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowProfileView()
        {
            try
            {
                if (MainContent == null)
                {
                    MessageBox.Show("Ошибка инициализации интерфейса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем новый view для профиля
                ProfileView profileView = new ProfileView(currentClient);
                profileView.ProfileUpdated += (sender, updatedClient) =>
                {
                    // Обновляем данные в интерфейсе после изменения профиля
                    currentClient = updatedClient;
                    UserNameText.Text = currentClient.FullName;
                    if (!string.IsNullOrEmpty(currentClient.ImageUrl))
                    {
                        try
                        {
                            ProfileImage.Source = new BitmapImage(new Uri(currentClient.ImageUrl));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                        }
                    }
                };

                // Отображаем view в основном контенте
                MainContent.Content = profileView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Обработчики событий

        private void OnServiceSelected(ServiceModel service)
        {
            selectedService = service;
            ShowDateTimeSelectionView();
        }

        private void OnDateTimeSelected(object sender, DateTime_TimeSelectedEventArgs e)
        {
            selectedDate = e.SelectedDate;
            selectedTime = e.SelectedTime;
            ShowVeterinarianSelectionView();
        }

        private void OnVeterinarianSelected(object sender, int veterinarianId)
        {
            selectedVeterinarianId = veterinarianId;

            // Загружаем имя ветеринара
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = "SELECT fio FROM VC_Sotrudniki WHERE sotrudnik_id = @VeterinarianId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@VeterinarianId", veterinarianId);

                    object result = command.ExecuteScalar();
                    selectedVeterinarianName = result != null ? result.ToString() : "Неизвестный ветеринар";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных ветеринара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ShowMedicalCardFormView();
        }

        private void OnMedicalCardFormSubmitted(object sender, MedicalCardData medicalCardData)
        {
            ShowAppointmentConfirmationView(medicalCardData);
        }

        private void OnAppointmentConfirmed(object sender, AppointmentConfirmationResult result)
        {
            ShowAppointmentSuccessView(result);
        }

        private void OnAppointmentCancelled(int appointmentId)
        {
            // Отмена записи на прием
            CancelAppointment(appointmentId);

            // Обновление списка записей
            ShowAppointmentsView();
        }

        private void OnProfileUpdated(ClientModel updatedClient)
        {
            // Обновление данных клиента
            UpdateClientData(updatedClient);

            // Обновление интерфейса
            currentClient = updatedClient;
            UserNameText.Text = currentClient.FullName;

            if (!string.IsNullOrEmpty(currentClient.ImageUrl))
            {
                try
                {
                    ProfileImage.Source = new BitmapImage(new Uri(currentClient.ImageUrl));
                }
                catch
                {
                    // Если не удалось загрузить изображение, используем стандартное
                }
            }
        }

        #endregion

        #region Методы работы с базой данных

        private bool CheckExistingMedicalCard()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT COUNT(*) FROM VC_MedKarty 
                                     WHERE zapis_id IN (SELECT zapis_id FROM VC_Zapisi WHERE klient_id = @ClientId)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", currentClientId);
                        int count = (int)command.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке наличия медкарты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CancelAppointment(int appointmentId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    // Удаляем связанные медкарты
                    string deleteMedCardQuery = "DELETE FROM VC_MedKarty WHERE zapis_id = @AppointmentId";
                    using (SqlCommand command = new SqlCommand(deleteMedCardQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AppointmentId", appointmentId);
                        command.ExecuteNonQuery();
                    }

                    // Удаляем запись на прием
                    string deleteAppointmentQuery = "DELETE FROM VC_Zapisi WHERE zapis_id = @AppointmentId AND klient_id = @ClientId";
                    using (SqlCommand command = new SqlCommand(deleteAppointmentQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AppointmentId", appointmentId);
                        command.Parameters.AddWithValue("@ClientId", currentClientId);
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Запись на прием успешно отменена!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отмене записи на прием: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateClientData(ClientModel client)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    string updateQuery = @"UPDATE VC_Klienty 
                                         SET fio = @FullName, 
                                             telefon = @Phone, 
                                             email = @Email,
                                             login = @Login,
                                             url_izobrazhenija = @ImageUrl 
                                         WHERE klient_id = @ClientId";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", client.FullName);
                        command.Parameters.AddWithValue("@Phone", client.Phone);

                        if (string.IsNullOrEmpty(client.Email))
                            command.Parameters.AddWithValue("@Email", DBNull.Value);
                        else
                            command.Parameters.AddWithValue("@Email", client.Email);

                        if (string.IsNullOrEmpty(client.Login))
                            command.Parameters.AddWithValue("@Login", DBNull.Value);
                        else
                            command.Parameters.AddWithValue("@Login", client.Login);

                        if (string.IsNullOrEmpty(client.ImageUrl))
                            command.Parameters.AddWithValue("@ImageUrl", DBNull.Value);
                        else
                            command.Parameters.AddWithValue("@ImageUrl", client.ImageUrl);

                        command.Parameters.AddWithValue("@ClientId", client.ClientId);

                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Данные профиля успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем текущие данные клиента
                    currentClient = client;

                    // Обновляем отображение имени клиента
                    UserNameText.Text = currentClient.FullName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
