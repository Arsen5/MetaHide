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
            Log($"Папка: {_testImagesFolder}");

            var imageFiles = GetTestImages();
            if (imageFiles.Count == 0)
            {
                Log("Нет изображений! Добавьте PNG, JPG, BMP, GIF в папку TestImages");
                return;
            }
            Log($"Найдено {imageFiles.Count} изображений\n");

            // 1. Базовые тесты для PNG/JPEG (обычный и скрытый режимы, с шифрованием)
            foreach (var img in imageFiles)
            {
                string ext = Path.GetExtension(img).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    TestStandard(img);
            }

            // 2. Специализированные тесты
            TestLsbOnPng();       // LSB для PNG со всеми вариантами шифрования
            TestBmpFiles();       // BMP (LSB и скрытый) со всеми вариантами шифрования
            TestGifFiles();       // GIF (скрытый режим) со всеми вариантами шифрования

            Log("\n=== СВОДКА ===");
            foreach (var r in _results)
                Log($"{(r.Success ? "[OK]" : "[FAIL]")} {r.ImageName} - {r.Mode}: {r.ErrorMessage ?? r.Message}");
        }

        private List<string> GetTestImages()
        {
            var images = new List<string>();
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.png"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.jpg"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.jpeg"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.bmp"));
            images.AddRange(Directory.GetFiles(_testImagesFolder, "*.gif"));
            return images;
        }

        // ========== БАЗОВЫЕ ТЕСТЫ ДЛЯ PNG/JPEG ==========
        private void TestStandard(string imagePath)
        {
            string fileName = Path.GetFileName(imagePath);
            Log($"\nТестируем: {fileName} ({new FileInfo(imagePath).Length} байт)");
            if (!IsValidImage(imagePath))
            {
                _results.Add(new TestResult { ImageName = fileName, Mode = "Invalid", Success = false, ErrorMessage = "Невалидное изображение" });
                return;
            }

            // Видимый режим
            RunTest(imagePath, fileName, "exif", false, false, "Visible_NoEnc");
            RunTest(imagePath, fileName, "exif", false, true, "Visible_XOR", EncryptionModel.EncryptionType.XOR, "pass");
            RunTest(imagePath, fileName, "exif", false, true, "Visible_AES", EncryptionModel.EncryptionType.AES128, "pass");

            // Скрытый режим (маркер)
            RunTest(imagePath, fileName, "marker", true, false, "Hidden_NoEnc");
            RunTest(imagePath, fileName, "marker", true, true, "Hidden_XOR", EncryptionModel.EncryptionType.XOR, "pass");
            RunTest(imagePath, fileName, "marker", true, true, "Hidden_AES", EncryptionModel.EncryptionType.AES128, "pass");

            // Последовательный
            TestSequential(imagePath, fileName);
        }

        private void RunTest(string imagePath, string fileName, string method, bool hiddenMode, bool useEnc, string testName,
                            EncryptionModel.EncryptionType encType = EncryptionModel.EncryptionType.None, string password = "")
        {
            Log($"  {testName}...");
            try
            {
                _model.SetMethod(method);
                _model.SetHiddenMode(hiddenMode);
                if (useEnc) _model.SetEncryptionSettings(encType, password);
                else _model.SetEncryptionSettings(EncryptionModel.EncryptionType.None, "");

                string testData = $"{testName}_{DateTime.Now:HHmmss}";
                var hide = _model.HideData(imagePath, testData);
                if (!hide.success)
                {
                    Log($"    Ошибка записи: {hide.message}");
                    _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = hide.message });
                    return;
                }

                if (useEnc) _model.SetEncryptionSettings(encType, password);
                var extract = _model.ExtractData(hide.outputPath);
                bool ok = extract.success && extract.data == testData;
                Log($"    {(ok ? "✓" : "✗")} Запись: {hide.success}, Извлечение: {extract.success}, Данные: {extract.data}");
                _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = ok, Message = ok ? "OK" : extract.message });
                try { File.Delete(hide.outputPath); } catch { }
            }
            catch (Exception ex)
            {
                Log($"    Исключение: {ex.Message}");
                _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = ex.Message });
            }
        }

        private void TestSequential(string imagePath, string fileName)
        {
            Log("  Sequential (exif+marker)...");
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"seq_{Path.GetFileNameWithoutExtension(fileName)}.temp{Path.GetExtension(imagePath)}");
                File.Copy(imagePath, tempFile, true);
                string visibleData = $"VisibleSeq_{DateTime.Now:HHmmss}";
                string hiddenData = $"HiddenSeq_{DateTime.Now:HHmmss}";

                _model.SetMethod("exif");
                _model.SetHiddenMode(false);
                var vis = _model.HideData(tempFile, visibleData);
                if (!vis.success) { Log($"    Ошибка видимого: {vis.message}"); return; }

                _model.SetMethod("marker");
                _model.SetHiddenMode(true);
                var hid = _model.HideData(tempFile, hiddenData);
                if (!hid.success) { Log($"    Ошибка скрытого: {hid.message}"); return; }

                _model.SetMethod("exif");
                _model.SetHiddenMode(false);
                var extVis = _model.ExtractData(vis.outputPath);
                _model.SetMethod("marker");
                _model.SetHiddenMode(true);
                var extHid = _model.ExtractData(hid.outputPath);

                bool ok = extVis.success && extVis.data == visibleData && extHid.success && extHid.data == hiddenData;
                Log($"    {(ok ? "✓" : "✗")} Видимый: {extVis.success}, Скрытый: {extHid.success}");
                _results.Add(new TestResult { ImageName = fileName, Mode = "Sequential", Success = ok, Message = ok ? "OK" : "Не оба извлечены" });
                try { File.Delete(tempFile); File.Delete(vis.outputPath); File.Delete(hid.outputPath); } catch { }
            }
            catch (Exception ex) { Log($"    Исключение: {ex.Message}"); }
        }

        // ========== LSB НА PNG (с шифрованием) ==========
        private void TestLsbOnPng()
        {
            var pngFiles = Directory.GetFiles(_testImagesFolder, "*.png");
            if (pngFiles.Length == 0) { Log("\nНет PNG для LSB"); return; }
            Log("\n=== LSB НА PNG ===");
            foreach (string img in pngFiles)
            {
                string name = Path.GetFileName(img);
                RunLsbVariant(img, name, "LSB_NoEnc", false, EncryptionModel.EncryptionType.None, "");
                RunLsbVariant(img, name, "LSB_XOR", true, EncryptionModel.EncryptionType.XOR, "pass");
                RunLsbVariant(img, name, "LSB_AES", true, EncryptionModel.EncryptionType.AES128, "pass");
            }
        }

        private void RunLsbVariant(string imagePath, string fileName, string testName, bool useEnc, EncryptionModel.EncryptionType encType, string pwd)
        {
            Log($"  {testName}...");
            try
            {
                _model.SetMethod("lsb");
                _model.SetHiddenMode(false);
                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                else _model.SetEncryptionSettings(EncryptionModel.EncryptionType.None, "");

                string testData = $"{testName}_{DateTime.Now:HHmmss}";
                var hide = _model.HideData(imagePath, testData);
                if (!hide.success) { Log($"    Ошибка записи: {hide.message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = hide.message }); return; }

                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                var extract = _model.ExtractData(hide.outputPath);
                bool ok = extract.success && extract.data == testData;
                Log($"    {(ok ? "✓" : "✗")} Извлечено: {extract.data}");
                _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = ok, Message = ok ? "OK" : extract.message });
                try { File.Delete(hide.outputPath); } catch { }
            }
            catch (Exception ex) { Log($"    Исключение: {ex.Message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = ex.Message }); }
        }

        // ========== BMP (LSB и скрытый) с шифрованием ==========
        private void TestBmpFiles()
        {
            var bmpFiles = Directory.GetFiles(_testImagesFolder, "*.bmp");
            if (bmpFiles.Length == 0) { Log("\nНет BMP файлов"); return; }
            Log("\n=== BMP ===");
            foreach (string img in bmpFiles)
            {
                string name = Path.GetFileName(img);
                // LSB
                RunBmpVariant(img, name, "BMP_LSB_NoEnc", "lsb", false, false, EncryptionModel.EncryptionType.None, "");
                RunBmpVariant(img, name, "BMP_LSB_XOR", "lsb", false, true, EncryptionModel.EncryptionType.XOR, "pass");
                RunBmpVariant(img, name, "BMP_LSB_AES", "lsb", false, true, EncryptionModel.EncryptionType.AES128, "pass");
                // Скрытый режим (маркер)
                RunBmpVariant(img, name, "BMP_Hidden_NoEnc", "marker", true, false, EncryptionModel.EncryptionType.None, "");
                RunBmpVariant(img, name, "BMP_Hidden_XOR", "marker", true, true, EncryptionModel.EncryptionType.XOR, "pass");
                RunBmpVariant(img, name, "BMP_Hidden_AES", "marker", true, true, EncryptionModel.EncryptionType.AES128, "pass");
            }
        }

        private void RunBmpVariant(string imagePath, string fileName, string testName, string method, bool hiddenMode, bool useEnc,
                                   EncryptionModel.EncryptionType encType, string pwd)
        {
            Log($"  {testName}...");
            try
            {
                _model.SetMethod(method);
                _model.SetHiddenMode(hiddenMode);
                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                else _model.SetEncryptionSettings(EncryptionModel.EncryptionType.None, "");

                string testData = $"{testName}_{DateTime.Now:HHmmss}";
                var hide = _model.HideData(imagePath, testData);
                if (!hide.success) { Log($"    Ошибка записи: {hide.message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = hide.message }); return; }

                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                var extract = _model.ExtractData(hide.outputPath);
                bool ok = extract.success && extract.data == testData;
                Log($"    {(ok ? "✓" : "✗")} Извлечено: {extract.data}");
                _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = ok, Message = ok ? "OK" : extract.message });
                try { File.Delete(hide.outputPath); } catch { }
            }
            catch (Exception ex) { Log($"    Исключение: {ex.Message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = ex.Message }); }
        }

        // ========== GIF (скрытый режим) с шифрованием ==========
        private void TestGifFiles()
        {
            var gifFiles = Directory.GetFiles(_testImagesFolder, "*.gif");
            if (gifFiles.Length == 0) { Log("\nНет GIF файлов"); return; }
            Log("\n=== GIF ===");
            foreach (string img in gifFiles)
            {
                string name = Path.GetFileName(img);
                RunGifVariant(img, name, "GIF_Hidden_NoEnc", false, EncryptionModel.EncryptionType.None, "");
                RunGifVariant(img, name, "GIF_Hidden_XOR", true, EncryptionModel.EncryptionType.XOR, "pass");
                RunGifVariant(img, name, "GIF_Hidden_AES", true, EncryptionModel.EncryptionType.AES128, "pass");
            }
        }

        private void RunGifVariant(string imagePath, string fileName, string testName, bool useEnc, EncryptionModel.EncryptionType encType, string pwd)
        {
            Log($"  {testName}...");
            try
            {
                _model.SetMethod("gif");
                _model.SetHiddenMode(true); // для GIF используем скрытый режим
                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                else _model.SetEncryptionSettings(EncryptionModel.EncryptionType.None, "");

                string testData = $"{testName}_{DateTime.Now:HHmmss}";
                var hide = _model.HideData(imagePath, testData);
                if (!hide.success) { Log($"    Ошибка записи: {hide.message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = hide.message }); return; }

                if (useEnc) _model.SetEncryptionSettings(encType, pwd);
                var extract = _model.ExtractData(hide.outputPath);
                bool ok = extract.success && extract.data == testData;
                Log($"    {(ok ? "✓" : "✗")} Извлечено: {extract.data}");
                _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = ok, Message = ok ? "OK" : extract.message });
                try { File.Delete(hide.outputPath); } catch { }
            }
            catch (Exception ex) { Log($"    Исключение: {ex.Message}"); _results.Add(new TestResult { ImageName = fileName, Mode = testName, Success = false, ErrorMessage = ex.Message }); }
        }

        private bool IsValidImage(string path)
        {
            try { using (var img = System.Drawing.Image.FromFile(path)) { return true; } }
            catch { return false; }
        }

        private void Log(string msg)
        {
            string logPath = Path.Combine(Application.StartupPath, "metahide.log");
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {msg}";
            File.AppendAllText(logPath, entry + Environment.NewLine);
            Console.WriteLine(msg);
        }
    }

    public class TestResult
    {
        public string ImageName { get; set; }
        public string Mode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
}