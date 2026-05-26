using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MetaHide.model;

namespace MetaHide.tests
{
    public class SteganographyTester
    {
        private Model _model;
        private string _testImagesFolder;
        private List<TestResult> _results;

        public SteganographyTester()
        {
            _model = new Model();
            _testImagesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestImages");
            _results = new List<TestResult>();

            Directory.CreateDirectory(_testImagesFolder);
        }

        public void RunTests()
        {
            Log("=== ЗАПУСК ТЕСТОВ ===");
            Log($"Папка с изображениями: {_testImagesFolder}");

            var imageFiles = GetTestImages();
            if (imageFiles.Count == 0)
            {
                Log("Нет изображений в папке TestImages!");
                Log("Добавьте PNG и JPG файлы в папку TestImages на рабочем столе");
                return;
            }

            Log($"Найдено {imageFiles.Count} изображений");
            Log("");

            foreach (var imageFile in imageFiles)
            {
                TestImage(imageFile);
            }

            Log("=== ТЕСТЫ ЗАВЕРШЕНЫ ===");
            Log("");
            Log("Сводка по тестам:");
            foreach (var result in _results)
            {
                string status = result.Success ? "[OK]" : "[FAIL]";
                Log($"{status} {result.ImageName} - {result.Mode}: {result.ErrorMessage ?? result.Message}");
            }
        }

        private List<string> GetTestImages()
        {
            var images = new List<string>();
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.png"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.jpg"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.jpeg"));
            return images;
        }

        private void TestImage(string imagePath)
        {
            string fileName = Path.GetFileName(imagePath);
            Log($"Тестируем: {fileName} ({new FileInfo(imagePath).Length} байт)");

            if (!IsFileValid(imagePath))
            {
                Log($"  Файл не является валидным изображением, пропускаем");
                _results.Add(new TestResult
                {
                    ImageName = fileName,
                    Mode = "All",
                    Success = false,
                    ErrorMessage = "Файл не является валидным изображением"
                });
                return;
            }

            TestVisibleMode(imagePath, fileName);
            TestHiddenMode(imagePath, fileName);
            TestSequentialMode(imagePath, fileName);
            TestEncryptionMode(imagePath, fileName);

            Log("");
        }

