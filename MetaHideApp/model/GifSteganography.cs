using System;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model
{
    public class GifSteganography : ISteganography
    {
        private bool _hiddenMode = false;

        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;

        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".gif";
        }

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.GetFileNameWithoutExtension(imagePath) + "_hidden.gif");

                byte[] gifBytes = File.ReadAllBytes(imagePath);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] allBytes = new byte[4 + dataBytes.Length];
                byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
                Array.Copy(lenBytes, 0, allBytes, 0, 4);
                Array.Copy(dataBytes, 0, allBytes, 4, dataBytes.Length);

                // Находим конец GIF (маркер 0x3B)
                int endPos = -1;
                for (int i = gifBytes.Length - 1; i >= 0; i--)
                {
                    if (gifBytes[i] == 0x3B)
                    {
                        endPos = i;
                        break;
                    }
                }
                if (endPos == -1) return (false, "Неверный формат GIF (нет маркера конца)", null);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(gifBytes, 0, endPos);

                    // Блок комментария
                    ms.WriteByte(0x21); // Extension introducer
                    ms.WriteByte(0xFE); // Comment extension

                    int totalLen = allBytes.Length;
                    ms.WriteByte((byte)(totalLen & 0xFF));
                    ms.WriteByte((byte)((totalLen >> 8) & 0xFF));

                    ms.Write(allBytes, 0, allBytes.Length);
                    ms.WriteByte(0x00); // Терминатор блока
                    ms.WriteByte(0x3B); // Конец GIF

                    File.WriteAllBytes(outputPath, ms.ToArray());
                }
                return (true, $"Данные скрыты в GIF через комментарий ({data.Length} символов)", outputPath);
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
                byte[] gifBytes = File.ReadAllBytes(imagePath);

                for (int i = 0; i < gifBytes.Length - 6; i++)
                {
                    if (gifBytes[i] == 0x21 && gifBytes[i + 1] == 0xFE)
                    {
                        int dataLen = gifBytes[i + 2] | (gifBytes[i + 3] << 8);
                        if (dataLen <= 0 || dataLen > 1000000) continue;
                        if (i + 4 + dataLen > gifBytes.Length) continue;

                        byte[] extracted = new byte[dataLen];
                        Array.Copy(gifBytes, i + 4, extracted, 0, dataLen);

                        if (extracted.Length < 4) continue;

                        int msgLen = BitConverter.ToInt32(extracted, 0);
                        if (msgLen <= 0 || msgLen > extracted.Length - 4) continue;

                        byte[] msgBytes = new byte[msgLen];
                        Array.Copy(extracted, 4, msgBytes, 0, msgLen);
                        string text = Encoding.UTF8.GetString(msgBytes);
                        return (true, $"Извлечено {msgLen} байт", text);
                    }
                }
                return (false, "Данные не найдены", null);
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
            var result = ExtractData(imagePath);
            return result.data ?? "Данные не найдены";
        }
    }
}