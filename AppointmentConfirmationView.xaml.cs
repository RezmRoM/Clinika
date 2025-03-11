using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Veterinary_Clinic
{
    public partial class AppointmentConfirmationView : UserControl
    {
        public event EventHandler<AppointmentConfirmationResult> AppointmentConfirmed;
        public event EventHandler BackToMedicalCardForm;

        private MedicalCardData _medicalCardData;
        private decimal _servicePrice;

        public AppointmentConfirmationView(MedicalCardData medicalCardData)
        {
            InitializeComponent();

            _medicalCardData = medicalCardData;

            LoadServicePrice();
            DisplayAppointmentDetails();
        }

        private void LoadServicePrice()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    string query = "SELECT tsena FROM VC_Uslugi WHERE uslugi_id = @ServiceId";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _medicalCardData.ServiceId);

                    object result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        _servicePrice = Convert.ToDecimal(result);
                    }
                    else
                    {
                        _servicePrice = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке цены услуги: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayAppointmentDetails()
        {
            // Устанавливаем информацию об услуге
            ServiceNameText.Text = _medicalCardData.ServiceName;
            ServiceDescriptionText.Text = _medicalCardData.ServiceName;
            ServicePriceText.Text = $"{_servicePrice:N0} ₽";

            // Устанавливаем информацию о дате и времени
            string formattedDate = _medicalCardData.AppointmentDate.ToString("d MMMM yyyy", new CultureInfo("ru-RU"));
            DateTimeText.Text = $"{formattedDate}, {_medicalCardData.AppointmentTime}";

            // Устанавливаем информацию о ветеринаре
            VeterinarianText.Text = _medicalCardData.VeterinarianName;

            // Устанавливаем информацию о питомце
            PetNameText.Text = _medicalCardData.PetName;

            // Устанавливаем информацию о жалобах
            ComplaintsText.Text = _medicalCardData.Complaints;

            // Устанавливаем дополнительную информацию, если она есть
            if (!string.IsNullOrEmpty(_medicalCardData.AdditionalInfo))
            {
                AdditionalInfoLabel.Visibility = Visibility.Visible;
                AdditionalInfoBorder.Visibility = Visibility.Visible;
                AdditionalInfoText.Text = _medicalCardData.AdditionalInfo;
            }
            else
            {
                AdditionalInfoLabel.Visibility = Visibility.Collapsed;
                AdditionalInfoBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToMedicalCardForm?.Invoke(this, EventArgs.Empty);
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmRulesCheckBox.IsChecked != true)
            {
                MessageBox.Show("Пожалуйста, подтвердите согласие с правилами клиники.",
                               "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int appointmentId = SaveAppointment();
                int medicalCardId = SaveMedicalCard(appointmentId);

                AppointmentConfirmationResult result = new AppointmentConfirmationResult
                {
                    AppointmentId = appointmentId,
                    MedicalCardId = medicalCardId,
                    ServiceName = _medicalCardData.ServiceName,
                    AppointmentDate = _medicalCardData.AppointmentDate,
                    AppointmentTime = _medicalCardData.AppointmentTime,
                    VeterinarianName = _medicalCardData.VeterinarianName,
                    PetName = _medicalCardData.PetName
                };

                AppointmentConfirmed?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении записи: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int SaveAppointment()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    // Проверяем структуру таблицы VC_Priemy
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

                    // Начинаем транзакцию
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Проверяем доступность времени
                            string checkQuery = $@"
                            SELECT COUNT(*) 
                            FROM VC_Priemy 
                            WHERE data_priema = @AppointmentDate 
                            AND {timeFieldName} = @AppointmentTime 
                            AND id_sotrudnika = @VeterinarianId";

                            SqlCommand checkCommand = new SqlCommand(checkQuery, connection, transaction);
                            checkCommand.Parameters.AddWithValue("@AppointmentDate", _medicalCardData.AppointmentDate);
                            checkCommand.Parameters.AddWithValue("@AppointmentTime", _medicalCardData.AppointmentTime);
                            checkCommand.Parameters.AddWithValue("@VeterinarianId", _medicalCardData.VeterinarianId);

                            int existingAppointments = Convert.ToInt32(checkCommand.ExecuteScalar());

                            if (existingAppointments > 0)
                            {
                                throw new Exception("Выбранное время уже занято. Пожалуйста, выберите другое время.");
                            }

                            // Добавляем запись
                            string insertQuery = $@"
                            INSERT INTO VC_Priemy (id_klienta, id_sotrudnika, id_uslugi, data_priema, {timeFieldName}, data_zapisi)
                            VALUES (@ClientId, @VeterinarianId, @ServiceId, @AppointmentDate, @AppointmentTime, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                            SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@ClientId", _medicalCardData.ClientId);
                            insertCommand.Parameters.AddWithValue("@VeterinarianId", _medicalCardData.VeterinarianId);
                            insertCommand.Parameters.AddWithValue("@ServiceId", _medicalCardData.ServiceId);
                            insertCommand.Parameters.AddWithValue("@AppointmentDate", _medicalCardData.AppointmentDate);
                            insertCommand.Parameters.AddWithValue("@AppointmentTime", _medicalCardData.AppointmentTime);

                            // Получаем ID созданной записи
                            int appointmentId = Convert.ToInt32(insertCommand.ExecuteScalar());

                            // Сохраняем медицинскую карту
                            int medicalCardId = SaveMedicalCard(appointmentId);

                            // Подтверждаем транзакцию
                            transaction.Commit();

                            return appointmentId;
                        }
                        catch (Exception)
                        {
                            // В случае ошибки откатываем транзакцию
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        private int SaveMedicalCard(int appointmentId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();

                    // Максимально упрощенный подход: вставляем только одно поле
                    string insertQuery = @"
                    INSERT INTO VC_MedKarty (klichka_zhivotnogo) 
                    VALUES (@PetName);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@PetName", _medicalCardData.PetName);

                    // Явно приводим результат к типу int
                    int newId = (int)command.ExecuteScalar();

                    if (newId > 0)
                    {
                        // Если запись создана успешно, обновляем некоторые поля
                        try
                        {
                            string updateQuery = @"
                            UPDATE VC_MedKarty 
                            SET diagnoz = 'Первичный осмотр'
                            WHERE zapis_id = @Id";

                            SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                            updateCommand.Parameters.AddWithValue("@Id", newId);
                            updateCommand.ExecuteNonQuery();
                        }
                        catch { }

                        // Возвращаем ID успешно созданной записи
                        return newId;
                    }

                    // Не удалось получить ID
                    return -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении медкарты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }
    }

    public class AppointmentConfirmationResult
    {
        public int AppointmentId { get; set; }
        public int MedicalCardId { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string VeterinarianName { get; set; }
        public string PetName { get; set; }
    }
}