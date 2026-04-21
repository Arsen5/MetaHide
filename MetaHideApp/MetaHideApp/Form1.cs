// Form1.cs (добавьте элементы управления)
using MetaHide;
using System;
using System.Windows.Forms;

namespace test
{
    public partial class Form1 : Form
    {
        // Временные элементы для тестирования
        private Button btnSelectImage;
        private Button btnHide;
        private Button btnExtract;
        private Button btnShowAllFields;  // ← НОВАЯ КНОПКА
        private TextBox txtText;
        private RichTextBox txtResult;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();
            CreateTestControls();
        }

        private void CreateTestControls()
        {
            // Кнопка выбора изображения
            btnSelectImage = new Button { Text = "Выбрать изображение", Location = new System.Drawing.Point(12, 12), Size = new System.Drawing.Size(150, 30) };
            btnSelectImage.Click += BtnSelectImage_Click;

            // Поле для текста
            txtText = new TextBox { Location = new System.Drawing.Point(12, 50), Size = new System.Drawing.Size(400, 100), Multiline = true };
            txtText.Text = "Введите текст (минимум 1 Кб)...\n" + new string('A', 1024); // Заглушка на 1 Кб

            // Кнопка "Спрятать"
            btnHide = new Button { Text = "Спрятать в EXIF", Location = new System.Drawing.Point(12, 160), Size = new System.Drawing.Size(150, 30) };
            btnHide.Click += BtnHide_Click;

            // Кнопка "Извлечь"
            btnExtract = new Button { Text = "Извлечь из EXIF", Location = new System.Drawing.Point(170, 160), Size = new System.Drawing.Size(150, 30) };
            btnExtract.Click += BtnExtract_Click;

            // НОВАЯ КНОПКА: "Показать все поля"
            btnShowAllFields = new Button
            {
                Text = "🔍 Показать все поля",
                Location = new System.Drawing.Point(330, 160),
                Size = new System.Drawing.Size(150, 30)
            };
            btnShowAllFields.Click += BtnShowAllFields_Click;

            // Поле для результата
            txtResult = new RichTextBox { Location = new System.Drawing.Point(12, 200), Size = new System.Drawing.Size(600, 150), ReadOnly = true };

            // Статус
            lblStatus = new Label { Text = "Готов", Location = new System.Drawing.Point(12, 360), Size = new System.Drawing.Size(600, 20) };

            // Добавляем на форму
            Controls.Add(btnSelectImage);
            Controls.Add(txtText);
            Controls.Add(btnHide);
            Controls.Add(btnExtract);
            Controls.Add(btnShowAllFields);  // ← ДОБАВЛЯЕМ НОВУЮ КНОПКУ
            Controls.Add(txtResult);
            Controls.Add(lblStatus);
        }

        private string _selectedImagePath = "";

        private void BtnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _selectedImagePath = ofd.FileName;
                    lblStatus.Text = $"Выбрано: {System.IO.Path.GetFileName(_selectedImagePath)}";
                }
            }
        }

        private void BtnHide_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!");
                return;
            }

            if (txtText.Text.Length < 1024)
            {
                MessageBox.Show($"Текст слишком короткий! Нужно минимум 1024 символа. Сейчас: {txtText.Text.Length} символов.\n\nДобавьте текст или используйте кнопку 'Сгенерировать 1 Кб'");
                return;
            }

            lblStatus.Text = "Скрываю данные...";
            var model = new Model(); // Временно напрямую, потом через Controller
            model.HideData(_selectedImagePath, txtText.Text);
            lblStatus.Text = "Готово";
        }

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!");
                return;
            }

            lblStatus.Text = "Извлекаю данные...";
            var model = new Model();
            string extracted = model.ExtractData(_selectedImagePath);
            txtResult.Text = extracted;
            lblStatus.Text = "Извлечение завершено";
        }

        // НОВЫЙ ОБРАБОТЧИК для кнопки "Показать все поля"
        private void BtnShowAllFields_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!");
                return;
            }

            lblStatus.Text = "Читаю EXIF поля...";
            var model = new Model();
            string result = model.GetAllExifFields(_selectedImagePath);
            txtResult.Text = result;

            // Подсчитываем количество полей для статуса
            int fieldCount = result.Split(new[] { "🔹" }, StringSplitOptions.None).Length - 1;
            lblStatus.Text = $"Готово. Найдено {fieldCount} полей";
        }

        // События для Controller (если используете)
        public event Action<string, string> HideRequested;
        public event Action<string> ExtractRequested;

        public void ShowExtractedData(string data)
        {
            txtResult.Text = data;
        }
    }
}