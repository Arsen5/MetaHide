using System;
using System.IO;
using TagLib;
using test;

namespace MetaHide.model
{
    public class VideoSteganography : ISteganography
    {
        private bool _hiddenMode = false;
        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;
        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp4" || ext == ".avi" || ext == ".mkv";
        }
        public (bool success, string message, string outputPath) HideData(string filePath, string data)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLower();
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileNameWithoutExtension(filePath) + "_hidden" + ext);
                System.IO.File.Copy(filePath, outputPath, true);
                using (var file = TagLib.File.Create(outputPath))
                {
                    string marker = "###METAHD###";
                    file.Tag.Comment = data + marker;
                    file.Save();
                }
                return (true, $"Данные скрыты в видео (метаданные)", outputPath);
            }
            catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
        }
        public (bool success, string message, string data) ExtractData(string filePath)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    string comment = file.Tag.Comment ?? "";
                    string marker = "###METAHD###";
                    int endIndex = comment.IndexOf(marker);
                    string result = endIndex > 0 ? comment.Substring(0, endIndex) : comment;
                    return (true, $"Извлечено {result.Length} символов", result);
                }
            }
            catch (Exception ex) { return (false, $"Ошибка: {ex.Message}", null); }
        }
        public bool HasHiddenData(string filePath) => ExtractData(filePath).success;
        public string GetAllExifFields(string filePath) => ExtractData(filePath).data ?? "Данные не найдены";
    }
}