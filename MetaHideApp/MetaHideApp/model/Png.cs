// PngSteganography.cs
using System;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model;

public class PngSteganography : ISteganography
{
    private const string CUSTOM_CHUNK_TYPE = "meta";
    private bool _hiddenMode = false;

    public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
    public int GetCurrentFieldId() => 0;
    public bool SupportsFormat(string filePath) => filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath)) return (false, "Файл не найден", null);
            if (string.IsNullOrEmpty(data)) return (false, "Текст не может быть пустым", null);

            byte[] pngBytes = File.ReadAllBytes(imagePath);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] chunkType = Encoding.ASCII.GetBytes(CUSTOM_CHUNK_TYPE);

            int iendPos = FindIEND(pngBytes);
            if (iendPos == -1) return (false, "Не найден IEND чанк", null);

            byte[] newChunk = CreateChunk(chunkType, dataBytes);
            string outputPath = GetOutputPath(imagePath, "_hidden.png");

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(pngBytes, 0, iendPos);
                ms.Write(newChunk, 0, newChunk.Length);
                ms.Write(pngBytes, iendPos, pngBytes.Length - iendPos);
                File.WriteAllBytes(outputPath, ms.ToArray());
            }

            return (true, $"Данные скрыты в кастомном чанке '{CUSTOM_CHUNK_TYPE}'", outputPath);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath)) return (false, "Файл не найден", null);
            byte[] data = File.ReadAllBytes(imagePath);
            byte[] chunkType = Encoding.ASCII.GetBytes(CUSTOM_CHUNK_TYPE);

            int pos = 8;
            while (pos + 12 <= data.Length)
            {
                int chunkLen = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                bool match = true;
                for (int i = 0; i < 4; i++)
                    if (data[pos + 4 + i] != chunkType[i]) { match = false; break; }

                if (match)
                {
                    byte[] chunkData = new byte[chunkLen];
                    Array.Copy(data, pos + 8, chunkData, 0, chunkLen);
                    return (true, "Данные извлечены", Encoding.UTF8.GetString(chunkData));
                }
                pos += chunkLen + 12;
            }
            return (false, "Кастомный чанк не найден", null);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    public bool HasHiddenData(string imagePath) => ExtractData(imagePath).success;
    public string GetAllExifFields(string imagePath) => ExtractData(imagePath).data ?? "Данные не найдены";

    private string GetOutputPath(string imagePath, string suffix) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileNameWithoutExtension(imagePath) + suffix);

    private byte[] CreateChunk(byte[] chunkType, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // Длина (big-endian)
            WriteUInt32BE(ms, (uint)data.Length);
            ms.Write(chunkType, 0, 4);
            ms.Write(data, 0, data.Length);

            // CRC
            byte[] crcData = new byte[4 + data.Length];
            Array.Copy(chunkType, 0, crcData, 0, 4);
            Array.Copy(data, 0, crcData, 4, data.Length);
            WriteUInt32BE(ms, CalculateCRC32(crcData));

            return ms.ToArray();
        }
    }

    private int FindIEND(byte[] data)
    {
        byte[] iendMarker = { 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
        for (int i = data.Length - iendMarker.Length; i >= 0; i--)
        {
            bool found = true;
            for (int j = 0; j < iendMarker.Length; j++)
                if (data[i + j] != iendMarker[j]) { found = false; break; }
            if (found) return i;
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
            byte b = data[i];
            for (int j = 0; j < 8; j++)
            {
                uint bit = (uint)(((crc >> 31) & 1) ^ ((b >> (7 - j)) & 1));
                crc = (crc << 1) ^ (bit * 0x04C11DB7);
            }
        }
        return crc ^ 0xFFFFFFFF;
    }
}