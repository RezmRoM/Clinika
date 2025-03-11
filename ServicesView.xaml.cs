using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Логика взаимодействия для ServicesView.xaml
    /// </summary>
    public partial class ServicesView : UserControl
    {
        private List<ServiceModel> services;

        // Событие выбора услуги
        public event Action<ServiceModel> ServiceSelected;

        private ObservableCollection<ServiceModel> _allServices = new ObservableCollection<ServiceModel>();
        private ObservableCollection<ServiceModel> _filteredServices = new ObservableCollection<ServiceModel>();

        // Текущий активный фильтр и поисковый запрос
        private string _currentFilter = "all";
        private string _searchQuery = string.Empty;

        // Категории услуг
        private Dictionary<int, string> _serviceCategories = new Dictionary<int, string>
        {
            { 1, "examination" }, // Первичный осмотр
            { 2, "prevention" },  // Вакцинация
            { 3, "aesthetics" },  // Стрижка когтей
            { 4, "surgery" },     // Хирургия
            { 5, "dental" },      // Стоматология
            { 6, "diagnostics" }, // УЗИ
            { 7, "examination" }, // Лечение кожных заболеваний
            { 8, "diagnostics" }  // Кардиология
        };

        // Словарь изображений для плейсхолдеров
        private Dictionary<string, string> _categoryImages = new Dictionary<string, string>
        {
            { "examination", "/Resources/Images/services/examination.jpg" },
            { "prevention", "/Resources/Images/services/vaccination.jpg" },
            { "aesthetics", "/Resources/Images/services/nails.jpg" },
            { "surgery", "/Resources/Images/services/surgery.jpg" },
            { "dental", "/Resources/Images/services/dental.jpg" },
            { "diagnostics", "/Resources/Images/services/ultrasound.jpg" }
        };

        public ServicesView()
        {
            InitializeComponent();
            InitializeView();
        }

        private void InitializeView()
        {
            // Инициализируем элементы управления
            if (SearchBox != null)
            {
                SearchBox.TextChanged += SearchBox_TextChanged;
                UpdateSearchControls();
            }

            // Устанавливаем настройки по умолчанию для сортировки
            if (SortComboBox != null && SortComboBox.Items.Count > 0)
            {
                SortComboBox.SelectedIndex = 0; // По популярности
            }

            // Загружаем данные и применяем фильтры
            LoadServices();
            InitializeFilters();
        }

        private void InitializeFilters()
        {
            // Установка выбранного фильтра
            SetSelectedFilter(_currentFilter);
        }

        private void LoadServices()
        {
            services = new List<ServiceModel>();
            _allServices.Clear(); // Очищаем коллекцию перед загрузкой новых данных

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.Value))
                {
                    connection.Open();
                    // Исправляем запрос, чтобы он соответствовал структуре таблицы
                    string query = @"SELECT usluga_id, nazvaniye_uslugi, stoimost, url_izobrazhenija 
                                    FROM VC_Uslugi";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Присваиваем категорию по умолчанию
                                string category = "examination";

                                // Создаем объект услуги из доступных данных
                                ServiceModel service = new ServiceModel
                                {
                                    ServiceId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Price = reader.GetDecimal(2),
                                    ImageUrl = !reader.IsDBNull(3) ? reader.GetString(3) :
                                               _categoryImages.ContainsKey(category) ? _categoryImages[category] :
                                               "https://cdn-icons-png.flaticon.com/512/2138/2138508.png", // Изображение по умолчанию
                                    Description = "Подробное описание услуги временно недоступно", // Значение по умолчанию
                                    Duration = 30, // Длительность по умолчанию - 30 минут
                                    Category = category // Категория по умолчанию
                                };

                                services.Add(service);
                                _allServices.Add(service); // Добавляем в ObservableCollection для фильтрации
                            }
                        }
                    }
                }

                // Обновляем интерфейс
                ApplyFilters(); // Это обновит ServicesItemsControl.ItemsSource
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ServiceModel service)
            {
                // Уведомляем об выборе услуги
                ServiceSelected?.Invoke(service);
            }
        }

        private void BookServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int serviceId)
            {
                // Находим выбранную услугу по ID
                ServiceModel selectedService = services.Find(s => s.ServiceId == serviceId);

                if (selectedService != null)
                {
                    // Уведомляем об выборе услуги
                    ServiceSelected?.Invoke(selectedService);
                }
            }
        }

        private void ApplyFilters()
        {
            // Проверяем, что коллекции инициализированы
            if (_allServices == null)
            {
                _allServices = new ObservableCollection<ServiceModel>();
            }

            if (_filteredServices == null)
            {
                _filteredServices = new ObservableCollection<ServiceModel>();
            }

            // Проверяем, что элементы управления инициализированы
            if (ServicesItemsControl == null || SortComboBox == null)
            {
                return;
            }

            // Применяем фильтры и сортировку
            IEnumerable<ServiceModel> filteredServices = _allServices;

            // Применяем фильтр по категории
            if (_currentFilter != "all")
            {
                filteredServices = filteredServices.Where(s => s.Category == _currentFilter);
            }

            // Применяем поисковый запрос
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                filteredServices = filteredServices.Where(s =>
                    s.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (s.Description != null && s.Description.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Применяем сортировку
            if (SortComboBox.SelectedIndex >= 0)
            {
                string sortOption = ((ComboBoxItem)SortComboBox.SelectedItem).Content.ToString();

                switch (sortOption)
                {
                    case "По цене (возр.)":
                        filteredServices = filteredServices.OrderBy(s => s.Price);
                        break;
                    case "По цене (убыв.)":
                        filteredServices = filteredServices.OrderByDescending(s => s.Price);
                        break;
                    case "По алфавиту":
                        filteredServices = filteredServices.OrderBy(s => s.Name);
                        break;
                    case "По популярности":
                    default:
                        // По умолчанию сортировка по ID (условно по популярности)
                        filteredServices = filteredServices.OrderBy(s => s.ServiceId);
                        break;
                }
            }

            // Обновляем отображаемый список
            _filteredServices.Clear();
            foreach (var service in filteredServices)
            {
                _filteredServices.Add(service);
            }

            ServicesItemsControl.ItemsSource = _filteredServices;
        }

        private void SetSelectedFilter(string filterTag)
        {
            // Сбрасываем выделение всех фильтров
            AllFilter.Background = (Brush)FindResource("SurfaceBrush");
            ExaminationFilter.Background = (Brush)FindResource("SurfaceBrush");
            PreventionFilter.Background = (Brush)FindResource("SurfaceBrush");
            SurgeryFilter.Background = (Brush)FindResource("SurfaceBrush");
            DiagnosticsFilter.Background = (Brush)FindResource("SurfaceBrush");
            DentalFilter.Background = (Brush)FindResource("SurfaceBrush");
            AestheticsFilter.Background = (Brush)FindResource("SurfaceBrush");

            // Устанавливаем выделение для выбранного фильтра
            switch (filterTag)
            {
                case "all":
                    AllFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "examination":
                    ExaminationFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "prevention":
                    PreventionFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "surgery":
                    SurgeryFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "diagnostics":
                    DiagnosticsFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "dental":
                    DentalFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
                case "aesthetics":
                    AestheticsFilter.Background = (Brush)FindResource("PrimaryBrush");
                    break;
            }

            // Устанавливаем цвет текста для выбранного фильтра
            if (AllFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)AllFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)AllFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (ExaminationFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)ExaminationFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)ExaminationFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (PreventionFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)PreventionFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)PreventionFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (SurgeryFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)SurgeryFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)SurgeryFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (DiagnosticsFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)DiagnosticsFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)DiagnosticsFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (DentalFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)DentalFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)DentalFilter.Child).Foreground = (Brush)FindResource("TextBrush");

            if (AestheticsFilter.Background == (Brush)FindResource("PrimaryBrush"))
                ((TextBlock)AestheticsFilter.Child).Foreground = Brushes.White;
            else
                ((TextBlock)AestheticsFilter.Child).Foreground = (Brush)FindResource("TextBrush");
        }

        // Обработчики событий UI

        private void FilterChip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border filterChip && filterChip.Tag != null)
            {
                string filterTag = filterChip.Tag.ToString();

                // Запоминаем выбранный фильтр
                _currentFilter = filterTag;

                // Визуально выделяем выбранный фильтр
                SetSelectedFilter(filterTag);

                // Применяем фильтры
                ApplyFilters();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text.Trim();
            UpdateSearchControls();
            ApplyFilters();
        }

        private void UpdateSearchControls()
        {
            // Проверяем, что элементы управления инициализированы
            if (SearchPlaceholder == null || ClearSearchButton == null || SearchBox == null)
            {
                return;
            }

            // Управляем видимостью плейсхолдера и кнопки очистки
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            ClearSearchButton.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
    }
}