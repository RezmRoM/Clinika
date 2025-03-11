-- 1. Проверяем структуру таблицы
SELECT COLUMN_NAME, DATA_TYPE, IS_IDENTITY 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'VC_MedKarty';

-- 2. Включаем IDENTITY_INSERT для таблицы (позволяет указывать ID вручную)
SET IDENTITY_INSERT VC_MedKarty ON;

-- 3. Выполняем вставку с указанием ID
INSERT INTO VC_MedKarty (
    zapis_id,           -- Теперь можно указать ID
    klichka_zhivotnogo, 
    diagnoz
)
VALUES (
    1000,               -- Указываем ID вручную
    'Тестовый питомец', 
    'Тестовый диагноз'
);

-- 4. Отключаем IDENTITY_INSERT после вставки
SET IDENTITY_INSERT VC_MedKarty OFF;

-- 5. Проверяем, что запись добавлена
SELECT * FROM VC_MedKarty WHERE zapis_id = 1000; 