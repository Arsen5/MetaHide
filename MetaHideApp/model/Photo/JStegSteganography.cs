using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using test;

namespace MetaHide.model
{
    public class JStegSteganography : ISteganography
    {
        private bool _hiddenMode = false;

        public void SetHiddenMode(bool hidden) => _hiddenMode = hidden;
        public int GetCurrentFieldId() => 0;

        public bool SupportsFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".jpg" || ext == ".jpeg";
        }

        private string FindJstegExe()
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsteg.exe");
            if (File.Exists(exePath))
                return exePath;
            return null;
        }

        public (bool success, string message, string outputPath) HideData(string imagePath, string data)
        {
            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.GetFileNameWithoutExtension(imagePath) + "_jsteg.jpg");

                string tempDataFile = Path.GetTempFileName();
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                File.WriteAllBytes(tempDataFile, dataBytes);

                string jstegPath = FindJstegExe();
                if (jstegPath == null)
                {
                    File.Delete(tempDataFile);
                    return (false, "jsteg.exe не найден. Положите файл в папку с программой.", null);
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = jstegPath,
                    Arguments = $"hide \"{imagePath}\" \"{tempDataFile}\" \"{outputPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    string error = p.StandardError.ReadToEnd();
                    File.Delete(tempDataFile);

                    if (p.ExitCode == 0)
                        return (true, $"Данные скрыты через JSteg ({data.Length} символов)", outputPath);
                    else
                        return (false, $"Ошибка jsteg: {error}", null);
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
                string jstegPath = FindJstegExe();
                if (jstegPath == null)
                    return (false, "jsteg.exe не найден", null);

                string tempOutputFile = Path.GetTempFileName();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = jstegPath,
                    Arguments = $"reveal \"{imagePath}\" \"{tempOutputFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    string error = p.StandardError.ReadToEnd();

                    if (p.ExitCode != 0)
                    {
                        File.Delete(tempOutputFile);
                        return (false, $"Ошибка jsteg: {error}", null);
                    }

                    if (!File.Exists(tempOutputFile) || new FileInfo(tempOutputFile).Length == 0)
                    {
                        File.Delete(tempOutputFile);
                        return (false, "Данные не найдены", null);
                    }

                    byte[] dataBytes = File.ReadAllBytes(tempOutputFile);
                    File.Delete(tempOutputFile);

                    string text = Encoding.UTF8.GetString(dataBytes);
                    return (true, $"Извлечено {dataBytes.Length} байт", text);
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