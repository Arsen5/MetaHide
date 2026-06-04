using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using test;

namespace MetaHide.model;

public class PngSteganography : ISteganography
{
    private bool _hiddenMode = false;
    private const string CUSTOM_CHUNK_TYPE = "meta";
    private const string XMP_KEYWORD = "XML:com.adobe.xmp";

    public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
    public int GetCurrentFieldId() => 0;
    public bool SupportsFormat(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext == ".png";
    }

    // ========================== ЗАПИСЬ ==========================
    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath)) return (false, "Файл не найден", null);
            if (string.IsNullOrEmpty(data)) return (false, "Текст не может быть пустым", null);

            string outputPath = GetOutputPath(imagePath, "_hidden.png");
            byte[] originalBytes = File.ReadAllBytes(imagePath);

            if (_hiddenMode)
            {
                // Скрытый режим: используем кастомный чанк "meta"
                return HideDataHiddenMode(originalBytes, data, outputPath);
            }
            else
            {
                // Видимый режим: используем XMP через iTXt чанк
                return HideDataVisibleMode(originalBytes, data, outputPath);
            }
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    private (bool success, string message, string outputPath) HideDataHiddenMode(byte[] originalBytes, string data, string outputPath)
    {
        // Находим IEND чанк
        int iendPos = FindIEND(originalBytes);
        if (iendPos == -1)
            return (false, "Не найден IEND чанк", null);

        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using (MemoryStream ms = new MemoryStream())
        {
            // Копируем все до IEND
            ms.Write(originalBytes, 0, iendPos);

            // Создаем кастомный чанк "meta"
            byte[] metaChunk = CreateMetaChunk(dataBytes);
            ms.Write(metaChunk, 0, metaChunk.Length);

            // Копируем IEND и остаток файла
            ms.Write(originalBytes, iendPos, originalBytes.Length - iendPos);

            File.WriteAllBytes(outputPath, ms.ToArray());
        }

        return (true, "Данные скрыты в кастомном чанке", outputPath);
    }

    private (bool success, string message, string outputPath) HideDataVisibleMode(byte[] originalBytes, string data, string outputPath)
    {
        // Находим IEND чанк
        int iendPos = FindIEND(originalBytes);
        if (iendPos == -1)
            return (false, "Не найден IEND чанк", null);

        // Создаем XMP чанк
        byte[] xmpChunk = CreateXMPChunk(data);

        using (MemoryStream ms = new MemoryStream())
        {
            // Копируем все до IEND
            ms.Write(originalBytes, 0, iendPos);

            // Вставляем XMP чанк перед IEND
            ms.Write(xmpChunk, 0, xmpChunk.Length);

            // Копируем IEND и остаток файла
            ms.Write(originalBytes, iendPos, originalBytes.Length - iendPos);

            File.WriteAllBytes(outputPath, ms.ToArray());
        }

        return (true, "Данные записаны в XMP метаданные", outputPath);
    }

    // ========================== ИЗВЛЕЧЕНИЕ ==========================
    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return (false, "Файл не найден", null);

            byte[] fileBytes = File.ReadAllBytes(imagePath);

            // 1. Сначала ищем в видимом режиме (XMP)
            string visibleText = ExtractXMPData(fileBytes);
            if (!string.IsNullOrEmpty(visibleText))
                return (true, "Найдены данные в XMP метаданных", visibleText);

            // 2. Если не нашли, ищем в скрытом режиме (кастомный чанк)
            string hiddenText = ExtractHiddenData(fileBytes);
            if (!string.IsNullOrEmpty(hiddenText))
                return (true, "Найдены скрытые данные", hiddenText);

            return (false, "Данные не найдены", null);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    private string ExtractXMPData(byte[] fileBytes)
    {
        int pos = 8; // Пропускаем PNG сигнатуру

        while (pos + 12 <= fileBytes.Length)
        {
            if (pos + 4 > fileBytes.Length) break;

            int chunkLen = (fileBytes[pos] << 24) | (fileBytes[pos + 1] << 16) |
                          (fileBytes[pos + 2] << 8) | fileBytes[pos + 3];

            if (chunkLen < 0 || chunkLen > 10000000) break;
            if (pos + 12 + chunkLen > fileBytes.Length) break;

            string chunkType = Encoding.ASCII.GetString(fileBytes, pos + 4, 4);

            if (chunkType == "iTXt")
            {
                // Извлекаем данные из iTXt чанка
                string xmpText = ExtractITxtData(fileBytes, pos, chunkLen);
                if (!string.IsNullOrEmpty(xmpText))
                    return xmpText;
            }

            pos += chunkLen + 12;
        }

        return null;
    }

    private string ExtractITxtData(byte[] fileBytes, int pos, int chunkLen)
    {
        try
        {
            // Структура iTXt: keyword + \0 + compression + method + language + \0 + translated + \0 + text
            int dataStart = pos + 8; // Пропускаем длину и тип

            // Находим конец keyword
            int keywordEnd = dataStart;
            while (keywordEnd < dataStart + chunkLen && fileBytes[keywordEnd] != 0)
                keywordEnd++;

            if (keywordEnd >= dataStart + chunkLen) return null;

            string keyword = Encoding.ASCII.GetString(fileBytes, dataStart, keywordEnd - dataStart);

            // Проверяем, что это XMP keyword
            if (keyword != XMP_KEYWORD) return null;

            // Пропускаем keyword + \0
            int posAfterKeyword = keywordEnd + 1;

            // Пропускаем compression flag (1 байт) и method (1 байт)
            if (posAfterKeyword + 2 >= dataStart + chunkLen) return null;
            posAfterKeyword += 2;

            // Пропускаем language (до \0)
            while (posAfterKeyword < dataStart + chunkLen && fileBytes[posAfterKeyword] != 0)
                posAfterKeyword++;
            if (posAfterKeyword >= dataStart + chunkLen) return null;
            posAfterKeyword++; // Пропускаем \0

            // Пропускаем translated keyword (до \0)
            while (posAfterKeyword < dataStart + chunkLen && fileBytes[posAfterKeyword] != 0)
                posAfterKeyword++;
            if (posAfterKeyword >= dataStart + chunkLen) return null;
            posAfterKeyword++; // Пропускаем \0

            // Остальное - это XMP данные
            int xmpLength = dataStart + chunkLen - posAfterKeyword;
            if (xmpLength <= 0) return null;

            string xmp = Encoding.UTF8.GetString(fileBytes, posAfterKeyword, xmpLength);

            // Извлекаем текст из XMP
            return ExtractTextFromXMP(xmp);
        }
        catch { return null; }
    }

    private string ExtractTextFromXMP(string xmp)
    {
        try
        {
            // Простой парсинг XMP для извлечения dc:title или dc:description
            int start = xmp.IndexOf("<dc:title>");
            if (start != -1)
            {
                start += 10; // длина "<dc:title>"
                int end = xmp.IndexOf("</dc:title>", start);
                if (end != -1 && start < end)
                    return xmp.Substring(start, end - start);
            }

            start = xmp.IndexOf("<dc:description>");
            if (start != -1)
            {
                start += 16; // длина "<dc:description>"
                int end = xmp.IndexOf("</dc:description>", start);
                if (end != -1 && start < end)
                    return xmp.Substring(start, end - start);
            }

            // Альтернативно, ищем в rdf:li
            start = xmp.IndexOf("<rdf:li");
            if (start != -1)
            {
                start = xmp.IndexOf('>', start) + 1;
                int end = xmp.IndexOf("</rdf:li>", start);
                if (end != -1 && start < end)
                    return xmp.Substring(start, end - start);
            }
        }
        catch { }
        return null;
    }

    private string ExtractHiddenData(byte[] fileBytes)
    {
        byte[] marker = Encoding.ASCII.GetBytes("##HIDDEN##");

        for (int i = fileBytes.Length - marker.Length; i >= 0; i--)
        {
            bool found = true;
            for (int j = 0; j < marker.Length; j++)
            {
                if (fileBytes[i + j] != marker[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                int start = i + marker.Length;
                if (start + 4 > fileBytes.Length) continue;

                int len = BitConverter.ToInt32(fileBytes, start);
                if (start + 4 + len > fileBytes.Length) continue;

                byte[] extracted = new byte[len];
                Array.Copy(fileBytes, start + 4, extracted, 0, len);
                return Encoding.UTF8.GetString(extracted);
            }
        }

        return null;
    }

    // ========================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========================
    private byte[] CreateMetaChunk(byte[] data)
    {
        // Создаем кастомный чанк "meta" с данными
        byte[] marker = Encoding.ASCII.GetBytes("##HIDDEN##");
        byte[] lenBytes = BitConverter.GetBytes(data.Length);

        using (MemoryStream ms = new MemoryStream())
        {
            // Данные чанка: marker + length + data
            ms.Write(marker, 0, marker.Length);
            ms.Write(lenBytes, 0, lenBytes.Length);
            ms.Write(data, 0, data.Length);

            byte[] chunkData = ms.ToArray();

            // Создаем полный чанк
            using (MemoryStream chunk = new MemoryStream())
            {
                // Длина
                WriteUInt32BE(chunk, (uint)chunkData.Length);

                // Тип
                chunk.Write(Encoding.ASCII.GetBytes("meta"), 0, 4);

                // Данные
                chunk.Write(chunkData, 0, chunkData.Length);

                // CRC (вычисляем от типа + данных)
                byte[] crcData = new byte[4 + chunkData.Length];
                Array.Copy(Encoding.ASCII.GetBytes("meta"), 0, crcData, 0, 4);
                Array.Copy(chunkData, 0, crcData, 4, chunkData.Length);
                uint crc = CalculateCRC32(crcData);

                WriteUInt32BE(chunk, crc);

                return chunk.ToArray();
            }
        }
    }

    private byte[] CreateXMPChunk(string text)
    {
        // Создаем XMP данные в правильном формате
        string xmp = CreateXMPString(text);

        // Формируем данные iTXt чанка
        using (MemoryStream dataStream = new MemoryStream())
        {
            // 1. Keyword (XML:com.adobe.xmp)
            byte[] keywordBytes = Encoding.ASCII.GetBytes(XMP_KEYWORD);
            dataStream.Write(keywordBytes, 0, keywordBytes.Length);
            dataStream.WriteByte(0); // null terminator

            // 2. Compression flag (0 = без компрессии)
            dataStream.WriteByte(0);

            // 3. Compression method (0)
            dataStream.WriteByte(0);

            // 4. Language (пусто)
            dataStream.WriteByte(0);

            // 5. Translated keyword (пусто)
            dataStream.WriteByte(0);

            // 6. XMP данные
            byte[] xmpBytes = Encoding.UTF8.GetBytes(xmp);
            dataStream.Write(xmpBytes, 0, xmpBytes.Length);

            byte[] chunkData = dataStream.ToArray();

            // Создаем полный чанк iTXt
            using (MemoryStream chunk = new MemoryStream())
            {
                // Длина
                WriteUInt32BE(chunk, (uint)chunkData.Length);

                // Тип
                chunk.Write(Encoding.ASCII.GetBytes("iTXt"), 0, 4);

                // Данные
                chunk.Write(chunkData, 0, chunkData.Length);

                // CRC (вычисляем от типа + данных)
                byte[] crcData = new byte[4 + chunkData.Length];
                Array.Copy(Encoding.ASCII.GetBytes("iTXt"), 0, crcData, 0, 4);
                Array.Copy(chunkData, 0, crcData, 4, chunkData.Length);
                uint crc = CalculateCRC32(crcData);

                WriteUInt32BE(chunk, crc);

                return chunk.ToArray();
            }
        }
    }

    private string CreateXMPString(string text)
    {
        // Создаем XMP в правильном формате для Windows
        return $@"<?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x='adobe:ns:meta/'>
  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
    <rdf:Description rdf:about='' xmlns:dc='http://purl.org/dc/elements/1.1/'>
      <dc:title>{text}</dc:title>
      <dc:description>{text}</dc:description>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end='w'?>";
    }

    private int FindIEND(byte[] data)
    {
        if (data.Length < 12) return -1;

        // Ищем IEND чанк: 0x00 0x00 0x00 0x00 0x49 0x45 0x4E 0x44 0xAE 0x42 0x60 0x82
        for (int i = data.Length - 12; i >= 0; i--)
        {
            if (i + 11 >= data.Length) continue;

            if (data[i] == 0x00 && data[i + 1] == 0x00 &&
                data[i + 2] == 0x00 && data[i + 3] == 0x00 &&
                data[i + 4] == 0x49 && data[i + 5] == 0x45 &&
                data[i + 6] == 0x4E && data[i + 7] == 0x44 &&
                data[i + 8] == 0xAE && data[i + 9] == 0x42 &&
                data[i + 10] == 0x60 && data[i + 11] == 0x82)
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

    public bool HasHiddenData(string imagePath) => ExtractData(imagePath).success;
    public string GetAllExifFields(string imagePath) => ExtractData(imagePath).data ?? "Данные не найдены";

    private string GetOutputPath(string imagePath, string suffix) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                     Path.GetFileNameWithoutExtension(imagePath) + suffix);
}