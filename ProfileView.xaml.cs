using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Veterinary_Clinic
{
    public partial class ProfileView : UserControl
    {
        private ClientModel _client;
        private int _appointmentsCount = 0;
        private int _medCardsCount = 0;

        public event EventHandler<ClientModel> ProfileUpdated;

        public ProfileView(ClientModel client)
        {
            InitializeComponent();
            _client = client;
            LoadProfileData();
            LoadStatistics();
        }

        #region Загрузка данных

        private void LoadProfileData()
        {
            if (_client == null) return;

            // Заполняем текстовые поля в режиме просмотра
            NameValueText.Text = _client.FullName;
            PhoneValueText.Text = _client.Phone;
            EmailValueText.Text = string.IsNullOrEmpty(_client.Email) ? "Не указан" : _client.Email;
            LoginValueText.Text = _client.Login;

            // Заполняем поля редактирования
            NameEditBox.Text = _client.FullName;
            PhoneEditBox.Text = _client.Phone;
            EmailEditBox.Text = _client.Email;

            // Устанавливаем изображение профиля
            if (!string.IsNullOrEmpty(_client.ImageUrl))
            {
                try
                {
                    ProfilePicture.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_client.ImageUrl));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    // Используем стандартное изображение при ошибке
                }
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    // Получаем количество записей
                    string appointmentsQuery = @"
                    SELECT COUNT(*) FROM VC_Priemy
                    WHERE id_klienta = @ClientId";

                    using (SqlCommand command = new SqlCommand(appointmentsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);
                        _appointmentsCount = (int)command.ExecuteScalar();
                        AppointmentsCountText.Text = _appointmentsCount.ToString();
                    }

                    // Получаем количество медкарт
                    string medCardsQuery = @"
                    SELECT COUNT(DISTINCT zhivotnoe_id) FROM VC_MedKarty mk
                    JOIN VC_Priemy p ON mk.id_priema = p.priem_id
                    WHERE p.id_klienta = @ClientId";

                    using (SqlCommand command = new SqlCommand(medCardsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);
                        _medCardsCount = (int)command.ExecuteScalar();
                        MedcardsCountText.Text = _medCardsCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region События элементов управления

        private void ProfilePicture_MouseEnter(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            ImageOverlay.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void ProfilePicture_MouseLeave(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            ImageOverlay.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void ChangeImage_Click(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите изображение профиля",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string newImagePath = openFileDialog.FileName;

                    // Здесь должна быть логика загрузки на сервер
                    // В простейшем случае, мы просто обновляем путь в базе данных

                    // Обновляем изображение профиля
                    ProfilePicture.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(newImagePath));

                    // Обновляем модель
                    _client.ImageUrl = newImagePath;

                    // Сохраняем в базу данных
                    UpdateImageUrl(newImagePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при выборе изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewModePanel.Visibility = Visibility.Collapsed;
            EditModePanel.Visibility = Visibility.Visible;
            PasswordChangePanel.Visibility = Visibility.Collapsed;
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ViewModePanel.Visibility = Visibility.Collapsed;
            EditModePanel.Visibility = Visibility.Collapsed;
            PasswordChangePanel.Visibility = Visibility.Visible;

            // Очищаем поля пароля
            CurrentPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ViewModePanel.Visibility = Visibility.Visible;
            EditModePanel.Visibility = Visibility.Collapsed;

            // Возвращаем исходные значения
            NameEditBox.Text = _client.FullName;
            PhoneEditBox.Text = _client.Phone;
            EmailEditBox.Text = _client.Email;
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(NameEditBox.Text))
            {
                MessageBox.Show("Пожалуйста, укажите ФИО.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneEditBox.Text))
            {
                MessageBox.Show("Пожалуйста, укажите телефон.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Обновляем модель
            _client.FullName = NameEditBox.Text.Trim();
            _client.Phone = PhoneEditBox.Text.Trim();
            _client.Email = EmailEditBox.Text.Trim();

            // Сохраняем в базу данных
            if (UpdateClientProfile())
            {
                // Обновляем отображение
                LoadProfileData();
                ViewModePanel.Visibility = Visibility.Visible;
                EditModePanel.Visibility = Visibility.Collapsed;

                // Оповещаем родительский компонент
                ProfileUpdated?.Invoke(this, _client);
            }
        }

        private void CancelPasswordChange_Click(object sender, RoutedEventArgs e)
        {
            ViewModePanel.Visibility = Visibility.Visible;
            PasswordChangePanel.Visibility = Visibility.Collapsed;
        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
            {
                MessageBox.Show("Введите текущий пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                MessageBox.Show("Введите новый пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем текущий пароль
            if (VerifyCurrentPassword(CurrentPasswordBox.Password))
            {
                // Обновляем пароль
                if (UpdatePassword(NewPasswordBox.Password))
                {
                    MessageBox.Show("Пароль успешно изменен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    ViewModePanel.Visibility = Visibility.Visible;
                    PasswordChangePanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                MessageBox.Show("Текущий пароль неверен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Методы работы с базой данных

        private bool UpdateClientProfile()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"
                    UPDATE VC_Klienty 
                    SET fio = @FullName, 
                        telefon = @Phone, 
                        email = @Email
                    WHERE klient_id = @ClientId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", _client.FullName);
                        command.Parameters.AddWithValue("@Phone", _client.Phone);
                        command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(_client.Email) ? DBNull.Value : (object)_client.Email);
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateImageUrl(string imageUrl)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"
                    UPDATE VC_Klienty 
                    SET url_izobrazhenija = @ImageUrl
                    WHERE klient_id = @ClientId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool VerifyCurrentPassword(string password)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"
                    SELECT COUNT(*) 
                    FROM VC_Klienty 
                    WHERE klient_id = @ClientId AND parol = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);
                        command.Parameters.AddWithValue("@Password", password);

                        int count = (int)command.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool UpdatePassword(string newPassword)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"
                    UPDATE VC_Klienty 
                    SET parol = @Password
                    WHERE klient_id = @ClientId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Password", newPassword);
                        command.Parameters.AddWithValue("@ClientId", _client.ClientId);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion
    }
}