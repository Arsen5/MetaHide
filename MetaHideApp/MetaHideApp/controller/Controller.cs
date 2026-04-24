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

        public Controller(View view, Model model)
        {
            _model = model;
            _view = view;

            // Подписываемся на события формы
            _view.HideRequested += OnHideRequested;
            _view.ExtractRequested += OnExtractRequested;
            //_view.ShowAllFieldsRequested += OnShowAllFieldsRequested;
            _view.ModeChangedRequested += OnModeChangedRequested;
        }

        private void OnHideRequested(string imagePath, string text)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("Сначала выберите изображение!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Введите текст для скрытия!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                MessageBox.Show("Сначала выберите изображение!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
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

        private void OnShowAllFieldsRequested(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("Сначала выберите изображение!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string result = _model.GetAllExifFields(imagePath);
            _view.ShowExtractedData(result);
            _view.UpdateStatus("Анализ завершён");
        }

        private void OnModeChangedRequested(bool isHidden)
        {
            _model.SetHiddenMode(isHidden);
            string modeName = isHidden ? "Скрытый (данные в конец файла)" : "Обычный (ImageDescription)";
            _view.UpdateStatus($"Режим: {modeName}");
        }
    }
}