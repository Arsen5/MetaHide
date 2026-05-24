using System.Collections.Generic;

namespace test
{
    public static class ExifDictionary
    {
        // Словарь: ID поля → человекочитаемое название
        public static readonly Dictionary<int, string> TagNames = new Dictionary<int, string>
        {
            // Основные поля (0th IFD)
            { 0x010E, "ImageDescription (Описание изображения)" },
            { 0x010F, "Make (Производитель камеры)" },
            { 0x0110, "Model (Модель камеры)" },
            { 0x0112, "Orientation (Ориентация)" },
            { 0x011A, "XResolution (X разрешение)" },
            { 0x011B, "YResolution (Y разрешение)" },
            { 0x0128, "ResolutionUnit (Единица разрешения)" },
            { 0x0131, "Software (Программа)" },
            { 0x0132, "DateTime (Дата и время)" },
            { 0x013B, "Artist (Автор)" },
            { 0x013E, "WhitePoint (Белая точка)" },
            { 0x013F, "PrimaryChromaticities (Основные хроматичности)" },
            { 0x0211, "YCbCrCoefficients (Коэффициенты YCbCr)" },
            { 0x0213, "YCbCrPositioning (Позиционирование YCbCr)" },
            { 0x0214, "ReferenceBlackWhite (Опорные черный/белый)" },
            { 0x8298, "Copyright (Авторские права)" },
            { 0x8825, "GPSInfo (GPS информация)" },
            { 0x8827, "ISOSpeedRatings (ISO)" },
            { 0x9000, "ExifVersion (Версия Exif)" },
            { 0x9003, "DateTimeOriginal (Дата и время оригинала)" },
            { 0x9004, "DateTimeDigitized (Дата и время оцифровки)" },
            { 0x9101, "ComponentsConfiguration (Конфигурация компонентов)" },
            { 0x9102, "CompressedBitsPerPixel (Сжатых битов на пиксель)" },
            { 0x9201, "ShutterSpeedValue (Выдержка)" },
            { 0x9202, "ApertureValue (Диафрагма)" },
            { 0x9203, "BrightnessValue (Яркость)" },
            { 0x9204, "ExposureBiasValue (Экспокоррекция)" },
            { 0x9205, "MaxApertureValue (Макс. диафрагма)" },
            { 0x9206, "SubjectDistance (Расстояние до объекта)" },
            { 0x9207, "MeteringMode (Режим замера)" },
            { 0x9208, "LightSource (Источник света)" },
            { 0x9209, "Flash (Вспышка)" },
            { 0x920A, "FocalLength (Фокусное расстояние)" },
            { 0x9214, "SubjectArea (Область объекта)" },
            { 0x927C, "MakerNote (Заметки производителя)" },
            { 0x9286, "UserComment (Пользовательский комментарий)" },
            { 0x9290, "SubsecTime (Доли секунды)" },
            { 0x9291, "SubsecTimeOriginal (Доли секунды оригинала)" },
            { 0x9292, "SubsecTimeDigitized (Доли секунды оцифровки)" },
            { 0xA000, "FlashpixVersion (Версия Flashpix)" },
            { 0xA001, "ColorSpace (Цветовое пространство)" },
            { 0xA002, "PixelXDimension (Ширина в пикселях)" },
            { 0xA003, "PixelYDimension (Высота в пикселях)" },
            { 0xA004, "RelatedSoundFile (Связанный звуковой файл)" },
            { 0xA005, "InteroperabilityIFD (Совместимость)" },
            { 0xA20B, "FlashEnergy (Энергия вспышки)" },
            { 0xA20C, "SpatialFrequencyResponse (Пространственно-частотная характеристика)" },
            { 0xA20E, "FocalPlaneXResolution (X разрешение фокальной плоскости)" },
            { 0xA20F, "FocalPlaneYResolution (Y разрешение фокальной плоскости)" },
            { 0xA210, "FocalPlaneResolutionUnit (Единица разрешения фокальной плоскости)" },
            { 0xA214, "SubjectLocation (Расположение объекта)" },
            { 0xA215, "ExposureIndex (Индекс экспозиции)" },
            { 0xA217, "SensingMethod (Метод сенсора)" },
            { 0xA300, "FileSource (Источник файла)" },
            { 0xA301, "SceneType (Тип сцены)" },
            { 0xA302, "CFAPattern (CFA паттерн)" },
            { 0xA401, "CustomRendered (Пользовательская обработка)" },
            { 0xA402, "ExposureMode (Режим экспозиции)" },
            { 0xA403, "WhiteBalance (Баланс белого)" },
            { 0xA404, "DigitalZoomRatio (Цифровой зум)" },
            { 0xA405, "FocalLengthIn35mmFilm (Фокусное в 35мм экв.)" },
            { 0xA406, "SceneCaptureType (Тип сцены)" },
            { 0xA407, "GainControl (Контроль усиления)" },
            { 0xA408, "Contrast (Контраст)" },
            { 0xA409, "Saturation (Насыщенность)" },
            { 0xA40A, "Sharpness (Резкость)" },
            { 0xA40B, "DeviceSettingDescription (Настройки устройства)" },
            { 0xA40C, "SubjectDistanceRange (Диапазон расстояний)" },
            { 0xA420, "ImageUniqueID (Уникальный ID изображения)" },
            { 0xA430, "CameraOwnerName (Имя владельца камеры)" },
            { 0xA431, "BodySerialNumber (Серийный номер корпуса)" },
            { 0xA432, "LensSpecification (Характеристики объектива)" },
            { 0xA433, "LensMake (Производитель объектива)" },
            { 0xA434, "LensModel (Модель объектива)" },
            { 0xA435, "LensSerialNumber (Серийный номер объектива)" },
            
            // Windows-специфичные поля (для отображения в проводнике)
            { 0x9C9B, "XPTitle (Заголовок Windows)" },
            { 0x9C9C, "XPComment (Комментарий Windows)" },
            { 0x9C9D, "XPAuthor (Автор Windows)" },
            { 0x9C9E, "XPKeywords (Ключевые слова Windows)" },
            { 0x9C9F, "XPSubject (Тема Windows)" },
        };

