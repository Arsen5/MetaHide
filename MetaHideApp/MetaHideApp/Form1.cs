using System;
using System.Windows.Forms;

namespace test
{
    public partial class Form1 : Form
    {
        // События для Controller
        public event Action<string, string> HideRequested;
        public event Action<string> ExtractRequested;
        public event Action<string> ShowAllFieldsRequested;
        public event Action<bool> ModeChangedRequested;

        // Элементы управления
        private Button btnSelectImage;
        private Button btnHide;
        private Button btnExtract;
        private Button btnShowAllFields;
        private TextBox txtText;
        private RichTextBox txtResult;
        private Label lblStatus;
        private RadioButton rbVisibleMode;
        private RadioButton rbHiddenMode;

        private string _selectedImagePath = "";

        public Form1()
        {
            InitializeComponent();
            CreateTestControls();
        }

        public void ShowExtractedData(string data)
        {
            txtResult.Text = data;
        }

        public void UpdateStatus(string message)
        {
            lblStatus.Text = message;
        }

        private void CreateTestControls()
        {
            // Кнопка выбора изображения
            btnSelectImage = new Button
            {
                Text = "Выбрать изображение",
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(150, 30)
            };
            btnSelectImage.Click += BtnSelectImage_Click;

            // Поле для текста
            txtText = new TextBox
            {
                Location = new System.Drawing.Point(12, 50),
                Size = new System.Drawing.Size(400, 100),
                Multiline = true
            };

            // Режимы работы
            rbVisibleMode = new RadioButton
            {
                Text = "Обычный режим (видно в свойствах)",
                Location = new System.Drawing.Point(12, 160),
                Size = new System.Drawing.Size(200, 25),
                Checked = true
            };
            rbVisibleMode.CheckedChanged += RbVisibleMode_CheckedChanged;

            rbHiddenMode = new RadioButton
            {
                Text = "Скрытый режим (не видно в свойствах)",
                Location = new System.Drawing.Point(12, 185),
                Size = new System.Drawing.Size(200, 25)
            };
            rbHiddenMode.CheckedChanged += RbHiddenMode_CheckedChanged;

            // Кнопка "Спрятать"
            btnHide = new Button
            {
                Text = "Спрятать в EXIF",
                Location = new System.Drawing.Point(12, 220),
                Size = new System.Drawing.Size(150, 30)
            };
            btnHide.Click += BtnHide_Click;

            // Кнопка "Извлечь"
            btnExtract = new Button
            {
                Text = "Извлечь из EXIF",
                Location = new System.Drawing.Point(170, 220),
                Size = new System.Drawing.Size(150, 30)
            };
            btnExtract.Click += BtnExtract_Click;

            // Кнопка "Показать все поля"
            btnShowAllFields = new Button
            {
                Text = "Показать все поля",
                Location = new System.Drawing.Point(330, 220),
                Size = new System.Drawing.Size(150, 30)
            };
            btnShowAllFields.Click += BtnShowAllFields_Click;

            // Поле для результата
            txtResult = new RichTextBox
            {
                Location = new System.Drawing.Point(12, 260),
                Size = new System.Drawing.Size(650, 180),
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9)
            };

            // Статус
            lblStatus = new Label
            {
                Text = "Готов",
                Location = new System.Drawing.Point(12, 450),
                Size = new System.Drawing.Size(600, 20)
            };

            // Добавляем на форму
            Controls.Add(btnSelectImage);
            Controls.Add(txtText);
            Controls.Add(rbVisibleMode);
            Controls.Add(rbHiddenMode);
            Controls.Add(btnHide);
            Controls.Add(btnExtract);
            Controls.Add(btnShowAllFields);
            Controls.Add(txtResult);
            Controls.Add(lblStatus);

            this.Size = new System.Drawing.Size(700, 520);
            this.Text = "MetaHide - Стеганография в EXIF";
        }

        private void RbVisibleMode_CheckedChanged(object sender, EventArgs e)
        {
            if (rbVisibleMode.Checked)
                ModeChangedRequested?.Invoke(false);
        }

        private void RbHiddenMode_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHiddenMode.Checked)
                ModeChangedRequested?.Invoke(true);
        }

        private void BtnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Изображения|*.jpg;*.jpeg";
                ofd.Title = "Выберите изображение";
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


            lblStatus.Text = "Скрываю данные...";
            HideRequested?.Invoke(_selectedImagePath, txtText.Text);
        }

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!");
                return;
            }

            lblStatus.Text = "Извлекаю данные...";
            ExtractRequested?.Invoke(_selectedImagePath);
        }

        private void BtnShowAllFields_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!");
                return;
            }

            lblStatus.Text = "Читаю EXIF поля...";
            ShowAllFieldsRequested?.Invoke(_selectedImagePath);
        }
    }
}