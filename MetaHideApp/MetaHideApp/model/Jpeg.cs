using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model;

public class JpegSteganography : ISteganography
{
    private bool _hiddenMode = false;
    private const int MAKERNOTE_TAG = 0x927C;

    public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
    public int GetCurrentFieldId() => MAKERNOTE_TAG;
    public bool SupportsFormat(string filePath) =>
        filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);
            if (string.IsNullOrEmpty(data))
                return (false, "Текст не может быть пустым", null);

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            string outputPath = GetOutputPath(imagePath, "_hidden.jpg");

            byte[] cleanedBytes = RemoveExifSegment(File.ReadAllBytes(imagePath));
            byte[] exifBlock = CreateExifBlockWithMakerNote(MAKERNOTE_TAG, dataBytes);

            int eoiPos = FindEOI(cleanedBytes);
            if (eoiPos == -1)
                return (false, "Не найден конец изображения (EOI)", null);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(cleanedBytes, 0, eoiPos);
                ms.Write(exifBlock, 0, exifBlock.Length);
                ms.Write(cleanedBytes, eoiPos, cleanedBytes.Length - eoiPos);
                File.WriteAllBytes(outputPath, ms.ToArray());
            }

            return (true, $"Данные скрыты в поле MakerNote (размер {dataBytes.Length} байт)", outputPath);
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);

            byte[] data = File.ReadAllBytes(imagePath);

            for (int i = 0; i < data.Length - 20; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xE1 &&
                    data[i + 4] == 0x45 && data[i + 5] == 0x78 &&
                    data[i + 6] == 0x69 && data[i + 7] == 0x66 &&
                    data[i + 8] == 0x00 && data[i + 9] == 0x00)
                {
                    bool isLittleEndian = (data[i + 10] == 0x49);
                    int ifdOffset = ReadInt32(data, i + 14, isLittleEndian);
                    int ifdStart = i + 10 + ifdOffset;
                    int numTags = ReadInt16(data, ifdStart, isLittleEndian);

                    for (int t = 0; t < numTags; t++)
                    {
                        int tagOffset = ifdStart + 2 + t * 12;
                        int currentTagId = ReadInt16(data, tagOffset, isLittleEndian);

                        if (currentTagId == MAKERNOTE_TAG)
                        {
                            int dataOffset = ReadInt32(data, tagOffset + 8, isLittleEndian);
                            int dataLen = ReadInt32(data, tagOffset + 4, isLittleEndian);

                            if (i + dataOffset + dataLen <= data.Length)
                            {
                                byte[] extracted = new byte[dataLen];
                                Array.Copy(data, i + dataOffset, extracted, 0, dataLen);
                                string result = Encoding.UTF8.GetString(extracted).TrimEnd('\0');
                                return (true, $"Извлечено {dataLen} байт", result);
                            }
                        }
                    }
                }
            }
            return (false, "Скрытые данные не найдены", null);
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    public bool HasHiddenData(string imagePath)
    {
        var result = ExtractData(imagePath);
        return result.success;
    }

    public string GetAllExifFields(string imagePath)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"═══════════════════════════════════════════════════════════════════════════");
            sb.AppendLine($"📷 Анализ файла: {Path.GetFileName(imagePath)}");
            sb.AppendLine($"📦 Размер файла: {new FileInfo(imagePath).Length} байт");
            sb.AppendLine($"🎯 Поиск скрытых данных в поле MakerNote (0x{MAKERNOTE_TAG:X4})");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");

            var result = ExtractData(imagePath);
            if (result.success)
            {
                sb.AppendLine($"\n🔒 СКРЫТЫЕ ДАННЫЕ НАЙДЕНЫ:");
                sb.AppendLine($"   📝 Длина: {result.data.Length} символов");
                sb.AppendLine($"   💬 Содержимое: {result.data.Substring(0, Math.Min(200, result.data.Length))}");
            }
            else
            {
                sb.AppendLine($"\n🔒 Скрытых данных не обнаружено.");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Ошибка: {ex.Message}";
        }
    }

    // ======================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ========================

    private string GetOutputPath(string imagePath, string suffix) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                     Path.GetFileNameWithoutExtension(imagePath) + suffix);

    private byte[] RemoveExifSegment(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            int i = 0;
            while (i < data.Length - 4)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xE1)
                {
                    if (i + 10 < data.Length &&
                        data[i + 4] == 0x45 && data[i + 5] == 0x78 &&
                        data[i + 6] == 0x69 && data[i + 7] == 0x66 &&
                        data[i + 8] == 0x00 && data[i + 9] == 0x00)
                    {
                        int len = (data[i + 2] << 8) + data[i + 3];
                        i += len + 4;
                        continue;
                    }
                }
                ms.WriteByte(data[i]);
                i++;
            }
            while (i < data.Length)
            {
                ms.WriteByte(data[i]);
                i++;
            }
            return ms.ToArray();
        }
    }

    private byte[] CreateExifBlockWithMakerNote(int tagId, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.WriteByte(0xFF);
            ms.WriteByte(0xE1);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            ms.Write(new byte[] { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 }, 0, 6);

            // TIFF заголовок (Little Endian)
            ms.WriteByte(0x49);
            ms.WriteByte(0x49);
            ms.WriteByte(0x2A);
            ms.WriteByte(0x00);
            ms.WriteByte(0x08);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            // Количество тегов = 1
            ms.WriteByte(0x01);
            ms.WriteByte(0x00);

            // Тег ID
            WriteUInt16(ms, (ushort)tagId, true);
            // Тип = UNDEFINED (7)
            WriteUInt16(ms, 7, true);
            // Количество байт
            WriteUInt32(ms, (uint)data.Length, true);

            // Смещение к данным
            long offsetPos = ms.Position;
            WriteUInt32(ms, 0xFFFFFFFF, true);
            WriteUInt32(ms, 0, true);

            // Данные
            long dataStart = ms.Position;
            ms.Write(data, 0, data.Length);
            if (data.Length % 2 == 1) ms.WriteByte(0);

            // Исправляем offset
            ms.Seek(offsetPos, SeekOrigin.Begin);
            WriteUInt32(ms, (uint)dataStart, true);

            byte[] result = ms.ToArray();
            int totalLen = result.Length - 4;
            result[2] = (byte)((totalLen >> 8) & 0xFF);
            result[3] = (byte)(totalLen & 0xFF);

            return result;
        }
    }

    private int FindEOI(byte[] data)
    {
        for (int i = data.Length - 2; i >= 0; i--)
            if (data[i] == 0xFF && data[i + 1] == 0xD9)
                return i;
        return -1;
    }

    private int ReadInt16(byte[] data, int offset, bool isLittleEndian)
    {
        if (isLittleEndian)
            return data[offset] | (data[offset + 1] << 8);
        return (data[offset] << 8) | data[offset + 1];
    }

    private int ReadInt32(byte[] data, int offset, bool isLittleEndian)
    {
        if (isLittleEndian)
            return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private void WriteUInt16(MemoryStream ms, ushort value, bool isLittleEndian)
    {
        if (isLittleEndian)
        {
            ms.WriteByte((byte)(value & 0xFF));
            ms.WriteByte((byte)((value >> 8) & 0xFF));
        }
        else
        {
            ms.WriteByte((byte)((value >> 8) & 0xFF));
            ms.WriteByte((byte)(value & 0xFF));
        }
    }

    private void WriteUInt32(MemoryStream ms, uint value, bool isLittleEndian)
    {
        if (isLittleEndian)
        {
            ms.WriteByte((byte)(value & 0xFF));
            ms.WriteByte((byte)((value >> 8) & 0xFF));
            ms.WriteByte((byte)((value >> 16) & 0xFF));
            ms.WriteByte((byte)((value >> 24) & 0xFF));
        }
        else
        {
            ms.WriteByte((byte)((value >> 24) & 0xFF));
            ms.WriteByte((byte)((value >> 16) & 0xFF));
            ms.WriteByte((byte)((value >> 8) & 0xFF));
            ms.WriteByte((byte)(value & 0xFF));
        }
    }
}