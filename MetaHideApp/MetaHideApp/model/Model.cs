using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using test;

namespace MetaHide.model;

public class Model : ISteganography
{
    private bool _hiddenMode = false;
    private const string HIDDEN_MARKER = "##HIDDEN##"; // Маркер скрытых данных

    // Для обычного режима (видимый в EXIF)
    private const int IMAGE_DESCRIPTION = 0x010E;

    public void SetHiddenMode(bool hidden)
    {
        _hiddenMode = hidden;
    }

    public int GetCurrentFieldId()
    {
        return _hiddenMode ? 0xFFFF : IMAGE_DESCRIPTION;
    }

    // Главный метод скрытия
    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);

            if (string.IsNullOrEmpty(data))
                return (false, "Текст не может быть пустым", null);

            // Сохраняем на рабочий стол
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = Path.GetFileNameWithoutExtension(imagePath) + "_hidden.jpg";
            string outputPath = Path.Combine(desktopPath, fileName);

            if (_hiddenMode)
            {
                // СКРЫТЫЙ РЕЖИМ: данные в конец файла
                File.Copy(imagePath, outputPath, true);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] markerBytes = Encoding.ASCII.GetBytes(HIDDEN_MARKER);
                byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);

                using (FileStream fs = new FileStream(outputPath, FileMode.Append))
                {
                    fs.Write(markerBytes, 0, markerBytes.Length);
                    fs.Write(lengthBytes, 0, lengthBytes.Length);
                    fs.Write(dataBytes, 0, dataBytes.Length);
                }
                return (true, $"Скрытые данные добавлены в конец файла (размер {dataBytes.Length} байт)", outputPath);
            }
            else
            {
                // ОБЫЧНЫЙ РЕЖИМ: данные в ImageDescription (видимый)
                using (Image img = Image.FromFile(imagePath))
                {
                    PropertyItem prop = (PropertyItem)Activator.CreateInstance(typeof(PropertyItem), true);
                    prop.Id = IMAGE_DESCRIPTION;
                    prop.Type = 2;
                    prop.Value = Encoding.UTF8.GetBytes(data);
                    prop.Len = prop.Value.Length;
                    img.SetPropertyItem(prop);
                    img.Save(outputPath, ImageFormat.Jpeg);
                }
                return (true, $"Данные записаны в ImageDescription (видимый режим)", outputPath);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    // Главный метод извлечения
    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);

            if (_hiddenMode)
            {
                // СКРЫТЫЙ РЕЖИМ: ищем маркер в конце файла
                byte[] fileBytes = File.ReadAllBytes(imagePath);
                byte[] markerBytes = Encoding.ASCII.GetBytes(HIDDEN_MARKER);

                for (int i = fileBytes.Length - markerBytes.Length; i >= 0; i--)
                {
                    bool found = true;
                    for (int j = 0; j < markerBytes.Length; j++)
                        if (fileBytes[i + j] != markerBytes[j]) { found = false; break; }

                    if (found)
                    {
                        int dataStart = i + markerBytes.Length;
                        if (dataStart + 4 > fileBytes.Length) break;
                        int dataLen = BitConverter.ToInt32(fileBytes, dataStart);
                        if (dataStart + 4 + dataLen > fileBytes.Length) break;
                        byte[] dataBytes = new byte[dataLen];
                        Array.Copy(fileBytes, dataStart + 4, dataBytes, 0, dataLen);
                        string extracted = Encoding.UTF8.GetString(dataBytes);
                        return (true, $"Извлечено {dataLen} байт из скрытых данных", extracted);
                    }
                }
                return (false, "Скрытые данные не найдены", null);
            }
            else
            {
                // ОБЫЧНЫЙ РЕЖИМ: читаем ImageDescription через MetadataExtractor
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                var exifDir = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (exifDir != null)
                {
                    var tag = exifDir.Tags.FirstOrDefault(t => t.Type == IMAGE_DESCRIPTION);
                    if (tag != null && tag.Description != null)
                        return (true, "Данные извлечены из ImageDescription", tag.Description);
                }
                return (false, "Поле ImageDescription не найдено", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    public bool HasHiddenData(string imagePath)
    {
        try
        {
            if (_hiddenMode)
            {
                byte[] fileBytes = File.ReadAllBytes(imagePath);
                byte[] markerBytes = Encoding.ASCII.GetBytes(HIDDEN_MARKER);
                for (int i = fileBytes.Length - markerBytes.Length; i >= 0; i--)
                {
                    bool found = true;
                    for (int j = 0; j < markerBytes.Length; j++)
                        if (fileBytes[i + j] != markerBytes[j]) { found = false; break; }
                    if (found) return true;
                }
                return false;
            }
            else
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                var exifDir = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                return exifDir != null && exifDir.Tags.Any(t => t.Type == IMAGE_DESCRIPTION);
            }
        }
        catch { return false; }
    }

    public string GetAllExifFields(string imagePath)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"═══════════════════════════════════════════════════════════════════════════");
            sb.AppendLine($"📷 Анализ файла: {Path.GetFileName(imagePath)}");
            sb.AppendLine($"🎯 Режим: {(_hiddenMode ? "СКРЫТЫЙ (данные в конце файла)" : "ОБЫЧНЫЙ (EXIF)")}");
            sb.AppendLine($"📦 Размер файла: {new FileInfo(imagePath).Length} байт");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            if (_hiddenMode)
            {
                // Проверяем наличие скрытых данных
                var result = ExtractData(imagePath);
                if (result.success)
                {
                    sb.AppendLine($"🔒 СКРЫТЫЕ ДАННЫЕ НАЙДЕНЫ:");
                    sb.AppendLine($"   📝 Длина: {result.data.Length} символов");
                    sb.AppendLine($"   💬 Содержимое: {result.data.Substring(0, Math.Min(200, result.data.Length))}");
                    sb.AppendLine($"   ⚠️ Эти данные НЕ ВИДНЫ в свойствах изображения");
                }
                else
                {
                    sb.AppendLine($"🔒 Скрытых данных не обнаружено.");
                }
                sb.AppendLine("───────────────────────────────────────────────────────────────────────");
                sb.AppendLine();
            }

            // Показываем EXIF поля для информации (через MetadataExtractor)
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                sb.AppendLine($"📁 EXIF поля (для справки):");
                foreach (var directory in directories)
                {
                    sb.AppendLine($"   📂 {directory.Name}");
                    foreach (var tag in directory.Tags)
                    {
                        string value = tag.Description ?? "<пусто>";
                        if (value.Length > 80) value = value.Substring(0, 80) + "...";
                        sb.AppendLine($"      🔹 {tag.Name}: {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"⚠️ Не удалось прочитать EXIF: {ex.Message}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Ошибка: {ex.Message}";
        }
    }
}