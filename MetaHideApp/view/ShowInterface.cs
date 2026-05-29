using MetaHide.model;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace MetaHide.view;

public partial class View
{
    public Guna2Panel MainPanel;
    public bool ChosenStatus;
    private Label cancel;

    // Элементы для шифрования и сжатия
    private Guna2ComboBox encryptionComboBox;
    private Guna2TextBox passwordTextBox;
    private Guna2CheckBox compressionCheckBox;
    private Guna2TextBox thresholdTextBox;

    // Элемент для выбора метода
    private Guna2ComboBox methodComboBox;

    private void CreateInterface()
    {
        // ========== ХЕДЕР ==========
        var header = new Guna2Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            FillColor = Color.White,
            BorderRadius = 15,
            ShadowDecoration = { Enabled = true, Shadow = new Padding(0, 0, 0, 2) },
            Padding = new Padding(20, 10, 20, 10)
        };

        var title = new Label
        {
            Text = "🔒 MetaHide",
            ForeColor = ColorTranslator.FromHtml("#444444"),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var version = new Label
        {
            Text = "Версия 1.0",
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Segoe UI", 10),
            AutoSize = true,
            Location = new Point(header.Width - 80, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.Add(title);
        header.Controls.Add(version);

        // ========== ЛЕВАЯ ПАНЕЛЬ (МЕТОДЫ) ==========
        var methodPanel = new Guna2Panel
        {
            Size = new Size(340, 320),
            Location = new Point(20, 70),
            FillColor = Color.White,
            BorderRadius = 20,
            ShadowDecoration = { Enabled = true, Depth = 10, Shadow = new Padding(0, 0, 10, 10) },
            Padding = new Padding(20)
        };

        var methodLabel = new Label
        {
            Text = "Метод скрытия",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Location = new Point(20, 10)
        };
        methodPanel.Controls.Add(methodLabel);

        var mlabel2 = new Label
        {
            Text = "Сначала выберите изображение",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            AutoSize = true,
            Location = new Point(20, 45)
        };
        methodPanel.Controls.Add(mlabel2);

        methodComboBox = new Guna2ComboBox
        {
            Location = new Point(20, 75),
            Size = new Size(280, 30),
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            BorderColor = Color.LightGray,
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };
        methodComboBox.Items.Add("Сначала выберите файл");
        methodComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (methodComboBox.SelectedIndex == -1 || methodComboBox.SelectedItem == null) return;
            string selectedText = methodComboBox.SelectedItem.ToString();
            string methodType = selectedText.Contains("Обычный") ? "exif" :
                               selectedText.Contains("Скрытый") && !selectedText.Contains("LSB") ? "marker" :
                               selectedText.Contains("LSB") ? "lsb" :
                               selectedText.Contains("GIF") ? "gif" : "exif";
            MethodTypeChanged?.Invoke(methodType);
            ModeChangedRequested?.Invoke(methodType == "marker");
            UpdateStatus($"Метод: {methodComboBox.Text}");
        };
        methodPanel.Controls.Add(methodComboBox);

        var encryptionLabel = new Label
        {
            Text = "Шифрование",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Location = new Point(20, 130)
        };
        methodPanel.Controls.Add(encryptionLabel);

        encryptionComboBox = new Guna2ComboBox
        {
            Location = new Point(20, 160),
            Size = new Size(280, 30),
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        encryptionComboBox.Items.AddRange(new object[] { "Без шифрования", "XOR", "AES-128", "AES-256" });
        encryptionComboBox.SelectedIndex = 0;
        encryptionComboBox.SelectedIndexChanged += (s, e) =>
        {
            var type = encryptionComboBox.SelectedIndex switch
            {
                1 => EncryptionModel.EncryptionType.XOR,
                2 => EncryptionModel.EncryptionType.AES128,
                3 => EncryptionModel.EncryptionType.AES256,
                _ => EncryptionModel.EncryptionType.None
            };
            EncryptionSettingsChanged?.Invoke(type, passwordTextBox.Text);
        };
        methodPanel.Controls.Add(encryptionComboBox);

        var passwordLabel = new Label
        {
            Text = "Пароль",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Location = new Point(20, 200)
        };
        methodPanel.Controls.Add(passwordLabel);

        passwordTextBox = new Guna2TextBox
        {
            Location = new Point(20, 225),
            Size = new Size(280, 30),
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            PasswordChar = '*',
            PlaceholderText = "Введите пароль",
            Font = new Font("Segoe UI", 10)
        };
        passwordTextBox.TextChanged += (s, e) =>
        {
            var type = encryptionComboBox.SelectedIndex switch
            {
                1 => EncryptionModel.EncryptionType.XOR,
                2 => EncryptionModel.EncryptionType.AES128,
                3 => EncryptionModel.EncryptionType.AES256,
                _ => EncryptionModel.EncryptionType.None
            };
            EncryptionSettingsChanged?.Invoke(type, passwordTextBox.Text);
        };
        methodPanel.Controls.Add(passwordTextBox);

        // ========== ПАНЕЛЬ ЛОГОВ ==========
        var logPanel = new Guna2Panel
        {
            Size = new Size(340, 200),
            Location = new Point(20, 400),
            FillColor = ColorTranslator.FromHtml("#EEF2FF"),
            BorderRadius = 20,
            ShadowDecoration = { Enabled = true, Depth = 8, Shadow = new Padding(0, 0, 8, 8) },
            Padding = new Padding(15)
        };

        compressionCheckBox = new Guna2CheckBox
        {
            Text = "Включить сжатие",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Location = new Point(15, 15),
            AutoSize = true
        };
        compressionCheckBox.CheckedChanged += (s, e) =>
        {
            int threshold = int.TryParse(thresholdTextBox.Text, out int t) ? t : 1;
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };
        logPanel.Controls.Add(compressionCheckBox);

        var thresholdLabel = new Label
        {
            Text = "Порог (КБ):",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#555555"),
            Location = new Point(15, 50),
            AutoSize = true
        };
        logPanel.Controls.Add(thresholdLabel);

        thresholdTextBox = new Guna2TextBox
        {
            Location = new Point(100, 47),
            Size = new Size(60, 25),
            BorderRadius = 8,
            FillColor = Color.White,
            Text = "1",
            Font = new Font("Segoe UI", 9)
        };
        thresholdTextBox.TextChanged += (s, e) =>
        {
            int threshold = int.TryParse(thresholdTextBox.Text, out int t) ? t : 1;
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };
        logPanel.Controls.Add(thresholdTextBox);

        var instructButton = new Guna2Button
        {
            Text = "📖 Инструкция",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            BorderRadius = 15,
            Size = new Size(140, 35),
            Location = new Point(15, 90),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        instructButton.HoverState.FillColor = ColorTranslator.FromHtml("#E2E8F0");
        logPanel.Controls.Add(instructButton);

        var logButton = new Guna2Button
        {
            Text = "📜 Журнал логов",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            BorderRadius = 15,
            Size = new Size(140, 35),
            Location = new Point(170, 90),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        logButton.Click += (s, e) =>
        {
            string path = Path.Combine(Application.StartupPath, "metahide.log");
            if (File.Exists(path)) Process.Start("notepad.exe", path);
            else MessageBox.Show($"Файл не найден: {path}");
        };
        logButton.HoverState.FillColor = ColorTranslator.FromHtml("#E2E8F0");
        logPanel.Controls.Add(logButton);

        var clearLogButton = new Guna2Button
        {
            Text = "Очистить лог",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#DC2626"),
            BorderRadius = 15,
            Size = new Size(140, 35),
            Location = new Point(15, 135),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        clearLogButton.Click += (s, e) =>
        {
            string path = Path.Combine(Application.StartupPath, "metahide.log");
            try { if (File.Exists(path)) File.WriteAllText(path, string.Empty); MessageBox.Show("Лог очищен", "Успех"); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        };
        clearLogButton.HoverState.FillColor = ColorTranslator.FromHtml("#FEE2E2");
        logPanel.Controls.Add(clearLogButton);

        var testButton = new Guna2Button
        {
            Text = "🧪 Запустить тесты",
            FillColor = ColorTranslator.FromHtml("#3B82F6"),
            ForeColor = Color.White,
            BorderRadius = 15,
            Size = new Size(140, 35),
            Location = new Point(170, 135),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        testButton.Click += (s, e) =>
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try { new MetaHide.tests.SteganographyTester().RunTests(); MessageBox.Show("Тесты завершены!"); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
            });
        };
        testButton.HoverState.FillColor = ColorTranslator.FromHtml("#2563EB");
        logPanel.Controls.Add(testButton);

        // ========== ЦЕНТРАЛЬНАЯ PANEL (DROP) ==========
        MainPanel = new Guna2Panel
        {
            AllowDrop = true,
            Location = new Point(380, 70),
            Size = new Size(780, 490),
            FillColor = Color.White,
            BorderRadius = 25,
            ShadowDecoration = { Enabled = true, Depth = 12, Shadow = new Padding(0, 0, 12, 12) }
        };
        MainWindow();

        var dropImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(290, 100),
            Image = Image.FromFile("imgs/SVG.png")
        };
        var dropText = new Label
        {
            Location = new Point(200, 200),
            Text = "Перетащите файл сюда",
            Font = new Font("Segoe UI", 18),
            ForeColor = Color.Gray,
            AutoSize = true
        };
        btnSelectImage = new Guna2Button
        {
            Text = "Выберите файл",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BorderRadius = 25,
            Font = new Font("Segoe UI", 11),
            Size = new Size(180, 50),
            Location = new Point(290, 250),
            Cursor = Cursors.Hand
        };
        btnSelectImage.Click += (s, e) => SelectFile();
        MainPanel.Controls.Add(dropImage);
        MainPanel.Controls.Add(dropText);
        MainPanel.Controls.Add(btnSelectImage);

        // ========== ФУТЕР ==========
        var footer = new Guna2Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            FillColor = Color.White,
            BorderRadius = 15,
            ShadowDecoration = { Enabled = true, Shadow = new Padding(0, -2, 0, 0) },
            Padding = new Padding(20, 10, 20, 10)
        };

        var footerFile = new Guna2Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FillColor = ColorTranslator.FromHtml("#F8FAFC"),
            BorderRadius = 10,
            Padding = new Padding(10, 5, 10, 5)
        };
        lblStatus = new Label
        {
            Text = "Файл — не выбран",
            Dock = DockStyle.Top,
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true
        };
        var t1 = new Label
        {
            Text = "Статус: ожидание загрузки",
            Dock = DockStyle.Top,
            ForeColor = ColorTranslator.FromHtml("#888888"),
            Font = new Font("Segoe UI", 8),
            AutoSize = true
        };
        footerFile.Controls.Add(t1);
        footerFile.Controls.Add(lblStatus);

        btnHide = new Guna2Button
        {
            Text = "Зашифровать",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BorderRadius = 30,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            Size = new Size(480, 45),
            Location = new Point(30, 45),
            Cursor = Cursors.Hand
        };
        btnHide.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
                MessageBox.Show("Сначала выберите изображение!", "Внимание");
            else TextWindow();
        };
        btnExtract = new Guna2Button
        {
            Text = "Расшифровать",
            FillColor = Color.WhiteSmoke,
            ForeColor = Color.Black,
            BorderRadius = 30,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            Size = new Size(520, 45),
            Location = new Point(530, 45),
            Cursor = Cursors.Hand
        };
        btnExtract.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
                MessageBox.Show("Сначала выберите изображение!", "Внимание");
            else ExtractRequested?.Invoke(_selectedImagePath);
        };

        footer.Controls.Add(footerFile);
        footer.Controls.Add(btnHide);
        footer.Controls.Add(btnExtract);

        // ========== ДОБАВЛЯЕМ ВСЁ НА ФОРМУ ==========
        form.Controls.Add(MainPanel);
        form.Controls.Add(footer);
        form.Controls.Add(methodPanel);
        form.Controls.Add(logPanel);
        form.Controls.Add(header);

        // Скругление самой формы
        form.Paint += (sender, e) =>
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int radius = 20;
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(form.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(form.Width - radius, form.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, form.Height - radius, radius, radius, 90, 90);
                form.Region = new Region(path);
            }
        };
    }

    // Обновление доступных методов в зависимости от типа файла
    private void UpdateAvailableMethods(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        methodComboBox.Items.Clear();
        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
                methodComboBox.Items.AddRange(new[] { "Обычный (видно в свойствах)", "Скрытый (маркер в конец)" });
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                break;
            case ".png":
                methodComboBox.Items.AddRange(new[] { "Обычный (видно в свойствах)", "Скрытый (маркер в конец)", "LSB (в пикселях)" });
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                break;
            case ".bmp":
                methodComboBox.Items.AddRange(new[] { "LSB (в пикселях)", "Скрытый (маркер в конец)" });
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                break;
            case ".gif":
                methodComboBox.Items.AddRange(new[] { "Скрытый (маркер в конец)", "GIF (комментарий)" });
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                break;
            default:
                methodComboBox.Items.Add("Формат не поддерживается");
                methodComboBox.Enabled = false;
                break;
        }
    }

    private void MainWindow()
    {
        MainPanel.Controls.Clear();
        MainPanel.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
        MainPanel.DragDrop += (s, e) =>
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                string ext = Path.GetExtension(files[0]).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    _selectedImagePath = files[0];
                    UpdateStatus($"Выбран файл: {Path.GetFileName(files[0])}");
                    ChosenStatus = true;
                    UpdateAvailableMethods(files[0]);
                    SelectedFileWindow();
                }
                else UpdateStatus("Пожалуйста, выберите JPG, PNG, BMP или GIF файл");
            }
        };
        var image = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(290, 100), Image = Image.FromFile("imgs/SVG.png") };
        var text = new Label { Location = new Point(200, 200), Text = "Перетащите файл сюда", Font = new Font("Segoe UI", 18), ForeColor = Color.Gray, AutoSize = true };
        btnSelectImage = new Guna2Button
        {
            Text = "Выберите файл",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BorderRadius = 25,
            Font = new Font("Segoe UI", 11),
            Size = new Size(180, 50),
            Location = new Point(290, 250),
            Cursor = Cursors.Hand
        };
        btnSelectImage.Click += (s, e) => SelectFile();
        MainPanel.Controls.Add(image);
        MainPanel.Controls.Add(text);
        MainPanel.Controls.Add(btnSelectImage);
    }

    private void SelectFile()
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                SetSelectedImagePath(ofd.FileName);
                UpdateStatus($"Выбран файл: {Path.GetFileName(ofd.FileName)}");
                ChosenStatus = true;
                UpdateAvailableMethods(ofd.FileName);
                SelectedFileWindow();
            }
        }
    }

    private void SelectedFileWindow()
    {
        if (!ChosenStatus) return;
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(0, 130, 0, 0);
        var image = new PictureBox { Dock = DockStyle.Top, Image = Image.FromFile("imgs/chosen.png"), SizeMode = PictureBoxSizeMode.CenterImage };
        var text = new Label
        {
            Dock = DockStyle.Top,
            Text = lblStatus.Text.Length > 36 ? lblStatus.Text.Substring(13, 23) + "..." : (lblStatus.Text.Length > 13 ? lblStatus.Text.Substring(13) : ""),
            Height = 50,
            Font = new Font("Segoe UI", 20),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };
        var label = new Label
        {
            Dock = DockStyle.Top,
            Text = "Выберите действие в нижней панели управления",
            Height = 50,
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };
        var buttonContainer = new Panel { Dock = DockStyle.Top, Height = 70 };
        btnSelectImage = new Guna2Button
        {
            Text = "Заменить файл",
            Size = new Size(180, 50),
            FillColor = ColorTranslator.FromHtml("#DDDDDD"),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            BorderRadius = 25,
            Font = new Font("Segoe UI", 11),
            Location = new Point((buttonContainer.Width - 180) / 2, 10),
            Anchor = AnchorStyles.Top
        };
        btnSelectImage.Click += (s, e) => SelectFile();
        buttonContainer.Controls.Add(btnSelectImage);
        MainPanel.Controls.Add(buttonContainer);
        MainPanel.Controls.Add(label);
        MainPanel.Controls.Add(text);
        MainPanel.Controls.Add(image);
    }

    public void TextWindow()
    {
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);
        var label = new Label { Text = "Скрытое сообщение", Font = new Font("Segoe UI", 14, FontStyle.Bold), Width = 500, Height = 40, Dock = DockStyle.Top };
        txt = new Guna2TextBox
        {
            PlaceholderText = "Введите текст, который нужно скрыть в файле...",
            Dock = DockStyle.Top,
            Height = 330,
            Multiline = true,
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke
        };
        cancel = new Label { Text = "Отмена", ForeColor = Color.Gray, Font = new Font("Segoe UI", 11), AutoSize = true, Dock = DockStyle.Top, Cursor = Cursors.Hand };
        txt.KeyDown += (e, s) =>
        {
            if (s.KeyCode == Keys.Enter)
            {
                var type = GetSelectedEncryptionType();
                var pass = GetPassword();
                EncryptionSettingsChanged?.Invoke(type, pass);
                HideRequested?.Invoke(_selectedImagePath, txt.Text);
            }
        };
        cancel.Click += (e, s) => MainWindow();
        MainPanel.Controls.Add(cancel);
        MainPanel.Controls.Add(txt);
        MainPanel.Controls.Add(label);
    }

    public void ShowExtractedData(string data)
    {
        if (MainPanel.InvokeRequired) { MainPanel.Invoke((MethodInvoker)(() => ShowExtractedData(data))); return; }
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);
        var label = new Label { Text = "Извлечённое сообщение", Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Top, Height = 40 };
        var textBox = new Guna2TextBox
        {
            Text = string.IsNullOrEmpty(data) ? "Данные не найдены" : data,
            Dock = DockStyle.Top,
            Height = 330,
            Multiline = true,
            ReadOnly = true,
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            ScrollBars = ScrollBars.Vertical
        };
        var backButton = new Guna2Button
        {
            Text = "Назад",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BorderRadius = 17,
            Font = new Font("Segoe UI", 11),
            Size = new Size(120, 35),
            Location = new Point(330, 380),
            Cursor = Cursors.Hand
        };
        backButton.Click += (s, e) => MainWindow();
        MainPanel.Controls.Add(backButton);
        MainPanel.Controls.Add(textBox);
        MainPanel.Controls.Add(label);
    }

    private EncryptionModel.EncryptionType GetSelectedEncryptionType()
    {
        if (encryptionComboBox == null) return EncryptionModel.EncryptionType.None;
        return encryptionComboBox.SelectedIndex switch
        {
            1 => EncryptionModel.EncryptionType.XOR,
            2 => EncryptionModel.EncryptionType.AES128,
            3 => EncryptionModel.EncryptionType.AES256,
            _ => EncryptionModel.EncryptionType.None
        };
    }
    private string GetPassword() => passwordTextBox?.Text ?? "";
}