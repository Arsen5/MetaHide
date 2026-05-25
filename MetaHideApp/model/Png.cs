using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
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

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            string outputPath = GetOutputPath(imagePath, "_hidden.png");

            byte[] pngBytes = File.ReadAllBytes(imagePath);
            int iendPos = FindIEND(pngBytes);
            if (iendPos == -1)
                return (false, "Не найден IEND чанк", null);

            byte[] beforeIEND = new byte[iendPos];
            Array.Copy(pngBytes, 0, beforeIEND, 0, iendPos);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(beforeIEND, 0, beforeIEND.Length);

                if (_hiddenMode)
                {
                    byte[] chunkType = Encoding.ASCII.GetBytes(CUSTOM_CHUNK_TYPE);
                    byte[] newChunk = CreateChunk(chunkType, dataBytes);
                    ms.Write(newChunk, 0, newChunk.Length);
                }
                else
                {
                    byte[] textChunk = CreateTextChunk(VISIBLE_KEYWORD, dataBytes);
                    ms.Write(textChunk, 0, textChunk.Length);
                }

                ms.Write(pngBytes, iendPos, pngBytes.Length - iendPos);
                File.WriteAllBytes(outputPath, ms.ToArray());
            }

            string modeName = _hiddenMode ? "скрытый" : "обычный";
            return (true, $"Данные записаны ({modeName} режим, {dataBytes.Length} байт)", outputPath);
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

            List<string> allMessages = new List<string>();

            int pos = 8;

            while (pos + 12 <= data.Length)
            {
                int chunkLen = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                if (chunkLen < 0 || pos + chunkLen + 12 > data.Length) break;

                byte[] chunkType = new byte[4];
                Array.Copy(data, pos + 4, chunkType, 0, 4);

                // Скрытый чанк "meta"
                if (chunkType[0] == customChunkType[0] && chunkType[1] == customChunkType[1] &&
                    chunkType[2] == customChunkType[2] && chunkType[3] == customChunkType[3])
                {
                    byte[] chunkData = new byte[chunkLen];
                    Array.Copy(data, pos + 8, chunkData, 0, chunkLen);
                    string text = Encoding.UTF8.GetString(chunkData);
                    if (!string.IsNullOrEmpty(text))
                        allMessages.Add(text);
                }

                // Видимый чанк "tEXt"
                if (chunkType[0] == textChunkType[0] && chunkType[1] == textChunkType[1] &&
                    chunkType[2] == textChunkType[2] && chunkType[3] == textChunkType[3])
                {
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
                            string text = Encoding.UTF8.GetString(textData);
                            if (!string.IsNullOrEmpty(text))
                                allMessages.Add(text);
                        }
                    }
                }

                pos += chunkLen + 12;
            }

            if (allMessages.Count > 0)
                return (true, "Данные найдены", string.Join(Environment.NewLine, allMessages));
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
            WriteUInt32BE(ms, (uint)data.Length);
            ms.Write(chunkType, 0, 4);
            ms.Write(data, 0, data.Length);

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
            chunkData[keywordBytes.Length] = 0;
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
        for (int i = data.Length - 12; i >= 0; i--)
        {
            if (data[i] == 0x49 && data[i + 1] == 0x45 &&
                data[i + 2] == 0x4E && data[i + 3] == 0x44)
            {
                if (i >= 4 && data[i - 4] == 0x00 && data[i - 3] == 0x00 &&
                    data[i - 2] == 0x00 && data[i - 1] == 0x00)
                {
                    return i - 4;
                }
            }
        }
        return FindLastValidChunk(data);
    }

    private int FindLastValidChunk(byte[] data)
    {
        int pos = 8;
        int lastValidPos = pos;

        while (pos + 12 <= data.Length)
        {
            int chunkLen = (data[pos] << 24) | (data[pos + 1] << 16) |
                          (data[pos + 2] << 8) | data[pos + 3];

            if (chunkLen < 0 || chunkLen > 10000000)
                break;

            if (pos + 12 + chunkLen > data.Length)
                break;

            byte[] chunkType = new byte[4];
            Array.Copy(data, pos + 4, chunkType, 0, 4);

            bool isValidType = true;
            for (int i = 0; i < 4; i++)
            {
                if (!((chunkType[i] >= 'A' && chunkType[i] <= 'Z') ||
                      (chunkType[i] >= 'a' && chunkType[i] <= 'z')))
                {
                    isValidType = false;
                    break;
                }
            }

            if (isValidType)
            {
                lastValidPos = pos;
                pos += chunkLen + 12;
            }
            else
            {
                break;
            }
        }

        return lastValidPos;
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