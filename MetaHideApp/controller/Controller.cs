using System;
using System.IO;
using System.Windows.Forms;
using MetaHide.model;
using View = MetaHide.view.View;

namespace MetaHide.controller
{
    internal class Controller
    {
        private Model _model;
        private View _view;
        private string _currentMethod = "exif";

        public Controller(View view, Model model)
        {
            _model = model;
            _view = view;

            _view.HideRequested += OnHideRequested;
            _view.ExtractRequested += OnExtractRequested;
            _view.ModeChangedRequested += OnModeChangedRequested;
            _view.EncryptionSettingsChanged += OnEncryptionSettingsChanged;
            _view.CompressionSettingsChanged += OnCompressionSettingsChanged;
            _view.MethodTypeChanged += OnMethodTypeChanged;
        }

        private void OnMethodTypeChanged(string methodType)
        {
            _currentMethod = methodType;
            _model.SetMethod(methodType);
            _view.UpdateStatus($"Выбран метод: {methodType}");

            if (methodType == "mp3")
                _view.UpdateStatus("MP3: данные скрываются в ID3 комментариях");
            else if (methodType == "video")
                _view.UpdateStatus("Видео: данные скрываются в метаданных");
        }

        private void OnHideRequested(string imagePath, string text)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("Сначала выберите файл!");
                return;
            }
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Введите текст для скрытия!");
                return;
            }

            _model.SetMethod(_currentMethod);
            bool isHidden = (_currentMethod == "marker");
            _model.SetHiddenMode(isHidden);

            var result = _model.HideData(imagePath, text);

            if (result.success)
            {
                MessageBox.Show(result.message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _view.UpdateStatus($"Сохранено: {Path.GetFileName(result.outputPath)}");
            }
            else
            {
                MessageBox.Show(result.message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _view.UpdateStatus("Ошибка при скрытии");
            }
        }

        private void OnExtractRequested(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("Сначала выберите файл!");
                return;
            }

            _model.SetMethod(_currentMethod);
            bool isHidden = (_currentMethod == "marker");
            _model.SetHiddenMode(isHidden);

            string? password = null;
            if (_model.GetEncryptionType() != EncryptionModel.EncryptionType.None)
            {
                password = _view.ShowPasswordDialog();
                if (password == null) return;
                _model.SetEncryptionSettings(_model.GetEncryptionType(), password);
            }

            var result = _model.ExtractData(imagePath);

            if (result.success)
            {
                _view.ShowExtractedData(result.data);
                _view.UpdateStatus(result.message);
            }
            else
            {
                _view.ShowExtractedData("");
                _view.UpdateStatus(result.message);
                MessageBox.Show(result.message, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnModeChangedRequested(bool isHidden)
        {
            _model.SetHiddenMode(isHidden);
            _view.UpdateStatus(isHidden ? "Скрытый режим" : "Обычный режим");
        }

        private void OnEncryptionSettingsChanged(EncryptionModel.EncryptionType type, string password)
        {
            _model.SetEncryptionSettings(type, password);
            _view.UpdateStatus($"Шифрование: {type}");
        }

        private void OnCompressionSettingsChanged(bool useCompression, int thresholdKB)
        {
            _model.SetCompressionSettings(useCompression, thresholdKB);
            _view.UpdateStatus(useCompression ? $"Сжатие включено (порог {thresholdKB} КБ)" : "Сжатие выключено");
        }
    }
}