using System;

namespace test
{
    public interface ISteganography
    {
        // Установить режим (true = скрытый, false = видимый)
        void SetHiddenMode(bool hidden);

        // Получить текущий ID поля
        int GetCurrentFieldId();

        // Скрыть данные (возвращает успех, сообщение, путь к выходному файлу)
        (bool success, string message, string outputPath) HideData(string imagePath, string data);

        // Извлечь данные (возвращает успех, сообщение, данные)
        (bool success, string message, string data) ExtractData(string imagePath);

        // Проверить, есть ли скрытые данные
        bool HasHiddenData(string imagePath);

        // Получить все EXIF поля (для отладки)
        string GetAllExifFields(string imagePath);
        // для разных форматов
        bool SupportsFormat(string filePath);
    }
}