        private void TestVisibleMode(string imagePath, string fileName)
        {
            Log("  Тест видимого режима...");

            try
            {
                _model.SetHiddenMode(false);
                string testData = $"Test_Visible_{DateTime.Now:HHmmss}";

                var hideResult = _model.HideData(imagePath, testData);
                if (!hideResult.success)
                {
                    Log($"    Ошибка записи: {hideResult.message}");
                    _results.Add(new TestResult
                    {
                        ImageName = fileName,
                        Mode = "Visible",
                        Success = false,
                        ErrorMessage = hideResult.message
                    });
                    return;
                }

                var extractResult = _model.ExtractData(hideResult.outputPath);
                bool success = extractResult.success && extractResult.data.Contains(testData);

                Log($"    Запись: {hideResult.success}, Извлечение: {extractResult.success}, Данные: {extractResult.data}");

                _results.Add(new TestResult
                {
                    ImageName = fileName,
                    Mode = "Visible",
                    Success = success,
                    Message = success ? "OK" : extractResult.message,
                    ExtractedData = extractResult.data
                });

                try { File.Delete(hideResult.outputPath); } catch { }
            }
            catch (Exception ex)
            {
                Log($"    Исключение: {ex.Message}");
                _results.Add(new TestResult
                {
                    ImageName = fileName,
                    Mode = "Visible",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private void TestHiddenMode(string imagePath, string fileName)
        {
            Log("  Тест скрытого режима...");

            try
            {
                _model.SetHiddenMode(true);
                string testData = $"Test_Hidden_{DateTime.Now:HHmmss}";

                var hideResult = _model.HideData(imagePath, testData);
                if (!hideResult.success)
                {
                    Log($"    Ошибка записи: {hideResult.message}");
                    _results.Add(new TestResult
                    {
                        ImageName = fileName,
                        Mode = "Hidden",
                        Success = false,
                        ErrorMessage = hideResult.message
                    });
                    return;
                }

                var extractResult = _model.ExtractData(hideResult.outputPath);
                bool success = extractResult.success && extractResult.data.Contains(testData);

                Log($"    Запись: {hideResult.success}, Извлечение: {extractResult.success}, Данные: {extractResult.data}");

                _results.Add(new TestResult
                {
                    ImageName = fileName,
                    Mode = "Hidden",
                    Success = success,
                    Message = success ? "OK" : extractResult.message,
                    ExtractedData = extractResult.data
                });

                try { File.Delete(hideResult.outputPath); } catch { }
            }
            catch (Exception ex)
            {
                Log($"    Исключение: {ex.Message}");
                _results.Add(new TestResult
                {
                    ImageName = fileName,
                    Mode = "Hidden",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private void TestSequentialMode(string imagePath, string fileName)
        {
            Log("  Тест последовательной записи (видимый → скрытый)...");

            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"seq_{Path.GetFileNameWithoutExtension(fileName)}.temp{Path.GetExtension(imagePath)}");
                File.Copy(imagePath, tempFile, true);

                string visibleData = $"VisibleSeq_{DateTime.Now:HHmmss}";
                string hiddenData = $"HiddenSeq_{DateTime.Now:HHmmss}";

                // ЗАПИСЫВАЕМ ВИДИМОЕ В ОТДЕЛЬНЫЙ ФАЙЛ
                _model.SetHiddenMode(false);
                var visibleResult = _model.HideData(tempFile, visibleData);
                if (!visibleResult.success)
                {
                    Log($"    Ошибка записи видимого: {visibleResult.message}");
                    return;
                }

                // ЗАПИСЫВАЕМ СКРЫТОЕ В ОТДЕЛЬНЫЙ ФАЙЛ (НЕ ПОВЕРХ ВИДИМОГО)
                _model.SetHiddenMode(true);
                var hiddenResult = _model.HideData(tempFile, hiddenData);
                if (!hiddenResult.success)
                {
                    Log($"    Ошибка записи скрытого: {hiddenResult.message}");
                    return;
                }

                // ТЕПЕРЬ У НАС ЕСТЬ ДВА ФАЙЛА:
                // visibleResult.outputPath — файл с видимым сообщением
                // hiddenResult.outputPath — файл со скрытым сообщением

                // ИЗВЛЕКАЕМ ИЗ ОБОИХ
                _model.SetHiddenMode(false);
                var extractVisible = _model.ExtractData(visibleResult.outputPath);

                _model.SetHiddenMode(true);
                var extractHidden = _model.ExtractData(hiddenResult.outputPath);

                bool hasVisible = extractVisible.success && extractVisible.data.Contains(visibleData);
                bool hasHidden = extractHidden.success && extractHidden.data.Contains(hiddenData);

                if (hasVisible && hasHidden)
                {
                    Log($"    Успех: оба сообщения найдены");
                    _results.Add(new TestResult
                    {
                        ImageName = fileName,
                        Mode = "Sequential",
                        Success = true,
                        Message = "Видимый+скрытый работают"
                    });
                }
                else
                {
                    Log($"    Ошибка: видимый={hasVisible}, скрытый={hasHidden}");
                    _results.Add(new TestResult
                    {
                        ImageName = fileName,
                        Mode = "Sequential",
                        Success = false,
                        ErrorMessage = "Не удалось извлечь оба сообщения"
                    });
                }

                // Очистка
                try { File.Delete(tempFile); } catch { }
                try { File.Delete(visibleResult.outputPath); } catch { }
                try { File.Delete(hiddenResult.outputPath); } catch { }
            }
            catch (Exception ex)
            {
                Log($"    Исключение: {ex.Message}");
            }
        }

        private void TestEncryptionMode(string imagePath, string fileName)
        {
            Log("  Тест шифрования (XOR и AES)...");

            try
            {
                string password = "test123";
                string testData = $"Encrypt_{DateTime.Now:HHmmss}";

                // XOR
                _model.SetEncryptionSettings(EncryptionModel.EncryptionType.XOR, password);
                _model.SetCompressionSettings(true, 1);
                _model.SetHiddenMode(true);

                var xorResult = _model.HideData(imagePath, testData);
                if (xorResult.success)
                {
                    _model.SetEncryptionSettings(EncryptionModel.EncryptionType.XOR, password);
                    var extractResult = _model.ExtractData(xorResult.outputPath);

                    if (extractResult.success && extractResult.data.Contains(testData))
                    {
                        Log($"    XOR: OK");
                        _results.Add(new TestResult
                        {
                            ImageName = fileName,
                            Mode = "Encryption XOR",
                            Success = true,
                            Message = "XOR работает"
                        });
                    }
                    else
                    {
                        Log($"    XOR: Ошибка");
                        _results.Add(new TestResult
                        {
                            ImageName = fileName,
                            Mode = "Encryption XOR",
                            Success = false,
                            ErrorMessage = "Не удалось расшифровать"
                        });
                    }
                    try { File.Delete(xorResult.outputPath); } catch { }
                }

                // AES
                _model.SetEncryptionSettings(EncryptionModel.EncryptionType.AES128, password);
                _model.SetCompressionSettings(true, 1);
                _model.SetHiddenMode(true);

                var aesResult = _model.HideData(imagePath, testData);
                if (aesResult.success)
                {
                    _model.SetEncryptionSettings(EncryptionModel.EncryptionType.AES128, password);
                    var extractResult = _model.ExtractData(aesResult.outputPath);

                    if (extractResult.success && extractResult.data.Contains(testData))
                    {
                        Log($"    AES: OK");
                        _results.Add(new TestResult
                        {
                            ImageName = fileName,
                            Mode = "Encryption AES",
                            Success = true,
                            Message = "AES работает"
                        });
                    }
                    else
                    {
                        Log($"    AES: Ошибка");
                        _results.Add(new TestResult
                        {
                            ImageName = fileName,
                            Mode = "Encryption AES",
                            Success = false,
                            ErrorMessage = "Не удалось расшифровать"
                        });
                    }
                    try { File.Delete(aesResult.outputPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"    Исключение: {ex.Message}");
            }
        }

        private bool IsFileValid(string filePath)
        {
            try
            {
                using (var img = System.Drawing.Image.FromFile(filePath))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void Log(string message)
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "metahide.log");
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
            Console.WriteLine(message);
        }
    }

    public class TestResult
    {
        public string ImageName { get; set; }
        public string Mode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TestData { get; set; }
        public string ExtractedData { get; set; }
        public string ErrorMessage { get; set; }
    }
}