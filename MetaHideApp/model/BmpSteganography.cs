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

                if (_hiddenMode)
                {
                    // Скрытый режим: добавляем данные в конец файла
                    // Формат: [данные] + [4 байта длины]
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(imageBytes, 0, imageBytes.Length);
                        ms.Write(dataBytes, 0, dataBytes.Length);
                        ms.Write(lenBytes, 0, 4);
                        File.WriteAllBytes(outputPath, ms.ToArray());
                    }
                    return (true, $"Данные скрыты в BMP через маркер в конец ({data.Length} символов)", outputPath);
                }
                else
                {
                    // LSB режим
                    using (Bitmap original = new Bitmap(imagePath))
                    {
                        using (Bitmap workBmp = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(workBmp))
                            {
                                g.DrawImage(original, 0, 0, original.Width, original.Height);
                            }

                            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                            byte[] allBytes = new byte[4 + dataBytes.Length];
                            byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
                            Array.Copy(lenBytes, 0, allBytes, 0, 4);
                            Array.Copy(dataBytes, 0, allBytes, 4, dataBytes.Length);

                            Rectangle rect = new Rectangle(0, 0, workBmp.Width, workBmp.Height);
                            BitmapData bmpData = workBmp.LockBits(rect, ImageLockMode.ReadWrite, workBmp.PixelFormat);
                            int stride = bmpData.Stride;
                            int totalBytesImg = stride * workBmp.Height;
                            byte[] pixels = new byte[totalBytesImg];
                            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, totalBytesImg);

                            int totalBits = allBytes.Length * 8;
                            int bitPos = 0;

                            for (int i = 0; i < totalBytesImg && bitPos < totalBits; i++)
                            {
                                int byteIdx = bitPos / 8;
                                int bitIdx = bitPos % 8;
                                int dataBit = (allBytes[byteIdx] >> bitIdx) & 1;
                                pixels[i] = (byte)((pixels[i] & 0xFE) | dataBit);
                                bitPos++;
                            }

                            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, totalBytesImg);
                            workBmp.UnlockBits(bmpData);
                            workBmp.Save(outputPath, ImageFormat.Bmp);
                        }
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
                    if (imageBytes.Length < 5)
                        return (false, "Файл слишком мал", null);

                    // Длина записана в последних 4 байтах
                    int dataLen = BitConverter.ToInt32(imageBytes, imageBytes.Length - 4);
                    if (dataLen <= 0 || dataLen > 10_000_000)
                        return (false, $"Некорректная длина: {dataLen}", null);

                    int startPos = imageBytes.Length - 4 - dataLen;
                    if (startPos < 0)
                        return (false, "Данные повреждены", null);

                    byte[] dataBytes = new byte[dataLen];
                    Array.Copy(imageBytes, startPos, dataBytes, 0, dataLen);
                    string text = Encoding.UTF8.GetString(dataBytes);
                    return (true, $"Извлечено {dataLen} байт", text);
                }
                else
                {
                    // LSB извлечение
                    using (Bitmap original = new Bitmap(imagePath))
                    {
                        using (Bitmap workBmp = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(workBmp))
                            {
                                g.DrawImage(original, 0, 0, original.Width, original.Height);
                            }

                            Rectangle rect = new Rectangle(0, 0, workBmp.Width, workBmp.Height);
                            BitmapData bmpData = workBmp.LockBits(rect, ImageLockMode.ReadOnly, workBmp.PixelFormat);
                            int stride = bmpData.Stride;
                            int totalBytesImg = stride * workBmp.Height;
                            byte[] pixels = new byte[totalBytesImg];
                            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, totalBytesImg);
                            workBmp.UnlockBits(bmpData);

                            List<byte> readBytes = new List<byte>();
                            int bitPos = 0;
                            byte currentByte = 0;

                            for (int i = 0; i < totalBytesImg; i++)
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