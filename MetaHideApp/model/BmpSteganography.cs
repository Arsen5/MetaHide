using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model
{
    public class BmpSteganography : ISteganography
    {
        private bool _hiddenMode = false;

        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;

        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".bmp";
        }

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.GetFileNameWithoutExtension(imagePath) + "_hidden.bmp");

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] allBytes = new byte[4 + dataBytes.Length];
                byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
                Array.Copy(lenBytes, 0, allBytes, 0, 4);
                Array.Copy(dataBytes, 0, allBytes, 4, dataBytes.Length);

                if (_hiddenMode)
                {
                    // Скрытый режим: добавляем маркер и данные в конец
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(imageBytes, 0, imageBytes.Length);
                        ms.Write(allBytes, 0, allBytes.Length);
                        File.WriteAllBytes(outputPath, ms.ToArray());
                    }
                    return (true, $"Данные скрыты в BMP через маркер в конец ({data.Length} символов)", outputPath);
                }
                else
                {
                    // LSB режим
                    using (Bitmap bmp = new Bitmap(imagePath))
                    {
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
                        int stride = bmpData.Stride;
                        int totalBytes = stride * bmp.Height;
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
                        bmp.UnlockBits(bmpData);
                        bmp.Save(outputPath, ImageFormat.Bmp);
                    }
                    return (true, $"Данные скрыты в BMP через LSB ({data.Length} символов)", outputPath);
                }
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
                if (_hiddenMode)
                {
                    // Извлечение из скрытого режима (данные в конце)
                    byte[] imageBytes = File.ReadAllBytes(imagePath);

                    // Читаем последние 4 байта как длину
                    if (imageBytes.Length < 5) return (false, "Файл слишком маленький", null);

                    int dataLen = BitConverter.ToInt32(imageBytes, imageBytes.Length - 4);
                    if (dataLen <= 0 || dataLen > 1000000)
                        return (false, $"Некорректная длина: {dataLen}", null);

                    if (imageBytes.Length < 4 + dataLen)
                        return (false, "Недостаточно данных", null);

                    byte[] dataBytes = new byte[dataLen];
                    Array.Copy(imageBytes, imageBytes.Length - 4 - dataLen, dataBytes, 0, dataLen);
                    string text = Encoding.UTF8.GetString(dataBytes);
                    return (true, $"Извлечено {dataLen} байт", text);
                }
                else
                {
                    // LSB извлечение
                    using (Bitmap bmp = new Bitmap(imagePath))
                    {
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                        int stride = bmpData.Stride;
                        int totalBytes = stride * bmp.Height;
                        byte[] pixels = new byte[totalBytes];
                        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, totalBytes);
                        bmp.UnlockBits(bmpData);

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
                        if (dataLen <= 0 || dataLen > readBytes.Count - 4)
                            return (false, $"Некорректная длина: {dataLen}", null);

                        byte[] dataBytes = readBytes.GetRange(4, dataLen).ToArray();
                        string text = Encoding.UTF8.GetString(dataBytes);
                        return (true, $"Извлечено {dataLen} байт", text);
                    }
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