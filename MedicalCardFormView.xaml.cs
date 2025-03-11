using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Veterinary_Clinic
{
    public partial class MedicalCardFormView : UserControl
    {
        private int _appointmentId;
        private int _vetId;
        private int _petId;
        private ServiceModel _service;
        private DateTime _appointmentDateTime;
        private PetModel _pet;
        
        public event EventHandler FormCompleted;
        public event EventHandler FormCancelled;
        public event EventHandler BackToVeterinarianSelection;
        public event EventHandler<MedicalCardData> MedicalCardCompleted;

        public MedicalCardFormView(int appointmentId, int vetId, ServiceModel service, DateTime appointmentDateTime, PetModel pet)
        {
            InitializeComponent();
            
            _appointmentId = appointmentId;
            _vetId = vetId;
            _petId = pet.PetId;
            _service = service;
            _appointmentDateTime = appointmentDateTime;
            _pet = pet;
            
            LoadData();
        }
        
        // Конструктор для обратной совместимости
        public MedicalCardFormView(int clientId, int serviceId, string serviceName, DateTime appointmentDate, 
                                  string appointmentTime, int veterinarianId, string veterinarianName)
        {
            InitializeComponent();
            
            // Создаем временные объекты для совместимости
            _service = new ServiceModel { ServiceId = serviceId, Name = serviceName };
            _appointmentDateTime = appointmentDate;
            _vetId = veterinarianId;
            
            // Остальные поля будут заполнены позже
        }

        private void LoadData()
        {
            // Заполняем информацию о приеме
            ServiceNameText.Text = _service.Name;
            AppointmentDateTimeText.Text = $"{_appointmentDateTime.ToString("dd.MM.yyyy")} {_appointmentDateTime.ToString("HH:mm")}";
            
            // Заполняем информацию о животном
            PetNameText.Text = _pet.Name;
            PetTypeText.Text = _pet.TypeName;
            PetBreedText.Text = !string.IsNullOrEmpty(_pet.Breed) ? _pet.Breed : "Не указана";
            PetAgeText.Text = !string.IsNullOrEmpty(_pet.FormattedAge) ? _pet.FormattedAge : "Не указан";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем обязательные поля
            if (string.IsNullOrWhiteSpace(ComplaintsTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните поле 'Жалобы'", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                ComplaintsTextBox.Focus();
                return;
            }
            
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    // Создаем запись в медицинской карте
                    string query = @"
                        INSERT INTO VC_MedKarty 
                        (zhivotnoe_id, zapis_id, id_sotrudnik, data_priema, zhaloby, diagnoz, rekomendacii) 
                        VALUES 
                        (@PetId, @AppointmentId, @VetId, @AppointmentDate, @Complaints, @Diagnosis, @Recommendations);
                        
                        SELECT SCOPE_IDENTITY();";
                        
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PetId", _petId);
                    command.Parameters.AddWithValue("@AppointmentId", _appointmentId);
                    command.Parameters.AddWithValue("@VetId", _vetId);
                    command.Parameters.AddWithValue("@AppointmentDate", _appointmentDateTime);
                    command.Parameters.AddWithValue("@Complaints", ComplaintsTextBox.Text);
                    
                    if (!string.IsNullOrWhiteSpace(DiagnosisTextBox.Text))
                        command.Parameters.AddWithValue("@Diagnosis", DiagnosisTextBox.Text);
                    else
                        command.Parameters.AddWithValue("@Diagnosis", DBNull.Value);
                        
                    if (!string.IsNullOrWhiteSpace(RecommendationsTextBox.Text))
                        command.Parameters.AddWithValue("@Recommendations", RecommendationsTextBox.Text);
                    else
                        command.Parameters.AddWithValue("@Recommendations", DBNull.Value);
                    
                    int medicalCardId = Convert.ToInt32(command.ExecuteScalar());
                    
                    // Обновляем статус записи на "Проведена"
                    string updateQuery = "UPDATE VC_Zapisi SET status = 'Проведена' WHERE zapis_id = @AppointmentId";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@AppointmentId", _appointmentId);
                    updateCommand.ExecuteNonQuery();
                    
                    MessageBox.Show("Медицинская карта успешно заполнена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Создаем объект данных для обратной совместимости
                    MedicalCardData cardData = new MedicalCardData
                    {
                        PetId = _petId,
                        PetName = _pet.Name,
                        ServiceId = _service.ServiceId,
                        ServiceName = _service.Name,
                        VeterinarianId = _vetId,
                        AppointmentDate = _appointmentDateTime,
                        Complaints = ComplaintsTextBox.Text,
                        AdditionalInfo = RecommendationsTextBox.Text
                    };
                    
                    // Вызываем оба события для обратной совместимости
                    MedicalCardCompleted?.Invoke(this, cardData);
                    FormCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении медицинской карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Спрашиваем пользователя о подтверждении отмены
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите отменить заполнение медицинской карты? Все введенные данные будут потеряны.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // Вызываем оба события для обратной совместимости
                BackToVeterinarianSelection?.Invoke(this, EventArgs.Empty);
                FormCancelled?.Invoke(this, EventArgs.Empty);
            }
        }
    }
} 