using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model;

public class Model : ISteganography
{
    private readonly List<ISteganography> _handlers;
    private ISteganography _activeHandler;
    private bool _hiddenMode = false;

    private string _selectedMethod = "exif";

    private readonly EncryptionModel _encryptionModel;
    private readonly CompressionModel _compressionModel;
    private EncryptionModel.EncryptionType _encryptionType = EncryptionModel.EncryptionType.None;
    private string _encryptionPassword = "";
    private bool _useCompression = false;
    private int _compressionThresholdKB = 1;

    public Model()
    {
        _handlers = new List<ISteganography>
        {
            new JpegSteganography(),
            new PngSteganography(),
            new LSBSteganography(),
            new BmpSteganography(),
            new GifSteganography()
        };

        _encryptionModel = new EncryptionModel();
        _compressionModel = new CompressionModel();
    }

    public void SetMethod(string method)
    {
        _selectedMethod = method;
        System.Diagnostics.Debug.WriteLine($"Метод установлен: {_selectedMethod}");
    }

    public void SetEncryptionSettings(EncryptionModel.EncryptionType type, string password)
    {
        _encryptionType = type;
        _encryptionPassword = password;
    }

    public EncryptionModel.EncryptionType GetEncryptionType()
    {
        return _encryptionType;
    }

    public void SetCompressionSettings(bool useCompression, int thresholdKB)
    {
        _useCompression = useCompression;
        _compressionThresholdKB = thresholdKB;
    }

    public void SetHiddenMode(bool hidden)
    {
        _hiddenMode = hidden;
        _activeHandler?.SetHiddenMode(hidden);
    }

    public int GetCurrentFieldId()
    {
        return _activeHandler?.GetCurrentFieldId() ?? 0;
    }

    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        _activeHandler = GetHandlerByMethod(imagePath);

        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла для выбранного метода", null);

        try
        {
            long sourceSize = new FileInfo(imagePath).Length;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Сжатие (только если включено И выбрано шифрование)
            if (_useCompression && _encryptionType != EncryptionModel.EncryptionType.None)
            {
                dataBytes = _compressionModel.Compress(dataBytes, _compressionThresholdKB);
            }

            // Шифрование (только если выбрано шифрование)
            if (_encryptionType != EncryptionModel.EncryptionType.None)
            {
                dataBytes = _encryptionModel.Encrypt(dataBytes, _encryptionPassword, _encryptionType);
            }

            string processedData;
            if (_encryptionType == EncryptionModel.EncryptionType.None)
            {
                processedData = data;
            }
            else
            {
                processedData = Convert.ToBase64String(dataBytes);
            }

            _activeHandler.SetHiddenMode(_hiddenMode);
            var result = _activeHandler.HideData(imagePath, processedData);

            if (result.success)
            {
                long resultSize = new FileInfo(result.outputPath).Length;
                LogOperation("Встраивание", imagePath,
                    $"Метод: {_activeHandler.GetType().Name}, выбранный: {_selectedMethod}",
                    "Успех", sourceSize, resultSize);
            }

            return result;
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        _activeHandler = GetHandlerByMethod(imagePath);

        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла для выбранного метода", null);

        try
        {
            long sourceSize = new FileInfo(imagePath).Length;
            _activeHandler.SetHiddenMode(_hiddenMode);
            var result = _activeHandler.ExtractData(imagePath);

            if (result.success)
            {
                string extractedData = result.data;
                string finalText;

                // Если включено шифрование — расшифровываем
                if (_encryptionType != EncryptionModel.EncryptionType.None)
                {
                    try
                    {
                        byte[] extractedBytes = Convert.FromBase64String(extractedData);
                        byte[] decryptedBytes = _encryptionModel.Decrypt(extractedBytes, _encryptionPassword, _encryptionType);

                        // Распаковка если было сжатие
                        if (_useCompression)
                        {
                            try
                            {
                                decryptedBytes = _compressionModel.Decompress(decryptedBytes);
                            }
                            catch { }
                        }

                        finalText = Encoding.UTF8.GetString(decryptedBytes);
                    }
                    catch (Exception ex)
                    {
                        LogOperation("Ошибка расшифровки", imagePath, "Расшифровка", ex.Message, sourceSize, 0);
                        return (false, $"Ошибка расшифровки: {ex.Message}", null);
                    }
                }
                else
                {
                    // Без шифрования
                    finalText = extractedData;
                }

                LogOperation("Извлечение", imagePath,
                    $"Метод: {_activeHandler.GetType().Name}, выбранный: {_selectedMethod}",
                    "Успех", sourceSize, 0);

                return (true, result.message, finalText);
            }
            else
            {
                LogOperation("Извлечение", imagePath,
                    $"Метод: {_activeHandler.GetType().Name}, выбранный: {_selectedMethod}",
                    result.message, sourceSize, 0);
                return result;
            }
        }
        catch (Exception ex)
        {
            LogOperation("Ошибка извлечения", imagePath, "Обработка данных", ex.Message, 0, 0);
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    private ISteganography GetHandlerByMethod(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();

        System.Diagnostics.Debug.WriteLine($"GetHandlerByMethod: selectedMethod={_selectedMethod}, ext={ext}");

        if (_selectedMethod == "lsb")
        {
            if (ext == ".png")
                return new LSBSteganography();
            if (ext == ".bmp")
                return new BmpSteganography();
            return null;
        }

        if (_selectedMethod == "gif")
        {
            if (ext == ".gif")
                return new GifSteganography();
            return null;
        }

        if (_selectedMethod == "marker")
        {
            if (ext == ".jpg" || ext == ".jpeg")
                return new JpegSteganography();
            if (ext == ".png")
                return new PngSteganography();
            if (ext == ".gif")
                return new GifSteganography();
            if (ext == ".bmp")
                return new BmpSteganography();
            return null;
        }

        if (_selectedMethod == "exif")
        {
            foreach (var handler in _handlers)
                if (handler.SupportsFormat(filePath))
                    return handler;
        }

        foreach (var handler in _handlers)
            if (handler.SupportsFormat(filePath))
                return handler;

        return null;
    }

    public bool HasHiddenData(string imagePath)
    {
        var handler = GetHandlerByMethod(imagePath);
        if (handler == null) return false;
        handler.SetHiddenMode(_hiddenMode);
        return handler.HasHiddenData(imagePath);
    }

    public string GetAllExifFields(string imagePath)
    {
        var handler = GetHandlerByMethod(imagePath);
        if (handler == null) return "Неподдерживаемый формат файла";
        handler.SetHiddenMode(_hiddenMode);
        return handler.GetAllExifFields(imagePath);
    }

    public bool SupportsFormat(string filePath)
    {
        return GetHandlerByMethod(filePath) != null;
    }

    private void LogOperation(string operation, string file, string method, string result, long sourceSize, long resultSize)
    {
        try
        {
            string sizeInfo = "";
            if (sourceSize > 0)
            {
                sizeInfo = $" | Размер: {sourceSize} байт";
                if (resultSize > 0)
                {
                    sizeInfo += $" -> {resultSize} байт";
                    double increase = (double)resultSize / sourceSize * 100;
                    sizeInfo += $" (увеличение: {increase:F1}%)";
                }
            }

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {operation} | {Path.GetFileName(file)} | {method} | {result}{sizeInfo}";
            string logPath = Path.Combine(Application.StartupPath, "metahide.log");
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch
        {
        }
    }
}