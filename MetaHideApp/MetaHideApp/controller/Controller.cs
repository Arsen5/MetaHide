using MetaHide.model;
using System.Windows.Forms;
using test;

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
            _view.ShowAllFieldsRequested += OnShowAllFieldsRequested;
            _view.ModeChangedRequested += OnModeChangedRequested;
        }

        private void OnHideRequested(string imagePath, string text)
        {
            var result = _model.HideData(imagePath, text);

            if (result.success)
            {
                MessageBox.Show(result.message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _view.UpdateStatus($"Готово! Сохранено: {Path.GetFileName(result.outputPath)}");
            }
            else
            {
                MessageBox.Show(result.message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _view.UpdateStatus("Ошибка при скрытии");
            }
        }

        private void OnExtractRequested(string imagePath)
        {
            var result = _model.ExtractData(imagePath);

            if (result.success)
            {
                _view.ShowExtractedData(result.data);
                _view.UpdateStatus(result.message);
            }
            else
            {
                _view.ShowExtractedData(result.message);
                _view.UpdateStatus("Данные не найдены");
            }
        }

        private void OnShowAllFieldsRequested(string imagePath)
        {
            string result = _model.GetAllExifFields(imagePath);
            _view.ShowExtractedData(result);

            int fieldCount = result.Split(new[] { "🔹" }, StringSplitOptions.None).Length - 1;
            _view.UpdateStatus($"Найдено {fieldCount} полей");
        }

        private void OnModeChangedRequested(bool isHidden)
        {
            _model.SetHiddenMode(isHidden);
            string modeName = isHidden ? "Скрытый" : "Обычный";
            _view.UpdateStatus($"Режим: {modeName}");
        }
    }
}