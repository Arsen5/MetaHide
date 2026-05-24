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

    // Новые поля для шифрования и сжатия
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
            new PngSteganography()
        };

        _encryptionModel = new EncryptionModel();
        _compressionModel = new CompressionModel();
    }

    // Методы для настройки шифрования и сжатия
    public void SetEncryptionSettings(EncryptionModel.EncryptionType type, string password)
    {
        _encryptionType = type;
        _encryptionPassword = password;
    }

    // НОВЫЙ МЕТОД: для получения текущего типа шифрования
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
        _activeHandler = GetHandler(imagePath);
        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла (только .jpg, .jpeg, .png)", null);

        try
        {
            // 1. Подготовка данных
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // 2. Сжатие (ТОЛЬКО если включено И выбрано шифрование)
            if (_useCompression && _encryptionType != EncryptionModel.EncryptionType.None)
            {
                dataBytes = _compressionModel.Compress(dataBytes, _compressionThresholdKB);
            }

            // 3. Шифрование (ТОЛЬКО если выбрано шифрование)
            if (_encryptionType != EncryptionModel.EncryptionType.None)
            {
                dataBytes = _encryptionModel.Encrypt(dataBytes, _encryptionPassword, _encryptionType);
            }

            // 4. ФОРМИРОВАНИЕ ДАННЫХ ДЛЯ ВСТРАИВАНИЯ
            string processedData;
            if (_encryptionType == EncryptionModel.EncryptionType.None)
            {
                // ВИДИМЫЙ РЕЖИМ: встраиваем текст напрямую (без Base64)
                processedData = data;
            }
            else
            {
                // ШИФРОВАННЫЙ РЕЖИМ: используем Base64
                processedData = Convert.ToBase64String(dataBytes);
            }

            // 5. Встраивание в изображение
            _activeHandler.SetHiddenMode(_hiddenMode);
            var result = _activeHandler.HideData(imagePath, processedData);

            // Логирование
            if (result.success)
            {
                string encryptionInfo = _encryptionType == EncryptionModel.EncryptionType.None ?
                    "без шифрования" : $"шифрование: {_encryptionType}";
                string compressionInfo = _useCompression ? "сжатие: да" : "сжатие: нет";

                LogOperation("Встраивание", imagePath,
                    $"Метод: {_activeHandler.GetType().Name}, {encryptionInfo}, {compressionInfo}",
                    "Успех");
            }

            return result;
        }
        catch (Exception ex)
        {
            LogOperation("Ошибка встраивания", imagePath, "Подготовка данных", ex.Message);
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        _activeHandler = GetHandler(imagePath);
        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла", null);

        try
        {
            _activeHandler.SetHiddenMode(_hiddenMode);
            var result = _activeHandler.ExtractData(imagePath);

            if (result.success)
            {
                string extractedData = result.data;
                string finalText;

                // Проверяем, является ли извлеченное значение Base64
                if (IsBase64String(extractedData))
                {
                    // Это Base64 (шифрованный режим)
                    byte[] extractedBytes = Convert.FromBase64String(extractedData);

                    // 2. Дешифрование (ТОЛЬКО если выбрано шифрование)
                    byte[] decryptedBytes;
                    if (_encryptionType != EncryptionModel.EncryptionType.None)
                    {
                        decryptedBytes = _encryptionModel.Decrypt(extractedBytes, _encryptionPassword, _encryptionType);
                    }
                    else
                    {
                        // Если шифрование не выбрано, но данные в Base64 - это ошибка
                        decryptedBytes = extractedBytes;
                    }

                    // 3. Распаковка (ТОЛЬКО если было сжатие и выбрано шифрование)
                    byte[] finalBytes = decryptedBytes;
                    if (_useCompression && _encryptionType != EncryptionModel.EncryptionType.None)
                    {
                        try
                        {
                            finalBytes = _compressionModel.Decompress(decryptedBytes);
                        }
                        catch
                        {
                            finalBytes = decryptedBytes;
                        }
                    }

                    finalText = Encoding.UTF8.GetString(finalBytes);
                }
                else
                {
                    // Это обычный текст (видимый режим)
                    finalText = extractedData;
                }

                string encryptionInfo = _encryptionType == EncryptionModel.EncryptionType.None ?
                    "без дешифрования" : $"дешифрование: {_encryptionType}";

                LogOperation("Извлечение", imagePath,
                    $"Метод: {_activeHandler.GetType().Name}, {encryptionInfo}", "Успех");

                return (true, result.message, finalText);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogOperation("Ошибка извлечения", imagePath, "Обработка данных", ex.Message);
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    // Вспомогательный метод для проверки Base64
    private bool IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return false;

        // Base64 должен быть кратен 4 и содержать только допустимые символы
        if (base64.Length % 4 != 0)
            return false;

        // Проверяем допустимые символы Base64
        foreach (char c in base64)
        {
            if (!((c >= 'A' && c <= 'Z') ||
                  (c >= 'a' && c <= 'z') ||
                  (c >= '0' && c <= '9') ||
                  c == '+' || c == '/' || c == '='))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasHiddenData(string imagePath)
    {
        var handler = GetHandler(imagePath);
        if (handler == null) return false;
        handler.SetHiddenMode(_hiddenMode);
        return handler.HasHiddenData(imagePath);
    }

    public string GetAllExifFields(string imagePath)
    {
        var handler = GetHandler(imagePath);
        if (handler == null) return "Неподдерживаемый формат файла";
        handler.SetHiddenMode(_hiddenMode);
        return handler.GetAllExifFields(imagePath);
    }

    public bool SupportsFormat(string filePath)
    {
        return GetHandler(filePath) != null;
    }

    private ISteganography GetHandler(string filePath)
    {
        foreach (var handler in _handlers)
            if (handler.SupportsFormat(filePath))
                return handler;
        return null;
    }

    private void LogOperation(string operation, string file, string method, string result)
    {
        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {operation} | {Path.GetFileName(file)} | {method} | {result}";
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "metahide.log");
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch
        {
            // Игнорируем ошибки логирования
        }
    }
}