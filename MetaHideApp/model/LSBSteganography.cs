using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model
{
    public class LSBSteganography : ISteganography
    {
        private bool _hiddenMode = false;

        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;

        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".png" || ext == ".bmp";
        }

        // Метод для проверки максимальной ёмкости изображения в байтах
        private int GetMaxCapacity(string imagePath)
        {
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                // 24-битное RGB: ширина * высота * 3 канала / 8 бит = макс байт
                // Вычитаем 4 байта для заголовка длины
                return (bmp.Width * bmp.Height * 3) / 8 - 4;
            }
        }

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
                // Проверяем ёмкость перед записью
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                int maxCapacity = GetMaxCapacity(imagePath);

                bool isCapacityWarning = false;
                if (dataBytes.Length > maxCapacity)
                {
                    isCapacityWarning = true;
                    // Продолжаем, но с предупреждением
                }

                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.GetFileNameWithoutExtension(imagePath) + "_hidden.png");

                using (Bitmap bmp = new Bitmap(imagePath))
                {
                    Bitmap rgbBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(rgbBmp))
                    {
                        g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                    }

                    byte[] allBytes = new byte[4 + dataBytes.Length];
                    byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
                    Array.Copy(lenBytes, 0, allBytes, 0, 4);
                    Array.Copy(dataBytes, 0, allBytes, 4, dataBytes.Length);

                    Rectangle rect = new Rectangle(0, 0, rgbBmp.Width, rgbBmp.Height);
                    BitmapData bmpData = rgbBmp.LockBits(rect, ImageLockMode.ReadWrite, rgbBmp.PixelFormat);
                    int stride = bmpData.Stride;
                    int totalBytes = stride * rgbBmp.Height;
                    byte[] pixels = new byte[totalBytes];
                    System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, totalBytes);

                    int totalBits = allBytes.Length * 8;
                    int bitPos = 0;

                    for (int i = 0; i < totalBytes && bitPos < totalBits; i++)
                    {
                        int byteIdx = bitPos / 8;
                        int bitIdx = bitPos % 8;
                        int dataBit = (allBytes[byteIdx] >> bitIdx) & 1;
                        pixels[i] = (byte)((pixels[i] & 0xFE) | dataBit);
                        bitPos++;
                    }

                    System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, totalBytes);
                    rgbBmp.UnlockBits(bmpData);
                    rgbBmp.Save(outputPath, ImageFormat.Png);
                }

                string warningMessage = "";
                if (isCapacityWarning)
                {
                    warningMessage = $" (ВНИМАНИЕ: изображение слишком маленькое! Макс. размер: {maxCapacity} байт, требуется: {dataBytes.Length} байт. Возможны помехи при извлечении.)";
                }

                return (true, $"Данные скрыты через LSB ({data.Length} символов){warningMessage}", outputPath);
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
                using (Bitmap bmp = new Bitmap(imagePath))
                {
                    Bitmap rgbBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(rgbBmp))
                    {
                        g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                    }

                    Rectangle rect = new Rectangle(0, 0, rgbBmp.Width, rgbBmp.Height);
                    BitmapData bmpData = rgbBmp.LockBits(rect, ImageLockMode.ReadOnly, rgbBmp.PixelFormat);
                    int stride = bmpData.Stride;
                    int totalBytes = stride * rgbBmp.Height;
                    byte[] pixels = new byte[totalBytes];
                    System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, totalBytes);
                    rgbBmp.UnlockBits(bmpData);

                    // Читаем все байты подряд
                    List<byte> readBytes = new List<byte>();
                    int bitPos = 0;
                    byte currentByte = 0;
                    for (int i = 0; i < totalBytes; i++)
                    {
                        int bit = pixels[i] & 1;
                        currentByte = (byte)(currentByte | (bit << bitPos));
                        bitPos++;
                        if (bitPos == 8)
                        {
                            readBytes.Add(currentByte);
                            bitPos = 0;
                            currentByte = 0;
                        }
                    }

                    if (readBytes.Count < 4)
                        return (false, "Недостаточно данных", null);

                    int dataLen = BitConverter.ToInt32(readBytes.GetRange(0, 4).ToArray(), 0);
                    if (dataLen <= 0 || dataLen > 10000000)
                        return (false, "Некорректная длина", null);

                    // Проверяем, хватает ли данных
                    if (readBytes.Count < 4 + dataLen)
                    {
                        // Данные неполные — извлекаем то, что есть
                        int availableBytes = readBytes.Count - 4;
                        if (availableBytes > 0)
                        {
                            byte[] partialData = readBytes.GetRange(4, availableBytes).ToArray();
                            string partialText = Encoding.UTF8.GetString(partialData);
                            return (true, $"Извлечено частично ({availableBytes} из {dataLen} байт)", partialText);
                        }
                        return (false, "Недостаточно данных для извлечения", null);
                    }

                    byte[] dataBytes = readBytes.GetRange(4, dataLen).ToArray();
                    string text = Encoding.UTF8.GetString(dataBytes);
                    return (true, $"Извлечено {dataLen} байт", text);
                }
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