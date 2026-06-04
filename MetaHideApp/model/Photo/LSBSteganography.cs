using System;
using System.Collections.Generic;
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

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
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

                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
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
                return (true, $"Данные скрыты через LSB ({data.Length} символов)", outputPath);
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

                    // Читаем все байты подряд в список
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

                    // Первые 4 байта - длина
                    int dataLen = BitConverter.ToInt32(readBytes.GetRange(0, 4).ToArray(), 0);
                    if (dataLen <= 0 || dataLen > readBytes.Count - 4)
                        return (false, "Некорректная длина", null);

                    // Извлекаем ровно dataLen байт
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