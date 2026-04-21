// Controller.cs
using System.Windows.Forms;
using test;

namespace MetaHide
{
    internal class Controller
    {
        private Model _model;
        private Form1 _view;

        public Controller(Form1 view)
        {
            _model = new Model();
            _view = view;

            // Подписываемся на события формы
            _view.HideRequested += OnHideRequested;
            _view.ExtractRequested += OnExtractRequested;
        }

        private void OnHideRequested(string imagePath, string text)
        {
            _model.HideData(imagePath, text);
        }

        private void OnExtractRequested(string imagePath)
        {
            string data = _model.ExtractData(imagePath);
            _view.ShowExtractedData(data);
        }
    }
}