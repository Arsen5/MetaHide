using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model
{
    public class WavSteganography : ISteganography
    {
        private bool _hiddenMode = false;
        private int _bitsPerSample = 1; // 1-8 бит на сэмпл

        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;
        public void SetBitsPerSample(int bits) => _bitsPerSample = Math.Clamp(bits, 1, 8);

        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".wav";
        }

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.GetFileNameWithoutExtension(imagePath) + "_hidden.wav");

                byte[] wavBytes = File.ReadAllBytes(imagePath);

                // Парсим WAV заголовок
                int dataStart = 44;
                int bitsPerSample = 8;
                int bytesPerSample = 1;
                int numChannels = 1;

                // Читаем параметры WAV
                if (wavBytes.Length > 22)
                {
                    numChannels = BitConverter.ToInt16(wavBytes, 22);
                    bitsPerSample = BitConverter.ToInt16(wavBytes, 34);
                    bytesPerSample = bitsPerSample / 8;

                    // Ищем смещение данных (chunk "data")
                    dataStart = FindDataChunkOffset(wavBytes);
                }

                // Рассчитываем ёмкость
                int availableSamples = (wavBytes.Length - dataStart) / bytesPerSample;
                int maxBytes = (availableSamples * _bitsPerSample) / 8;

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] allBytes = new byte[4 + dataBytes.Length];
                byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
                Array.Copy(lenBytes, 0, allBytes, 0, 4);
                Array.Copy(dataBytes, 0, allBytes, 4, dataBytes.Length);

                int totalBits = allBytes.Length * 8;

                if (totalBits > availableSamples * _bitsPerSample)
                {
                    return (false, $"Файл слишком мал. Максимум: {maxBytes / 1024} КБ", null);
                }

                // Встраивание данных
                int bitPos = 0;
                int sampleIndex = 0;

                for (int i = dataStart; i < wavBytes.Length && bitPos < totalBits; i += bytesPerSample)
                {
                    int byteIdx = bitPos / 8;
                    int bitIdx = bitPos % 8;
                    int dataBit = (allBytes[byteIdx] >> bitIdx) & 1;

                    if (bytesPerSample == 2)
                    {
                        // 16-битный сэмпл
                        int sample = BitConverter.ToInt16(wavBytes, i);
                        sample = (sample & ~(1 << _bitsPerSample)) | (dataBit << _bitsPerSample);
                        byte[] sampleBytes = BitConverter.GetBytes((short)sample);
                        wavBytes[i] = sampleBytes[0];
                        wavBytes[i + 1] = sampleBytes[1];
                    }
                    else
                    {
                        // 8-битный сэмпл
                        wavBytes[i] = (byte)((wavBytes[i] & ~(1 << _bitsPerSample)) | (dataBit << _bitsPerSample));
                    }

                    bitPos++;
                    sampleIndex++;
                }

                File.WriteAllBytes(outputPath, wavBytes);
                return (true, $"Данные скрыты в WAV через LSB ({_bitsPerSample} бит/сэмпл, {data.Length} символов)", outputPath);
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
                byte[] wavBytes = File.ReadAllBytes(imagePath);

                // Парсим WAV заголовок
                int dataStart = 44;
                int bitsPerSample = 8;
                int bytesPerSample = 1;

                if (wavBytes.Length > 34)
                {
                    bitsPerSample = BitConverter.ToInt16(wavBytes, 34);
                    bytesPerSample = bitsPerSample / 8;
                    dataStart = FindDataChunkOffset(wavBytes);
                }

                List<byte> readBytes = new List<byte>();
                int bitPos = 0;
                byte currentByte = 0;

                for (int i = dataStart; i < wavBytes.Length; i += bytesPerSample)
                {
                    int bit = 0;

                    if (bytesPerSample == 2)
                    {
                        // 16-битный сэмпл
                        short sample = BitConverter.ToInt16(wavBytes, i);
                        bit = (sample >> _bitsPerSample) & 1;
                    }
                    else
                    {
                        // 8-битный сэмпл
                        bit = (wavBytes[i] >> _bitsPerSample) & 1;
                    }

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
                    return (false, "Некорректная длина", null);

                byte[] dataBytes = readBytes.GetRange(4, dataLen).ToArray();
                string text = Encoding.UTF8.GetString(dataBytes);
                return (true, $"Извлечено {dataLen} байт", text);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", null);
            }
        }

        private int FindDataChunkOffset(byte[] wavBytes)
        {
            // Ищем chunk "data" в WAV файле
            for (int i = 12; i < wavBytes.Length - 8; i++)
            {
                if (wavBytes[i] == 'd' && wavBytes[i + 1] == 'a' &&
                    wavBytes[i + 2] == 't' && wavBytes[i + 3] == 'a')
                {
                    return i + 8; // после заголовка chunk'а
                }
            }
            return 44; // стандартное смещение
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