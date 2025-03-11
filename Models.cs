using System;

namespace Veterinary_Clinic
{
    // Модель услуги
    public class ServiceModel
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public int Duration { get; set; } // в минутах
        public bool IsPopular { get; set; }
    }

    // Модель питомца
    public class PetModel
    {
        public int PetId { get; set; }
        public int ClientId { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Breed { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string FormattedAge { get; set; }
        public string Notes { get; set; }
        public string Color { get; set; }
        public string SpecialMarks { get; set; }
        public int MedicalRecordsCount { get; set; }
    }

    // Модель медицинской записи
    public class MedicalRecordModel
    {
        public int MedicalRecordId { get; set; }
        public int PetId { get; set; }
        public int? AppointmentId { get; set; }
        public int VeterinarianId { get; set; }
        public string VeterinarianName { get; set; }
        public DateTime VisitDate { get; set; }
        public string ServiceName { get; set; }
        public string Complaints { get; set; }
        public string Diagnosis { get; set; }
        public string Recommendations { get; set; }
    }

    // Модель медицинской карты для передачи данных
    public class MedicalCardData
    {
        public int ClientId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public int VeterinarianId { get; set; }
        public string VeterinarianName { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; }
        public string Complaints { get; set; }
        public string AdditionalInfo { get; set; }
    }

    // Модель записи на прием
    public class AppointmentModel
    {
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public int VeterinarianId { get; set; }
        public string VeterinarianName { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; }
        public decimal Price { get; set; }
        public AppointmentStatus Status { get; set; }

        public string FormattedDateTime
        {
            get
            {
                return $"{AppointmentDate.ToString("dd.MM.yyyy")} {AppointmentTime}";
            }
        }
    }

    // Аргументы события выбора питомца
    public class PetSelectionEventArgs : EventArgs
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public int VeterinarianId { get; set; }
        public string VeterinarianName { get; set; }
        public PetModel Pet { get; set; }
    }

    // Аргументы события выбора даты и времени
    public class DateTime_TimeSelectedEventArgs : EventArgs
    {
        public DateTime SelectedDate { get; set; }
        public string SelectedTime { get; set; }
        public TimeSpan SelectedTimeSpan { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
    }

    // Перечисление типов фильтра для приемов
    public enum AppointmentFilterType
    {
        All,
        Upcoming,
        Past
    }

    // Перечисление статусов приема
    public enum AppointmentStatus
    {
        Unknown = 0,
        Upcoming = 1,
        Completed = 2,
        Cancelled = 3
    }

    // Модель ветеринара
    public class VeterinarianModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int Experience { get; set; }
        public string Specialty { get; set; }
        public string YearsText { get; set; }
        
        // Для обратной совместимости
        public int VeterinarianId { 
            get { return Id; } 
            set { Id = value; } 
        }
        
        public string FullName { 
            get { return Name; } 
            set { Name = value; } 
        }
        
        public string Position { get; set; }
        public string ImageUrl { get; set; }
    }

    // Модель клиента
    public class ClientModel
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Login { get; set; }
        public string ImageUrl { get; set; }
    }
} 