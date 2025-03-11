using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Veterinary_Clinic
{
    public partial class PetSelectionView : UserControl
    {
        private int _clientId;
        private int _serviceId;
        private string _serviceName;
        private DateTime _appointmentDate;
        private string _appointmentTime;
        private int _veterinarianId;
        private string _veterinarianName;
        
        private Dictionary<int, string> _animalTypes = new Dictionary<int, string>();
        private List<PetModel> _pets = new List<PetModel>();
        private PetModel _selectedPet;
        
        public event EventHandler BackToDateTimeSelection;
        public event EventHandler<PetSelectionEventArgs> PetSelected;
        
        public PetSelectionView(int clientId, int serviceId, string serviceName, DateTime appointmentDate, 
                                string appointmentTime, int veterinarianId, string veterinarianName)
        {
            InitializeComponent();
            
            _clientId = clientId;
            _serviceId = serviceId;
            _serviceName = serviceName;
            _appointmentDate = appointmentDate;
            _appointmentTime = appointmentTime;
            _veterinarianId = veterinarianId;
            _veterinarianName = veterinarianName;
            
            LoadAnimalTypes();
            LoadClientPets();
        }
        
        private void LoadAnimalTypes()
        {
            try
            {
                _animalTypes.Clear();
                AnimalTypeComboBox.Items.Clear();
                
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    string query = "SELECT tip_id, nazvanie FROM VC_Tipy_Zhivotnyh";
                    SqlCommand command = new SqlCommand(query, connection);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int typeId = reader.GetInt32(reader.GetOrdinal("tip_id"));
                            string typeName = reader.GetString(reader.GetOrdinal("nazvanie"));
                            
                            _animalTypes.Add(typeId, typeName);
                            
                            ComboBoxItem item = new ComboBoxItem
                            {
                                Content = typeName,
                                Tag = typeId
                            };
                            AnimalTypeComboBox.Items.Add(item);
                        }
                    }
                }
                
                if (AnimalTypeComboBox.Items.Count > 0)
                    AnimalTypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов животных: {ex.Message}", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadClientPets()
        {
            try
            {
                _pets.Clear();
                PetsWrapPanel.Children.Clear();
                
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
                        z.primechanie
                    FROM 
                        VC_Zhivotnye z
                    JOIN 
                        VC_Tipy_Zhivotnyh t ON z.tip_id = t.tip_id
                    WHERE 
                        z.klient_id = @ClientId
                    ORDER BY 
                        z.imia";
                    
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
                            
                            PetModel pet = new PetModel
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
                                Notes = !reader.IsDBNull(reader.GetOrdinal("primechanie")) ? reader.GetString(reader.GetOrdinal("primechanie")) : null
                            };
                            
                            _pets.Add(pet);
                            
                            // Создаем карточку для животного
                            CreatePetCard(pet);
                        }
                    }
                }
                
                // Показываем сообщение, если нет питомцев
                NoPetsText.Visibility = _pets.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка питомцев: {ex.Message}", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        
        private void CreatePetCard(PetModel pet)
        {
            Border petCard = new Border
            {
                Style = (Style)FindResource("PetCardStyle"),
                Tag = pet.PetId
            };
            
            StackPanel content = new StackPanel
            {
                Margin = new Thickness(15)
            };
            
            // Иконка типа животного
            Border petIconBorder = new Border
            {
                Style = (Style)FindResource("PetIconStyle")
            };
            
            TextBlock petIconText = new TextBlock
            {
                Text = pet.TypeName[0].ToString().ToUpper(),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            petIconBorder.Child = petIconText;
            content.Children.Add(petIconBorder);
            
            // Имя питомца
            TextBlock nameText = new TextBlock
            {
                Text = pet.Name,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextBrush"),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            content.Children.Add(nameText);
            
            // Тип
            Grid typeGrid = CreateInfoGrid("Тип:", pet.TypeName);
            content.Children.Add(typeGrid);
            
            // Порода (если есть)
            if (!string.IsNullOrEmpty(pet.Breed))
            {
                Grid breedGrid = CreateInfoGrid("Порода:", pet.Breed);
                content.Children.Add(breedGrid);
            }
            
            // Пол
            Grid genderGrid = CreateInfoGrid("Пол:", pet.Gender);
            content.Children.Add(genderGrid);
            
            // Возраст (если известен)
            if (!string.IsNullOrEmpty(pet.FormattedAge))
            {
                Grid ageGrid = CreateInfoGrid("Возраст:", pet.FormattedAge);
                content.Children.Add(ageGrid);
            }
            
            petCard.Child = content;
            petCard.MouseLeftButtonDown += (sender, e) => PetCard_MouseLeftButtonDown(sender, e);
            
            PetsWrapPanel.Children.Add(petCard);
        }
        
        private Grid CreateInfoGrid(string label, string value)
        {
            Grid grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontSize = 14,
                Foreground = (Brush)FindResource("TextBrush"),
                Opacity = 0.7
            };
            
            TextBlock valueText = new TextBlock
            {
                Text = value,
                FontSize = 14,
                Foreground = (Brush)FindResource("TextBrush")
            };
            
            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(valueText, 1);
            
            grid.Children.Add(labelText);
            grid.Children.Add(valueText);
            
            return grid;
        }
        
        private void PetCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                int petId = Convert.ToInt32(border.Tag);
                SelectPet(petId);
            }
        }
        
        private void SelectPet(int petId)
        {
            _selectedPet = _pets.Find(p => p.PetId == petId);
            
            // Визуально выделяем выбранную карточку и снимаем выделение с остальных
            foreach (UIElement element in PetsWrapPanel.Children)
            {
                if (element is Border border)
                {
                    if (Convert.ToInt32(border.Tag) == petId)
                    {
                        border.BorderBrush = (Brush)FindResource("PrimaryBrush");
                        border.BorderThickness = new Thickness(2);
                    }
                    else
                    {
                        border.BorderBrush = Brushes.Transparent;
                        border.BorderThickness = new Thickness(0);
                    }
                }
            }
            
            // Активируем кнопку "Продолжить"
            ContinueButton.IsEnabled = true;
        }
        
        private void AddNewPetButton_Click(object sender, RoutedEventArgs e)
        {
            PetSelectionGrid.Visibility = Visibility.Collapsed;
            AddPetGrid.Visibility = Visibility.Visible;
            
            // Очищаем поля формы
            PetNameTextBox.Text = string.Empty;
            BreedTextBox.Text = string.Empty;
            ColorTextBox.Text = string.Empty;
            NotesTextBox.Text = string.Empty;
            BirthDatePicker.SelectedDate = null;
            
            if (GenderComboBox.Items.Count > 0)
                GenderComboBox.SelectedIndex = 0;
        }
        
        private void CancelAddPetButton_Click(object sender, RoutedEventArgs e)
        {
            PetSelectionGrid.Visibility = Visibility.Visible;
            AddPetGrid.Visibility = Visibility.Collapsed;
        }
        
        private void SavePetButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем обязательные поля
            if (string.IsNullOrWhiteSpace(PetNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите кличку питомца", 
                               "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                PetNameTextBox.Focus();
                return;
            }
            
            if (AnimalTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип животного", 
                               "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                AnimalTypeComboBox.Focus();
                return;
            }
            
            if (GenderComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите пол питомца", 
                               "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenderComboBox.Focus();
                return;
            }
            
            try
            {
                // Получаем выбранный тип животного
                int typeId = Convert.ToInt32(((ComboBoxItem)AnimalTypeComboBox.SelectedItem).Tag);
                
                // Получаем выбранный пол
                string gender = ((ComboBoxItem)GenderComboBox.SelectedItem).Content.ToString();
                
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    
                    // Добавляем нового питомца в базу данных
                    string query = @"
                    INSERT INTO VC_Zhivotnye (klient_id, tip_id, imia, poroda, pol, data_rozhdenia, primechanie)
                    VALUES (@ClientId, @TypeId, @Name, @Breed, @Gender, @BirthDate, @Notes);
                    
                    SELECT SCOPE_IDENTITY();";
                    
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientId", _clientId);
                    command.Parameters.AddWithValue("@TypeId", typeId);
                    command.Parameters.AddWithValue("@Name", PetNameTextBox.Text.Trim());
                    
                    if (!string.IsNullOrWhiteSpace(BreedTextBox.Text))
                        command.Parameters.AddWithValue("@Breed", BreedTextBox.Text.Trim());
                    else
                        command.Parameters.AddWithValue("@Breed", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Gender", gender);
                    
                    if (BirthDatePicker.SelectedDate.HasValue)
                        command.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate.Value);
                    else
                        command.Parameters.AddWithValue("@BirthDate", DBNull.Value);
                    
                    if (!string.IsNullOrWhiteSpace(NotesTextBox.Text))
                        command.Parameters.AddWithValue("@Notes", NotesTextBox.Text.Trim());
                    else
                        command.Parameters.AddWithValue("@Notes", DBNull.Value);
                    
                    int petId = Convert.ToInt32(command.ExecuteScalar());
                    
                    DateTime? birthDate = BirthDatePicker.SelectedDate;
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
                    
                    // Создаем модель для нового питомца
                    PetModel newPet = new PetModel
                    {
                        PetId = petId,
                        ClientId = _clientId,
                        TypeId = typeId,
                        TypeName = _animalTypes[typeId],
                        Name = PetNameTextBox.Text.Trim(),
                        Breed = BreedTextBox.Text?.Trim(),
                        Gender = gender,
                        BirthDate = birthDate,
                        FormattedAge = formattedAge,
                        Notes = NotesTextBox.Text?.Trim()
                    };
                    
                    // Добавляем питомца в коллекцию
                    _pets.Add(newPet);
                    
                    // Создаем карточку для нового питомца
                    CreatePetCard(newPet);
                    
                    // Выбираем нового питомца
                    SelectPet(petId);
                    
                    // Возвращаемся на экран выбора питомца
                    PetSelectionGrid.Visibility = Visibility.Visible;
                    AddPetGrid.Visibility = Visibility.Collapsed;
                    
                    // Скрываем сообщение об отсутствии питомцев, если оно отображалось
                    NoPetsText.Visibility = Visibility.Collapsed;
                    
                    MessageBox.Show($"Питомец '{newPet.Name}' успешно добавлен!", 
                                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных питомца: {ex.Message}", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToDateTimeSelection?.Invoke(this, EventArgs.Empty);
        }
        
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPet != null)
            {
                PetSelectionEventArgs args = new PetSelectionEventArgs
                {
                    ServiceId = _serviceId,
                    ServiceName = _serviceName,
                    AppointmentDate = _appointmentDate,
                    AppointmentTime = _appointmentTime,
                    VeterinarianId = _veterinarianId,
                    VeterinarianName = _veterinarianName,
                    Pet = _selectedPet
                };
                
                PetSelected?.Invoke(this, args);
            }
        }
    }
} 