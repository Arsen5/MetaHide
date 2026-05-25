using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using test;

namespace MetaHide.model;

public class JpegSteganography : ISteganography
{
    private bool _hiddenMode = false;
    private const string VISIBLE_MARKER = "##VISIBLE##";
    private const string HIDDEN_MARKER = "##HIDDEN##";

    public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
    public int GetCurrentFieldId() => 0;
    public bool SupportsFormat(string filePath) =>
        filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

    // ========================== ЗАПИСЬ ==========================
    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        try
        {
            if (!File.Exists(imagePath)) return (false, "Файл не найден", null);
            if (string.IsNullOrEmpty(data)) return (false, "Текст не может быть пустым", null);

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            string outputPath = GetOutputPath(imagePath, "_hidden.jpg");

            byte[] originalBytes = File.ReadAllBytes(imagePath);
            int eoiPos = FindEOI(originalBytes);
            if (eoiPos == -1)
                return (false, "Не найден конец изображения (EOI)", null);

            string marker = _hiddenMode ? HIDDEN_MARKER : VISIBLE_MARKER;
            byte[] markerBytes = Encoding.ASCII.GetBytes(marker);
            byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(originalBytes, 0, eoiPos);
                ms.Write(markerBytes, 0, markerBytes.Length);
                ms.Write(lengthBytes, 0, lengthBytes.Length);
                ms.Write(dataBytes, 0, dataBytes.Length);
                ms.Write(originalBytes, eoiPos, originalBytes.Length - eoiPos);
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

            byte[] fileBytes = File.ReadAllBytes(imagePath);
            List<string> allMessages = new List<string>();

            // Ищем видимый маркер
            byte[] visibleMarker = Encoding.ASCII.GetBytes(VISIBLE_MARKER);
            string visibleText = FindMarkerData(fileBytes, visibleMarker);
            if (!string.IsNullOrEmpty(visibleText))
                allMessages.Add(visibleText);

            // Ищем скрытый маркер
            byte[] hiddenMarker = Encoding.ASCII.GetBytes(HIDDEN_MARKER);
            string hiddenText = FindMarkerData(fileBytes, hiddenMarker);
            if (!string.IsNullOrEmpty(hiddenText))
                allMessages.Add(hiddenText);

            if (allMessages.Count > 0)
                return (true, "Данные найдены", string.Join(Environment.NewLine, allMessages));
            else
                return (false, "Данные не найдены", null);
        }
        catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
    }

    private string FindMarkerData(byte[] data, byte[] marker)
    {
        for (int i = data.Length - marker.Length; i >= 0; i--)
        {
            bool found = true;
            for (int j = 0; j < marker.Length; j++)
                if (data[i + j] != marker[j]) { found = false; break; }

            if (found)
            {
                int dataStart = i + marker.Length;
                if (dataStart + 4 > data.Length) break;
                int dataLen = BitConverter.ToInt32(data, dataStart);
                if (dataStart + 4 + dataLen > data.Length) break;
                byte[] extracted = new byte[dataLen];
                Array.Copy(data, dataStart + 4, extracted, 0, dataLen);
                return Encoding.UTF8.GetString(extracted);
            }
        }
        return null;
    }

    public bool HasHiddenData(string imagePath) => ExtractData(imagePath).success;
    public string GetAllExifFields(string imagePath) => ExtractData(imagePath).data ?? "Данные не найдены";

    private string GetOutputPath(string imagePath, string suffix) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                     Path.GetFileNameWithoutExtension(imagePath) + suffix);

    private int FindEOI(byte[] data)
    {
        for (int i = data.Length - 2; i >= 0; i--)
            if (data[i] == 0xFF && data[i + 1] == 0xD9)
                return i;
        return -1;
    }
}