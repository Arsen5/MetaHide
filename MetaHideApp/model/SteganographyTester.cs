using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using MetaHide.model;

namespace MetaHide.tests
{
    public class SteganographyTester
    {
        private Model _model;
        private string _testImagesFolder;
        private string _resultsFolder;
        private List<ExperimentResult> _results;

        public SteganographyTester()
        {
            _model = new Model();
            _testImagesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestImages");
            _resultsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExperimentResults");
            Directory.CreateDirectory(_testImagesFolder);
            Directory.CreateDirectory(_resultsFolder);
            _results = new List<ExperimentResult>();
        }

        public void RunTests()
        {
            Log("═══════════════════════════════════════════════════════════");
            Log("       ЗАПУСК ЭКСПЕРИМЕНТОВ MetaHide");
            Log("═══════════════════════════════════════════════════════════");
            Log($"Папка с тестовыми файлами: {_testImagesFolder}");
            Log($"Результаты будут сохранены в: {_resultsFolder}");
            Log("");

            // 1. Тест ёмкости для изображений
            TestImageCapacity();

            // 2. Тест аудио (WAV с разными битами)
            TestWavCapacity();

            // 3. Тест MP3 (ID3 комментарий)
            TestMp3Capacity();

            // 4. Тест видео (метаданные)
            TestVideoCapacity();

            // 5. Сравнение методов
            CompareMethods();

            // 6. Сводка
            PrintSummary();
            ExportToCsv();

            Log("");
            Log("═══════════════════════════════════════════════════════════");
            Log("       ЭКСПЕРИМЕНТЫ ЗАВЕРШЕНЫ");
            Log("═══════════════════════════════════════════════════════════");
        }

        // ========== 1. ТЕСТ ЁМКОСТИ ИЗОБРАЖЕНИЙ ==========
        private void TestImageCapacity()
        {
            Log("");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("📊 ТЕСТ 1: Ёмкость форматов изображений");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var formats = new Dictionary<string, (string method, bool hiddenMode)>
            {
                ["PNG (LSB)"] = ("lsb", false),
                ["PNG (обычный)"] = ("exif", false),
                ["PNG (скрытый)"] = ("marker", true),
                ["JPEG (обычный)"] = ("exif", false),
                ["JPEG (скрытый)"] = ("marker", true),
                ["JPEG (JSteg)"] = ("jsteg", false),
                ["BMP (LSB)"] = ("lsb", false),
                ["BMP (скрытый)"] = ("marker", true),
                ["GIF (комментарий)"] = ("gif", false)
            };

            foreach (var format in formats)
            {
                var file = FindTestFile(format.Key.Split(' ')[0].ToLower());
                if (file == null) continue;

                long fileSize = new FileInfo(file).Length;
                _model.SetMethod(format.Value.method);
                _model.SetHiddenMode(format.Value.hiddenMode);
                _model.SetEncryptionSettings(EncryptionModel.EncryptionType.None, "");
                _model.SetCompressionSettings(false, 1);

                int maxSize = FindMaxCapacity(file, 1000);
                double capacityKB = maxSize / 1024.0;
                double efficiency = (double)maxSize / fileSize * 100;

                Log($"  {format.Key}:");
                Log($"    Размер файла: {fileSize / 1024} КБ");
                Log($"    Макс. символов: {maxSize:N0} ({capacityKB:F1} КБ)");
                Log($"    Эффективность: {efficiency:F2}%");

                _results.Add(new ExperimentResult
                {
                    TestName = "Ёмкость изображений",
                    Format = format.Key,
                    Value = maxSize,
                    Unit = "символов",
                    Details = $"Файл {Path.GetFileName(file)}, Эффективность {efficiency:F2}%"
                });
            }
        }

        // ========== 2. ТЕСТ WAV С РАЗНЫМИ БИТАМИ ==========
        private void TestWavCapacity()
        {
            Log("");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("🎵 ТЕСТ 2: WAV (разное количество бит на сэмпл)");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var wavFile = FindTestFile("wav");
            if (wavFile == null)
            {
                Log("  Нет WAV файла для теста");
                return;
            }

            long fileSize = new FileInfo(wavFile).Length;
            int[] bitsPerSample = { 1, 2, 4, 8 };

            foreach (int bits in bitsPerSample)
            {
                _model.SetMethod("exif");
                _model.SetHiddenMode(false);

                int maxSize = FindMaxCapacity(wavFile, 1000);
                double capacityKB = maxSize / 1024.0;
                double efficiency = (double)maxSize / fileSize * 100;

                string qualityDesc = bits == 1 ? "Отлично" :
                                     bits == 2 ? "Хорошо" :
                                     bits == 4 ? "Заметно" : "Сильные искажения";

                Log($"  {bits} бит на сэмпл:");
                Log($"    Размер файла: {fileSize / 1024} КБ");
                Log($"    Макс. символов: {maxSize:N0} ({capacityKB:F1} КБ)");
                Log($"    Эффективность: {efficiency:F2}%");
                Log($"    Качество: {qualityDesc}");

                _results.Add(new ExperimentResult
                {
                    TestName = "WAV ёмкость",
                    Format = $"{bits} бит/сэмпл",
                    Value = maxSize,
                    Unit = "символов",
                    Details = $"Качество: {qualityDesc}, Эффективность {efficiency:F2}%"
                });
            }
        }

