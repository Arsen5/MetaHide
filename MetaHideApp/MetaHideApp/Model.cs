// Model.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms; // для MessageBox

namespace test
{
    internal class Model : ISteganography
    {
        // Временное хранилище для извлечённых данных (для отладки)
        private string _lastExtractedData = "";
        // Внутри класса Model
        public string GetAllExifFields(string imagePath)
        {
            try
            {
                using (Image img = Image.FromFile(imagePath))
                {
                    PropertyItem[] propItems = img.PropertyItems;

                    if (propItems == null || propItems.Length == 0)
                        return "В этом изображении нет EXIF-полей.";

                    // Строим вывод
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
                    sb.AppendLine($"📷 Анализ EXIF полей: {System.IO.Path.GetFileName(imagePath)}");
                    sb.AppendLine($"📁 Всего полей: {propItems.Length}");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
                    sb.AppendLine();

                    foreach (PropertyItem prop in propItems)
                    {
                        string tagName = ExifDictionary.GetTagName(prop.Id);
                        string typeName = ExifDictionary.GetTypeName(prop.Type);
                        string value = ExifDictionary.FormatValue(prop.Value, prop.Type);

                        sb.AppendLine($"🔹 ID: 0x{prop.Id:X4} ({prop.Id})");
                        sb.AppendLine($"   📛 Название: {tagName}");
                        sb.AppendLine($"   📦 Тип: {typeName}");
                        sb.AppendLine($"   📏 Размер: {prop.Len} байт");
                        sb.AppendLine($"   📝 Значение: {value}");
                        sb.AppendLine("───────────────────────────────────────────────────────────────────────");
                        sb.AppendLine();
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка при чтении EXIF: {ex.Message}";
            }
        }
        public void HideData(string imagePath, string data)
        {
            try
            {
                // Проверка: текст не должен быть пустым
                if (string.IsNullOrEmpty(data))
                    throw new Exception("Текст не может быть пустым");

                // Проверка размера (≥1 Кб = 1024 байта)
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                if (dataBytes.Length < 1024)
                    throw new Exception($"Текст слишком короткий! Нужно минимум 1 Кб ({1024} байт), а у вас {dataBytes.Length} байт");

                // Открываем изображение
                using (Image img = Image.FromFile(imagePath))
                {
                    // Получаем существующий EXIF или создаём новый
                    PropertyItem prop = GetOrCreatePropertyItem(img, 0x010E); // 0x010E = ImageDescription

                    // Записываем текст в байтах
                    prop.Value = dataBytes;
                    prop.Len = dataBytes.Length;
                    prop.Type = 2; // ASCII (хотя мы пишем UTF-8, но так принимают)

                    // Устанавливаем свойство
                    img.SetPropertyItem(prop);

                    // Сохраняем с тем же качеством
                    img.Save(imagePath + "_hidden.jpg", ImageFormat.Jpeg);
                }

                MessageBox.Show($"Данные скрыты! Сохранено как: {imagePath}_hidden.jpg", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при скрытии: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string ExtractData(string imagePath)
        {
            try
            {
                using (Image img = Image.FromFile(imagePath))
                {
                    // Пытаемся получить свойство ImageDescription
                    PropertyItem[] propItems = img.PropertyItems;
                    PropertyItem targetProp = null;

                    foreach (var prop in propItems)
                    {
                        if (prop.Id == 0x010E) // ImageDescription
                        {
                            targetProp = prop;
                            break;
                        }
                    }

                    if (targetProp == null)
                        throw new Exception("Скрытые данные не найдены (нет поля ImageDescription)");

                    // Декодируем байты в строку
                    string extracted = Encoding.UTF8.GetString(targetProp.Value);
                    _lastExtractedData = extracted;

                    MessageBox.Show($"Данные извлечены! Длина: {extracted.Length} символов", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return extracted;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при извлечении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        public bool HasHiddenData(string imagePath)
        {
            try
            {
                using (Image img = Image.FromFile(imagePath))
                {
                    foreach (PropertyItem prop in img.PropertyItems)
                    {
                        if (prop.Id == 0x010E && prop.Value.Length > 0)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // Вспомогательный метод: получить существующий PropertyItem или создать новый
        private PropertyItem GetOrCreatePropertyItem(Image img, int id)
        {
            foreach (PropertyItem prop in img.PropertyItems)
            {
                if (prop.Id == id)
                    return prop;
            }

            // Создаём новый (через рефлексию, так как конструктор PropertyItem protected)
            Type type = typeof(PropertyItem);
            PropertyItem newProp = (PropertyItem)Activator.CreateInstance(type, true);
            newProp.Id = id;
            newProp.Type = 2; // ASCII
            return newProp;
        }

        // Для отладки: показать последние извлечённые данные
        public string GetLastExtractedData() => _lastExtractedData;
    }
}