using System;
using System.Windows;
using System.Windows.Media;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Вспомогательные методы, заменяющие конвертеры
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Преобразует логическое значение в Visibility
        /// </summary>
        public static Visibility BooleanToVisibility(bool value, bool invert = false)
        {
            if (invert)
            {
                return value ? Visibility.Collapsed : Visibility.Visible;
            }
            return value ? Visibility.Visible : Visibility.Collapsed;
        }
        
        /// <summary>
        /// Преобразует строку в Visibility (пустая строка = Collapsed)
        /// </summary>
        public static Visibility StringToVisibility(string value, bool invert = false)
        {
            if (invert)
            {
                return string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
            }
            return string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        /// <summary>
        /// Преобразует статус приема в текст
        /// </summary>
        public static string AppointmentStatusToText(AppointmentStatus status)
        {
            switch (status)
            {
                case AppointmentStatus.Upcoming:
                    return "Предстоит";
                case AppointmentStatus.Completed:
                    return "Проведен";
                case AppointmentStatus.Cancelled:
                    return "Отменен";
                default:
                    return "Неизвестно";
            }
        }
        
        /// <summary>
        /// Преобразует статус приема в текст
        /// </summary>
        public static string AppointmentStatusToText(string statusString)
        {
            if (string.IsNullOrEmpty(statusString))
                return "Неизвестно";
                
            switch (statusString.ToLower())
            {
                case "предстоит":
                    return "Предстоит";
                case "проведена":
                case "проведен":
                    return "Проведен";
                case "отменена":
                case "отменен":
                    return "Отменен";
                default:
                    return "Неизвестно";
            }
        }
        
        /// <summary>
        /// Преобразует статус приема в цвет
        /// </summary>
        public static SolidColorBrush AppointmentStatusToColor(AppointmentStatus status)
        {
            switch (status)
            {
                case AppointmentStatus.Upcoming:
                    return new SolidColorBrush(Colors.Green);
                case AppointmentStatus.Completed:
                    return new SolidColorBrush(Colors.Blue);
                case AppointmentStatus.Cancelled:
                    return new SolidColorBrush(Colors.Red);
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }
        
        /// <summary>
        /// Преобразует статус приема (строка) в цвет
        /// </summary>
        public static SolidColorBrush AppointmentStatusToColor(string statusString)
        {
            if (string.IsNullOrEmpty(statusString))
                return new SolidColorBrush(Colors.Gray);
                
            switch (statusString.ToLower())
            {
                case "предстоит":
                    return new SolidColorBrush(Colors.Green);
                case "проведена":
                case "проведен":
                    return new SolidColorBrush(Colors.Blue);
                case "отменена":
                case "отменен":
                    return new SolidColorBrush(Colors.Red);
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }
        
        /// <summary>
        /// Форматирует дату в строку по шаблону
        /// </summary>
        public static string FormatDateTime(DateTime dateTime, string format = "dd.MM.yyyy")
        {
            return dateTime.ToString(format);
        }
    }
} 