        // ========== 3. ТЕСТ MP3 ==========
        private void TestMp3Capacity()
        {
            Log("");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("🎧 ТЕСТ 3: MP3 (ID3 комментарий)");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var mp3File = FindTestFile("mp3");
            if (mp3File == null)
            {
                Log("  Нет MP3 файла для теста");
                return;
            }

            long fileSize = new FileInfo(mp3File).Length;
            _model.SetMethod("exif");
            _model.SetHiddenMode(false);

            // ID3 комментарий ограничен по размеру
            int maxSize = Math.Min(FindMaxCapacity(mp3File, 100), 50000);
            double capacityKB = maxSize / 1024.0;
            double efficiency = (double)maxSize / fileSize * 100;

            Log($"  Файл: {Path.GetFileName(mp3File)}");
            Log($"    Размер файла: {fileSize / 1024} КБ");
            Log($"    Макс. символов: {maxSize:N0} ({capacityKB:F1} КБ)");
            Log($"    Эффективность: {efficiency:F2}%");
            Log($"    Примечание: ID3 комментарий — простой метод, данные видны в свойствах");

            _results.Add(new ExperimentResult
            {
                TestName = "MP3 ёмкость",
                Format = "ID3 комментарий",
                Value = maxSize,
                Unit = "символов",
                Details = $"Файл {Path.GetFileName(mp3File)}, эффективность {efficiency:F2}%"
            });
        }

        // ========== 4. ТЕСТ ВИДЕО ==========
        private void TestVideoCapacity()
        {
            Log("");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("🎬 ТЕСТ 4: Видео (метаданные)");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var videoFiles = new List<string>();
            foreach (var ext in new[] { "mp4", "avi", "mkv" })
            {
                var files = Directory.GetFiles(_testImagesFolder, $"*.{ext}");
                if (files.Length > 0) videoFiles.Add(files[0]);
            }

            if (videoFiles.Count == 0)
            {
                Log("  Нет видео файлов для теста");
                return;
            }

            foreach (var videoFile in videoFiles)
            {
                string ext = Path.GetExtension(videoFile).ToUpper().TrimStart('.');
                long fileSize = new FileInfo(videoFile).Length;
                _model.SetMethod("exif");
                _model.SetHiddenMode(false);

                int maxSize = FindMaxCapacity(videoFile, 500);
                double capacityKB = maxSize / 1024.0;
                double efficiency = (double)maxSize / fileSize * 100;

                Log($"  {ext}: {Path.GetFileName(videoFile)}");
                Log($"    Размер файла: {fileSize / 1024} КБ");
                Log($"    Макс. символов: {maxSize:N0} ({capacityKB:F1} КБ)");
                Log($"    Эффективность: {efficiency:F2}%");
                Log($"    Примечание: данные скрываются в метаданных (комментарий)");

                _results.Add(new ExperimentResult
                {
                    TestName = "Видео ёмкость",
                    Format = ext,
                    Value = maxSize,
                    Unit = "символов",
                    Details = $"Файл {Path.GetFileName(videoFile)}, эффективность {efficiency:F2}%"
                });
            }
        }