        // Получить название поля по ID (если знаем, иначе "Неизвестное поле")
        public static string GetTagName(int id)
        {
            if (TagNames.ContainsKey(id))
                return TagNames[id];
            return $"Неизвестное поле (0x{id:X4})";
        }

        // Получить тип данных в человекочитаемом виде
        public static string GetTypeName(short type)
        {
            return type switch
            {
                1 => "BYTE (1 байт)",
                2 => "ASCII (текст)",
                3 => "SHORT (2 байта)",
                4 => "LONG (4 байта)",
                5 => "RATIONAL (дробь 8 байт)",
                7 => "UNDEFINED (любые данные)",
                9 => "SLONG (4 байта со знаком)",
                10 => "SRATIONAL (дробь 8 байт со знаком)",
                _ => $"Неизвестный тип ({type})"
            };
        }

        // Преобразовать байты в читаемую строку (для отображения)
        public static string FormatValue(byte[] value, short type)
        {
            if (value == null || value.Length == 0)
                return "<пусто>";

            // Ограничиваем вывод 100 символами
            int maxDisplay = 100;

            switch (type)
            {
                case 2: // ASCII
                    string text = System.Text.Encoding.ASCII.GetString(value).TrimEnd('\0');
                    if (text.Length > maxDisplay)
                        text = text.Substring(0, maxDisplay) + "...";
                    return $"\"{text}\"";

                case 7: // UNDEFINED (может быть UTF-8 текст)
                    try
                    {
                        string utfText = System.Text.Encoding.UTF8.GetString(value).TrimEnd('\0');
                        if (utfText.Length > maxDisplay)
                            utfText = utfText.Substring(0, maxDisplay) + "...";
                        return $"UTF-8: \"{utfText}\"";
                    }
                    catch
                    {
                        return $"Байты: {BitConverter.ToString(value.Take(16).ToArray())}...";
                    }

                case 3: // SHORT (2 байта)
                    if (value.Length >= 2)
                    {
                        short val = System.BitConverter.ToInt16(value, 0);
                        return val.ToString();
                    }
                    return "<недостаточно байт>";

                case 4: // LONG (4 байта)
                    if (value.Length >= 4)
                    {
                        uint val = System.BitConverter.ToUInt32(value, 0);
                        return val.ToString();
                    }
                    return "<недостаточно байт>";

                default:
                    // Показываем первые 16 байт в hex
                    string hex = BitConverter.ToString(value.Take(16).ToArray());
                    if (value.Length > 16)
                        hex += "...";
                    return hex;
            }
        }
    }
}