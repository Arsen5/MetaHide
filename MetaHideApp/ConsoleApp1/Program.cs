using System;
using System.IO;
using System.Text;

namespace ExifFieldCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== СОЗДАНИЕ СВОЕГО EXIF ПОЛЯ ===");
            Console.WriteLine("Введите полный путь к JPG файлу:");
            string imagePath = Console.ReadLine();

            if (!File.Exists(imagePath))
            {
                Console.WriteLine("Файл не найден!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Введите текст для скрытия:");
            string secretText = Console.ReadLine();

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outputPath = Path.Combine(desktopPath, "output_with_custom_field.jpg");

            CreateCustomExifField(imagePath, outputPath, 0x8888, secretText);

            Console.WriteLine($"\nФайл сохранён: {outputPath}");
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void CreateCustomExifField(string sourcePath, string destPath, int fieldId, string data)
        {
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Создаём EXIF блок с нашим полем
            byte[] exifBlock = CreateExifBlockWithCustomField(fieldId, dataBytes);

            // Находим маркер конца изображения FF D9
            int endMarkerPos = FindEndMarker(sourceBytes);

            using (MemoryStream ms = new MemoryStream())
            {
                // Пишем всё до FF D9
                ms.Write(sourceBytes, 0, endMarkerPos);

                // Вставляем наш EXIF блок перед FF D9
                ms.Write(exifBlock, 0, exifBlock.Length);

                // Пишем FF D9 и остальное
                ms.Write(sourceBytes, endMarkerPos, sourceBytes.Length - endMarkerPos);

                File.WriteAllBytes(destPath, ms.ToArray());
            }
        }

        static byte[] CreateExifBlockWithCustomField(int fieldId, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // APP1 маркер + длина (пока 0, заполним позже)
                ms.WriteByte(0xFF);
                ms.WriteByte(0xE1);
                ms.WriteByte(0x00);
                ms.WriteByte(0x00); // временно

                // "Exif\0\0" преамбула
                byte[] exifPreamble = { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 };
                ms.Write(exifPreamble, 0, exifPreamble.Length);

                // TIFF заголовок (little-endian)
                ms.WriteByte(0x49); // II
                ms.WriteByte(0x49);
                ms.WriteByte(0x2A);
                ms.WriteByte(0x00);
                ms.WriteByte(0x08); // offset to IFD0
                ms.WriteByte(0x00);
                ms.WriteByte(0x00);
                ms.WriteByte(0x00);

                // Количество тегов = 1
                ms.WriteByte(0x01);
                ms.WriteByte(0x00);

                // Наш тег (ID)
                WriteUInt16(ms, (ushort)fieldId);

                // Тип = ASCII (2)
                WriteUInt16(ms, 2);

                // Количество байт
                WriteUInt32(ms, (uint)data.Length);

                // Смещение к данным (пока временно)
                long dataOffsetPos = ms.Position;
                WriteUInt32(ms, 0);

                // Записываем данные в конец блока
                long dataStart = ms.Position;
                ms.Write(data, 0, data.Length);

                // Возвращаемся и пишем правильное смещение
                ms.Seek(dataOffsetPos, SeekOrigin.Begin);
                WriteUInt32(ms, (uint)dataStart);

                // Конец IFD (next IFD offset = 0)
                ms.Seek(0, SeekOrigin.End);
                WriteUInt32(ms, 0);

                // Получаем готовый блок
                byte[] fullBlock = ms.ToArray();

                // Исправляем длину в заголовке APP1
                int blockLength = fullBlock.Length - 4; // минус FF E1 + длина
                fullBlock[2] = (byte)((blockLength >> 8) & 0xFF);
                fullBlock[3] = (byte)(blockLength & 0xFF);

                return fullBlock;
            }
        }

        static int FindEndMarker(byte[] data)
        {
            // Ищем маркер конца FF D9
            for (int i = data.Length - 2; i >= 0; i--)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD9)
                    return i;
            }
            return data.Length; // если не нашли
        }

        static void WriteUInt16(MemoryStream ms, ushort value)
        {
            ms.WriteByte((byte)(value & 0xFF));
            ms.WriteByte((byte)((value >> 8) & 0xFF));
        }

        static void WriteUInt32(MemoryStream ms, uint value)
        {
            ms.WriteByte((byte)(value & 0xFF));
            ms.WriteByte((byte)((value >> 8) & 0xFF));
            ms.WriteByte((byte)((value >> 16) & 0xFF));
            ms.WriteByte((byte)((value >> 24) & 0xFF));
        }
    }
}