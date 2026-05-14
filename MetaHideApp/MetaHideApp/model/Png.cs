// PngSteganography.cs
using System;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model;

public class PngSteganography : ISteganography
{
    private const string CUSTOM_CHUNK_TYPE = "meta";
    private const string VISIBLE_KEYWORD = "Description";
    private bool _hiddenMode = false;

    public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
    public int GetCurrentFieldId() => 0;
    public bool SupportsFormat(string filePath) =>
        filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    // ========================== ЗАПИСЬ ==========================
    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath)) return (false, "Файл не найден", null);
            if (string.IsNullOrEmpty(data)) return (false, "Текст не может быть пустым", null);

            byte[] pngBytes = File.ReadAllBytes(imagePath);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            string outputPath = GetOutputPath(imagePath, "_hidden.png");

            // Находим позицию IEND (он всегда последний)
            int iendPos = FindIEND(pngBytes);
            if (iendPos == -1) return (false, "Не найден IEND чанк", null);

            // Отделяем всё до IEND
            byte[] beforeIEND = new byte[iendPos];
            Array.Copy(pngBytes, 0, beforeIEND, 0, iendPos);

            using (MemoryStream ms = new MemoryStream())
            {
                // Пишем всё, кроме IEND
                ms.Write(beforeIEND, 0, beforeIEND.Length);

                if (_hiddenMode)
                {
                    // СКРЫТЫЙ РЕЖИМ: кастомный чанк
                    byte[] chunkType = Encoding.ASCII.GetBytes(CUSTOM_CHUNK_TYPE);
                    byte[] newChunk = CreateChunk(chunkType, dataBytes);
                    ms.Write(newChunk, 0, newChunk.Length);
                }
                else
                {
                    // ВИДИМЫЙ РЕЖИМ: tEXt чанк
                    byte[] textChunk = CreateTextChunk(VISIBLE_KEYWORD, dataBytes);
                    ms.Write(textChunk, 0, textChunk.Length);
                }

                // Добавляем IEND обратно
                ms.Write(pngBytes, iendPos, pngBytes.Length - iendPos);
                File.WriteAllBytes(outputPath, ms.ToArray());
            }

            return (true, _hiddenMode ? "Данные скрыты (не видны)" : "Данные записаны (видны в свойствах)", outputPath);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    // ========================== ИЗВЛЕЧЕНИЕ ==========================
    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);

            byte[] data = File.ReadAllBytes(imagePath);
            byte[] customChunkType = Encoding.ASCII.GetBytes(CUSTOM_CHUNK_TYPE);
            byte[] textChunkType = Encoding.ASCII.GetBytes("tEXt");

            string hiddenText = null;
            string visibleText = null;

            int pos = 8; // пропускаем PNG сигнатуру

            while (pos + 12 <= data.Length)
            {
                int chunkLen = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                if (chunkLen < 0 || pos + chunkLen + 12 > data.Length) break;

                // Проверяем тип чанка
                byte[] chunkType = new byte[4];
                Array.Copy(data, pos + 4, chunkType, 0, 4);

                // Скрытый чанк "meta"
                if (chunkType[0] == customChunkType[0] && chunkType[1] == customChunkType[1] &&
                    chunkType[2] == customChunkType[2] && chunkType[3] == customChunkType[3])
                {
                    byte[] chunkData = new byte[chunkLen];
                    Array.Copy(data, pos + 8, chunkData, 0, chunkLen);
                    hiddenText = Encoding.UTF8.GetString(chunkData);
                }

                // Видимый чанк "tEXt"
                if (chunkType[0] == textChunkType[0] && chunkType[1] == textChunkType[1] &&
                    chunkType[2] == textChunkType[2] && chunkType[3] == textChunkType[3])
                {
                    // Извлекаем текст из tEXt чанка
                    int keywordEnd = pos + 8;
                    while (keywordEnd < pos + 8 + chunkLen && data[keywordEnd] != 0)
                        keywordEnd++;

                    if (keywordEnd < pos + 8 + chunkLen)
                    {
                        int dataStart = keywordEnd + 1;
                        int dataLen = chunkLen - (dataStart - (pos + 8));
                        if (dataLen > 0)
                        {
                            byte[] textData = new byte[dataLen];
                            Array.Copy(data, dataStart, textData, 0, dataLen);
                            visibleText = Encoding.UTF8.GetString(textData);
                        }
                    }
                }

                pos += chunkLen + 12;
            }

            // Формируем результат
            StringBuilder result = new StringBuilder();
            if (!string.IsNullOrEmpty(hiddenText))
                result.AppendLine($"🔒 Скрытый режим: {hiddenText}");
            if (!string.IsNullOrEmpty(visibleText))
                result.AppendLine($"📄 Обычный режим: {visibleText}");

            if (result.Length > 0)
                return (true, "Данные найдены", result.ToString().TrimEnd());
            else
                return (false, "Данные не найдены", null);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    public bool HasHiddenData(string imagePath) => ExtractData(imagePath).success;
    public string GetAllExifFields(string imagePath) => ExtractData(imagePath).data ?? "Данные не найдены";

    // ========================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========================
    private string GetOutputPath(string imagePath, string suffix) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                     Path.GetFileNameWithoutExtension(imagePath) + suffix);

    private byte[] CreateChunk(byte[] chunkType, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // Длина (big-endian)
            WriteUInt32BE(ms, (uint)data.Length);
            // Тип
            ms.Write(chunkType, 0, 4);
            // Данные
            ms.Write(data, 0, data.Length);
            // CRC
            byte[] crcData = new byte[4 + data.Length];
            Array.Copy(chunkType, 0, crcData, 0, 4);
            Array.Copy(data, 0, crcData, 4, data.Length);
            WriteUInt32BE(ms, CalculateCRC32(crcData));
            return ms.ToArray();
        }
    }

    private byte[] CreateTextChunk(string keyword, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            byte[] keywordBytes = Encoding.ASCII.GetBytes(keyword);
            byte[] chunkData = new byte[keywordBytes.Length + 1 + data.Length];
            Array.Copy(keywordBytes, 0, chunkData, 0, keywordBytes.Length);
            chunkData[keywordBytes.Length] = 0; // разделитель
            Array.Copy(data, 0, chunkData, keywordBytes.Length + 1, data.Length);

            WriteUInt32BE(ms, (uint)chunkData.Length);
            ms.Write(Encoding.ASCII.GetBytes("tEXt"), 0, 4);
            ms.Write(chunkData, 0, chunkData.Length);

            byte[] crcData = new byte[4 + chunkData.Length];
            Array.Copy(Encoding.ASCII.GetBytes("tEXt"), 0, crcData, 0, 4);
            Array.Copy(chunkData, 0, crcData, 4, chunkData.Length);
            WriteUInt32BE(ms, CalculateCRC32(crcData));
            return ms.ToArray();
        }
    }

    private int FindIEND(byte[] data)
    {
        // Ищем IEND чанк с конца файла
        byte[] iendSignature = { 0x49, 0x45, 0x4E, 0x44 };
        for (int i = data.Length - 12; i >= 0; i--)
        {
            if (data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x00 && data[i + 3] == 0x00 &&
                data[i + 4] == iendSignature[0] && data[i + 5] == iendSignature[1] &&
                data[i + 6] == iendSignature[2] && data[i + 7] == iendSignature[3])
            {
                return i;
            }
        }
        return -1;
    }

    private void WriteUInt32BE(MemoryStream ms, uint value)
    {
        ms.WriteByte((byte)((value >> 24) & 0xFF));
        ms.WriteByte((byte)((value >> 16) & 0xFF));
        ms.WriteByte((byte)((value >> 8) & 0xFF));
        ms.WriteByte((byte)(value & 0xFF));
    }

    private uint CalculateCRC32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            uint byteVal = data[i];
            for (int j = 0; j < 8; j++)
            {
                uint bit = (crc >> 31) & 1;
                crc <<= 1;
                if ((byteVal >> (7 - j) & 1) != bit)
                    crc ^= 0x04C11DB7;
            }
        }
        return crc ^ 0xFFFFFFFF;
    }
}