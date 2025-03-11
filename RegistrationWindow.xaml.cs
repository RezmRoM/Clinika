using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        private readonly string connectionString = "data source=stud-mssql.sttec.yar.ru,38325;user id=user122_db;password=user122;MultipleActiveResultSets=True;App=EntityFramework";

        public RegistrationWindow()
        {
            InitializeComponent();
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

        // Обработчик кнопки регистрации
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string imageUrl = ImageUrlTextBox.Text.Trim();

            // Проверка обязательных полей
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля: ФИО и телефон!", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка формата телефона
            if (!IsValidPhoneNumber(phone))
            {
                MessageBox.Show("Пожалуйста, введите корректный номер телефона!\nПример: +7(XXX)XXX-XX-XX", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка паролей
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите пароль!", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов!", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка существования телефона
                    string checkPhoneQuery = "SELECT COUNT(*) FROM VC_Klienty WHERE telefon = @Phone";
                    using (SqlCommand checkCommand = new SqlCommand(checkPhoneQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Phone", phone);
                        int count = (int)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Пользователь с таким номером телефона уже зарегистрирован!", 
                                           "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Регистрация нового клиента
                    string insertQuery = @"INSERT INTO VC_Klienty 
                                        (fio, telefon, url_izobrazhenija, id_roli) 
                                        VALUES 
                                        (@FullName, @Phone, @ImageUrl, @RoleId)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", fullName);
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@RoleId", 1); // Роль "Клиент"
                        
                        // URL изображения может быть пустым
                        if (string.IsNullOrEmpty(imageUrl))
                            command.Parameters.AddWithValue("@ImageUrl", DBNull.Value);
                        else
                            command.Parameters.AddWithValue("@ImageUrl", imageUrl);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Регистрация успешно завершена!\n\nТеперь вы можете войти в систему, используя свой номер телефона.", 
                               "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход к окну авторизации
                AuthorizationWindow authWindow = new AuthorizationWindow();
                authWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при регистрации: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверка формата номера телефона
        private bool IsValidPhoneNumber(string phone)
        {
            // Проверка российского номера телефона
            string pattern = @"^\+?[78][-\(]?\d{3}\)?-?\d{3}-?\d{2}-?\d{2}$";
            return Regex.IsMatch(phone, pattern);
        }

        // Переход к окну авторизации
        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            AuthorizationWindow authWindow = new AuthorizationWindow();
            authWindow.Show();
            this.Close();
        }
    }
}
