using System;

namespace Veterinary_Clinic
{
    /// <summary>
    /// Класс для хранения и управления строкой подключения к базе данных
    /// </summary>
    public static class ConnectionString
    {
        private static string _value = "data source=stud-mssql.sttec.yar.ru,38325;user id=user122_db;password=user122;MultipleActiveResultSets=True;App=EntityFramework";
        
        /// <summary>
        /// Значение строки подключения к базе данных
        /// </summary>
        public static string Value => _value;

        /// <summary>
        /// Устанавливает новое значение строки подключения
        /// </summary>
        /// <param name="newConnectionString">Новая строка подключения</param>
        public static void SetConnectionString(string newConnectionString)
        {
            if (string.IsNullOrEmpty(newConnectionString))
                throw new ArgumentException("Строка подключения не может быть пустой", nameof(newConnectionString));

            _value = newConnectionString;
        }
    }
} 