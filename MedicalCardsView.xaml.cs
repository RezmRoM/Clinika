using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Veterinary_Clinic
{
    public partial class MedicalCardsView : UserControl
    {
        private int _clientId;
        private ObservableCollection<PetModel> _pets;
        private ObservableCollection<MedicalRecordModel> _medicalRecords;

        public MedicalCardsView(int clientId)
        {
            InitializeComponent();
            _clientId = clientId;
            _pets = new ObservableCollection<PetModel>();
            _medicalRecords = new ObservableCollection<MedicalRecordModel>();

            LoadPets();
        }

        private void LoadPets()
        {
            _pets.Clear();
            
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT 
                            z.zhivotnoe_id, 
                            z.klient_id, 
                            z.tip_id,
                            t.nazvanie AS tip_nazvanie,
                            z.imia, 
                            z.poroda, 
                            z.pol, 
                            z.data_rozhdenia, 
                            z.primechanie,
                            (SELECT COUNT(*) FROM VC_MedKarty WHERE zhivotnoe_id = z.zhivotnoe_id) AS kolichestvo_zapisei
                        FROM 
                            VC_Zhivotnye z
                        JOIN 
                            VC_Tipy_Zhivotnyh t ON z.tip_id = t.tip_id
                        WHERE 
                            z.klient_id = @ClientId";
                            
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientId", _clientId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime? birthDate = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("data_rozhdenia")))
                            {
                                birthDate = reader.GetDateTime(reader.GetOrdinal("data_rozhdenia"));
                            }
                            
                            string formattedAge = null;
                            if (birthDate.HasValue)
                            {
                                int years = DateTime.Now.Year - birthDate.Value.Year;
                                if (DateTime.Now.DayOfYear < birthDate.Value.DayOfYear)
                                    years--;
                                
                                if (years == 0)
                                {
                                    int months = DateTime.Now.Month - birthDate.Value.Month;
                                    if (DateTime.Now.Day < birthDate.Value.Day)
                                        months--;
                                    
                                    if (months <= 0)
                                        months = 0;
                                    
                                    formattedAge = $"{months} мес.";
                                }
                                else
                                {
                                    formattedAge = $"{years} {GetYearDeclension(years)}";
                                }
                            }
                            
                            _pets.Add(new PetModel
                            {
                                PetId = reader.GetInt32(reader.GetOrdinal("zhivotnoe_id")),
                                ClientId = reader.GetInt32(reader.GetOrdinal("klient_id")),
                                TypeId = reader.GetInt32(reader.GetOrdinal("tip_id")),
                                TypeName = reader.GetString(reader.GetOrdinal("tip_nazvanie")),
                                Name = reader.GetString(reader.GetOrdinal("imia")),
                                Breed = !reader.IsDBNull(reader.GetOrdinal("poroda")) ? reader.GetString(reader.GetOrdinal("poroda")) : null,
                                Gender = reader.GetString(reader.GetOrdinal("pol")),
                                BirthDate = birthDate,
                                FormattedAge = formattedAge,
                                Notes = !reader.IsDBNull(reader.GetOrdinal("primechanie")) ? reader.GetString(reader.GetOrdinal("primechanie")) : null,
                                MedicalRecordsCount = reader.GetInt32(reader.GetOrdinal("kolichestvo_zapisei"))
                            });
                        }
                    }
                }
                
                PetsItemsControl.ItemsSource = _pets;
                
                // Настройка видимости элементов в карточках питомцев
                foreach (var pet in _pets)
                {
                    var container = PetsItemsControl.ItemContainerGenerator.ContainerFromItem(pet) as ContentPresenter;
                    if (container != null)
                    {
                        // Найдем BreedGrid и AgeGrid
                        var breedGrid = FindChildByName<Grid>(container, "BreedGrid");
                        var ageGrid = FindChildByName<Grid>(container, "AgeGrid");
                        
                        // Установим видимость в зависимости от наличия данных
                        if (breedGrid != null)
                            breedGrid.Visibility = string.IsNullOrEmpty(pet.Breed) ? Visibility.Collapsed : Visibility.Visible;
                        
                        if (ageGrid != null)
                            ageGrid.Visibility = string.IsNullOrEmpty(pet.FormattedAge) ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
                
                // Показывать сообщение, если нет питомцев
                NoPetsText.Visibility = _pets.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка питомцев: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string GetYearDeclension(int years)
        {
            if (years % 10 == 1 && years % 100 != 11)
                return "год";
            else if ((years % 10 == 2 || years % 10 == 3 || years % 10 == 4) && 
                    (years % 100 != 12 && years % 100 != 13 && years % 100 != 14))
                return "года";
            else
                return "лет";
        }
        
        private void PetCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                int petId = Convert.ToInt32(border.Tag);
                LoadMedicalRecords(petId);
            }
        }
        
        private void LoadMedicalRecords(int petId)
        {
            try
            {
                PetModel selectedPet = _pets.FirstOrDefault(p => p.PetId == petId);
                if (selectedPet == null) return;
                
                // Обновляем информацию о выбранном питомце
                SelectedPetName.Text = selectedPet.Name;
                SelectedPetInitial.Text = selectedPet.Name.Substring(0, 1).ToUpper();
                
                string details = selectedPet.TypeName;
                if (!string.IsNullOrEmpty(selectedPet.Breed))
                    details += " • " + selectedPet.Breed;
                if (!string.IsNullOrEmpty(selectedPet.FormattedAge))
                    details += " • " + selectedPet.FormattedAge;
                
                SelectedPetDetails.Text = details;
                
                // Загружаем медицинские записи
                _medicalRecords.Clear();
                
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT 
                            m.medkarta_id,
                            m.zhivotnoe_id,
                            m.zapis_id,
                            m.id_sotrudnika,
                            m.data_priema,
                            m.zhaloby,
                            m.diagnoz,
                            m.rekomendacii,
                            s.fio AS vrach_name,
                            u.nazvanie AS usluga_name
                        FROM 
                            VC_MedKarty m
                        JOIN 
                            VC_Sotrudniki s ON m.sotrudnika_id = s.id_sotrudnik
                        LEFT JOIN 
                            VC_Zapisi z ON m.zapis_id = z.zapis_id
                        LEFT JOIN 
                            VC_Uslugi u ON z.uslugi_id = u.uslugi_id
                        WHERE 
                            m.zhivotnoe_id = @PetId
                        ORDER BY 
                            m.data_priema DESC";
                            
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PetId", petId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _medicalRecords.Add(new MedicalRecordModel
                            {
                                MedicalRecordId = reader.GetInt32(reader.GetOrdinal("medkarta_id")),
                                PetId = reader.GetInt32(reader.GetOrdinal("zhivotnoe_id")),
                                AppointmentId = !reader.IsDBNull(reader.GetOrdinal("zapis_id")) ? reader.GetInt32(reader.GetOrdinal("zapis_id")) : (int?)null,
                                VeterinarianId = reader.GetInt32(reader.GetOrdinal("sotrudnika_id")),
                                VeterinarianName = reader.GetString(reader.GetOrdinal("vrach_name")),
                                VisitDate = reader.GetDateTime(reader.GetOrdinal("data_priema")),
                                ServiceName = !reader.IsDBNull(reader.GetOrdinal("usluga_name")) ? reader.GetString(reader.GetOrdinal("usluga_name")) : "Консультация",
                                Complaints = !reader.IsDBNull(reader.GetOrdinal("zhaloby")) ? reader.GetString(reader.GetOrdinal("zhaloby")) : null,
                                Diagnosis = !reader.IsDBNull(reader.GetOrdinal("diagnoz")) ? reader.GetString(reader.GetOrdinal("diagnoz")) : null,
                                Recommendations = !reader.IsDBNull(reader.GetOrdinal("rekomendacii")) ? reader.GetString(reader.GetOrdinal("rekomendacii")) : null
                            });
                        }
                    }
                }
                
                MedicalRecordsItemsControl.ItemsSource = _medicalRecords;
                
                // Показываем содержимое и скрываем сообщение о загрузке
                LoadingText.Visibility = Visibility.Collapsed;
                MedicalCardContent.Visibility = Visibility.Visible;
                
                // Настройка видимости панелей диагноза и рекомендаций
                MedicalRecordsItemsControl.ItemsSource = null;
                MedicalRecordsItemsControl.ItemsSource = _medicalRecords;
                
                // Показываем сообщение, если нет записей
                NoRecordsText.Visibility = _medicalRecords.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке медицинской карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Вспомогательный метод для поиска элемента по имени
        private T FindChildByName<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                {
                    return child as T;
                }
                
                var result = FindChildByName<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
    }
} 