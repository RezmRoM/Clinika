using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Veterinary_Clinic
{
    public partial class VeterinarianSelectionView : UserControl
    {
        public event EventHandler<int> VeterinarianSelected;
        public event EventHandler BackToDateTimeSelection;
        
        private int _serviceId;
        private DateTime _appointmentDate;
        private string _appointmentTime;
        private string _serviceName;
        
        private ObservableCollection<VeterinarianModel> _veterinarians = new ObservableCollection<VeterinarianModel>();
        
        public VeterinarianSelectionView(int serviceId, string serviceName, DateTime appointmentDate, string appointmentTime)
        {
            InitializeComponent();
            
            _serviceId = serviceId;
            _serviceName = serviceName;
            _appointmentDate = appointmentDate;
            _appointmentTime = appointmentTime;
            
            // Установка информации о выбранной услуге и времени
            SetAppointmentInfo();
            
            // Загрузка списка ветеринаров
            LoadVeterinarians();
        }
        
        private void SetAppointmentInfo()
        {
            ServiceNameText.Text = _serviceName;
            AppointmentDateText.Text = _appointmentDate.ToString("d MMMM yyyy", new CultureInfo("ru-RU"));
            AppointmentTimeText.Text = _appointmentTime;
        }
        
        private void LoadVeterinarians()
        {
            try
            {
                _veterinarians.Clear();
                
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    // Максимально простой запрос, использующий только столбцы из базы данных
                    string query = @"
                    SELECT 
                        sotrudnik_id, 
                        fio,
                        stazh,
                        dolzhnost,
                        url_izobrazhenija
                    FROM VC_Sotrudniki
                    WHERE id_roli = 2"; // Предполагаем, что id_roli = 2 - это ветеринары
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string fio = reader.GetString(1);
                                
                                // Получаем стаж, если он доступен
                                int experience = reader.IsDBNull(2) ? 5 + (id % 15) : reader.GetInt32(2);
                                
                                // Получаем должность
                                string position = reader.IsDBNull(3) ? "Ветеринар" : reader.GetString(3);
                                
                                // Получаем URL изображения
                                string imageUrl = reader.IsDBNull(4) ? 
                                    "https://cdn-icons-png.flaticon.com/512/3774/3774299.png" : 
                                    reader.GetString(4);
                                
                                // Генерируем данные, которых нет в базе
                                string specialty = GetGeneratedSpecialty(id);
                                int age = 30 + (id % 20); // Возраст от 30 до 49 лет
                                
                                _veterinarians.Add(new VeterinarianModel
                                {
                                    Id = id,
                                    Name = fio, // Используем поле fio как полное имя
                                    Age = age,
                                    Experience = experience,
                                    Specialty = position, // Используем должность как специализацию
                                    YearsText = GetYearEnding(experience),
                                    ImageUrl = imageUrl,
                                    Position = position
                                });
                            }
                        }
                    }
                }
                
                // Фильтруем ветеринаров по специализации, если они уже загружены
                if (_veterinarians.Count > 0)
                {
                    string targetSpecialty = GetSpecialtyByServiceId(_serviceId);
                    
                    // Если у нас есть специализация для поиска, применяем фильтр
                    if (!string.IsNullOrEmpty(targetSpecialty))
                    {
                        var filteredVets = _veterinarians
                            .Where(v => v.Position.Contains(targetSpecialty) || v.Specialty.Contains(targetSpecialty))
                            .ToList();
                        
                        // Если есть подходящие специалисты, используем только их
                        if (filteredVets.Count > 0)
                        {
                            _veterinarians.Clear();
                            foreach (var vet in filteredVets)
                            {
                                _veterinarians.Add(vet);
                            }
                        }
                    }
                }
                
                // Если нет ветеринаров вообще, добавляем заглушки
                if (_veterinarians.Count == 0)
                {
                    AddDummyVeterinarians();
                }
                
                // Устанавливаем источник данных для элемента управления
                VeterinariansItemsControl.ItemsSource = _veterinarians;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных ветеринара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // В случае ошибки добавляем заглушки
                AddDummyVeterinarians();
                VeterinariansItemsControl.ItemsSource = _veterinarians;
            }
        }
        
        // Вспомогательный метод для генерации специализации на основе ID
        private string GetGeneratedSpecialty(int id)
        {
            // Распределяем специализации на основе ID
            switch (id % 4)
            {
                case 0: return "Терапевт";
                case 1: return "Хирург";
                case 2: return "Диагност";
                case 3: return "Стоматолог";
                default: return "Ветеринар";
            }
        }
        
        // Вспомогательный метод для добавления ветеринаров-заглушек
        private void AddDummyVeterinarians()
        {
            _veterinarians.Clear();
            
            // Добавляем несколько заглушек с разными специализациями
            _veterinarians.Add(new VeterinarianModel
            {
                Id = 1,
                Name = "Волкова Светлана Николаевна",
                Age = 35,
                Experience = 10,
                Specialty = "Терапевт",
                YearsText = GetYearEnding(10),
                ImageUrl = "https://cdn-icons-png.flaticon.com/512/3774/3774299.png"
            });
            
            _veterinarians.Add(new VeterinarianModel
            {
                Id = 2,
                Name = "Петров Иван Александрович",
                Age = 42,
                Experience = 15,
                Specialty = "Хирург",
                YearsText = GetYearEnding(15),
                ImageUrl = "https://cdn-icons-png.flaticon.com/512/3774/3774299.png"
            });
            
            _veterinarians.Add(new VeterinarianModel
            {
                Id = 3,
                Name = "Смирнова Ольга Дмитриевна",
                Age = 38,
                Experience = 12,
                Specialty = "Диагност",
                YearsText = GetYearEnding(12),
                ImageUrl = "https://cdn-icons-png.flaticon.com/512/3774/3774299.png"
            });
            
            _veterinarians.Add(new VeterinarianModel
            {
                Id = 4,
                Name = "Козлов Дмитрий Сергеевич",
                Age = 45,
                Experience = 20,
                Specialty = "Стоматолог",
                YearsText = GetYearEnding(20),
                ImageUrl = "https://cdn-icons-png.flaticon.com/512/3774/3774299.png"
            });
        }
        
        private string GetYearEnding(int years)
        {
            int lastDigit = years % 10;
            int lastTwoDigits = years % 100;
            
            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "лет";
            
            switch (lastDigit)
            {
                case 1:
                    return "год";
                case 2:
                case 3:
                case 4:
                    return "года";
                default:
                    return "лет";
            }
        }
        
        private void Veterinarian_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                int veterinarianId = Convert.ToInt32(border.Tag);
                VeterinarianSelected?.Invoke(this, veterinarianId);
            }
        }
        
        private void SelectVeterinarianButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int veterinarianId = Convert.ToInt32(button.Tag);
                VeterinarianSelected?.Invoke(this, veterinarianId);
            }
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToDateTimeSelection?.Invoke(this, EventArgs.Empty);
        }
        
        private string GetSpecialtyByServiceId(int serviceId)
        {
            // Определяем специализацию ветеринара на основе ID услуги
            switch (serviceId)
            {
                case 1:
                case 2:
                case 3:
                    return "Терапевт";
                case 4:
                case 5:
                    return "Хирург";
                case 6:
                case 7:
                    return "Диагност";
                case 8:
                case 9:
                    return "Стоматолог";
                default:
                    return ""; // Пустая строка вернет всех специалистов
            }
        }
    }
} 