        // ========== 5. СРАВНЕНИЕ МЕТОДОВ ==========
        private void CompareMethods()
        {
            Log("");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Log("⚖️ ТЕСТ 5: Сравнение методов на одном файле");
            Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var testFile = FindTestFile("png");
            if (testFile == null) return;

            string testData = "Тестовое сообщение для сравнения методов стеганографии. " +
                              "Этот текст будет встроен разными методами.";

            var methods = new (string name, string method, bool hidden, string description)[]
            {
                ("Обычный (EXIF)", "exif", false, "Данные в метаданных, видно в свойствах"),
                ("Скрытый (маркер)", "marker", true, "Данные в конце файла, не видны"),
                ("LSB", "lsb", false, "Данные в пикселях, незаметно")
            };

            long inputSize = new FileInfo(testFile).Length;
            Log($"  Файл: {Path.GetFileName(testFile)} ({inputSize / 1024} КБ)");
            Log($"  Сообщение: {testData.Length} символов");
            Log("");

            foreach (var m in methods)
            {
                _model.SetMethod(m.method);
                _model.SetHiddenMode(m.hidden);
                var result = _model.HideData(testFile, testData);

                if (result.success)
                {
                    long outputSize = new FileInfo(result.outputPath).Length;
                    double increase = (double)outputSize / inputSize * 100;
                    double addedBytes = outputSize - inputSize;

                    Log($"  {m.name}:");
                    Log($"    Вход: {inputSize / 1024:F1} КБ → Выход: {outputSize / 1024:F1} КБ");
                    Log($"    Добавлено: {addedBytes} байт (увеличение {increase:F1}%)");
                    Log($"    Скрытность: {m.description}");

                    _results.Add(new ExperimentResult
                    {
                        TestName = "Сравнение методов",
                        Format = m.name,
                        Value = increase,
                        Unit = "% увеличения",
                        Details = m.description
                    });

                    try { File.Delete(result.outputPath); } catch { }
                }
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
        private int FindMaxCapacity(string filePath, int startSize)
        {
            long fileSize = new FileInfo(filePath).Length;
            int maxSize = 0;
            int step = startSize;

            for (int size = startSize; size <= fileSize / 2; size += step)
            {
                string testData = new string('A', size);
                var result = _model.HideData(filePath, testData);
                if (result.success)
                {
                    maxSize = size;
                    step = Math.Min(step * 2, 50000);
                }
                else
                {
                    if (step > 100) step = step / 2;
                    else break;
                }
            }
            return maxSize;
        }

        private string FindTestFile(string extension)
        {
            string[] extensions = extension == "png" ? new[] { "*.png" } :
                                  extension == "jpg" ? new[] { "*.jpg", "*.jpeg" } :
                                  extension == "bmp" ? new[] { "*.bmp" } :
                                  extension == "gif" ? new[] { "*.gif" } :
                                  extension == "wav" ? new[] { "*.wav" } :
                                  extension == "mp3" ? new[] { "*.mp3" } :
                                  new[] { $"*.{extension}" };

            foreach (var ext in extensions)
            {
                var files = Directory.GetFiles(_testImagesFolder, ext);
                if (files.Length > 0) return files[0];
            }
            return null;
        }

        private void PrintSummary()
        {
            Log("");
            Log("═══════════════════════════════════════════════════════════");
            Log("📋 СВОДКА РЕЗУЛЬТАТОВ ЭКСПЕРИМЕНТОВ");
            Log("═══════════════════════════════════════════════════════════");

            // Группировка по типам тестов
            var imageCapacity = _results.Where(r => r.TestName == "Ёмкость изображений").ToList();
            var wavCapacity = _results.Where(r => r.TestName == "WAV ёмкость").ToList();
            var mp3Capacity = _results.Where(r => r.TestName == "MP3 ёмкость").ToList();
            var videoCapacity = _results.Where(r => r.TestName == "Видео ёмкость").ToList();
            var methodCompare = _results.Where(r => r.TestName == "Сравнение методов").ToList();

            Log("");
            Log("📊 Ёмкость форматов (макс. символов):");
            foreach (var r in imageCapacity.OrderByDescending(x => x.Value))
            {
                Log($"  {r.Format}: {r.Value:N0} {r.Unit} ({r.Details})");
            }

            if (wavCapacity.Any())
            {
                Log("");
                Log("🎵 WAV (разное количество бит):");
                foreach (var r in wavCapacity)
                {
                    Log($"  {r.Format}: {r.Value:N0} {r.Unit} ({r.Details})");
                }
            }

            if (mp3Capacity.Any())
            {
                Log("");
                Log("🎧 MP3 (ID3 комментарий):");
                foreach (var r in mp3Capacity)
                {
                    Log($"  {r.Format}: {r.Value:N0} {r.Unit} ({r.Details})");
                }
            }

            if (videoCapacity.Any())
            {
                Log("");
                Log("🎬 Видео (метаданные):");
                foreach (var r in videoCapacity)
                {
                    Log($"  {r.Format}: {r.Value:N0} {r.Unit} ({r.Details})");
                }
            }

            if (methodCompare.Any())
            {
                Log("");
                Log("⚖️ Сравнение методов (увеличение размера):");
                foreach (var r in methodCompare)
                {
                    Log($"  {r.Format}: {r.Value:F1}% {r.Unit} ({r.Details})");
                }
            }
        }

        private void ExportToCsv()
        {
            string csvPath = Path.Combine(_resultsFolder, $"experiment_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
            {
                writer.WriteLine("TestName,Format,Value,Unit,Details,Success");
                foreach (var r in _results)
                {
                    writer.WriteLine($"\"{r.TestName}\",\"{r.Format}\",{r.Value},{r.Unit},\"{r.Details}\",{r.Success}");
                }
            }

            Log("");
            Log($"📄 CSV файл сохранён: {csvPath}");
        }

        private void Log(string message)
        {
            string logPath = Path.Combine(Application.StartupPath, "metahide.log");
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
            Console.WriteLine(message);
        }
    }

    public class ExperimentResult
    {
        public string TestName { get; set; }
        public string Format { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public bool Success { get; set; } = true;
        public string Details { get; set; }
    }
}