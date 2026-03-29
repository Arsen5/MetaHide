using System;

namespace test
{
    public interface ISteganography
    {
        // Скрыть данные в изображении
        void HideData(string imagePath, string data);

        // Извлечь данные из изображения
        string ExtractData(string imagePath);

        // Проверить, есть ли скрытые данные
        bool HasHiddenData(string imagePath);
    }
}
