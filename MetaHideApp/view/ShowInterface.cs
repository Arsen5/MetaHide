using MetaHide.model;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MetaHide.view;

public partial class View
{
    public Panel MainPanel;
    public bool ChosenStatus;
    private Label cancel;

    // Элементы для шифрования и сжатия
    private ComboBox encryptionComboBox;
    private TextBox passwordTextBox;
    private CheckBox compressionCheckBox;
    private TextBox thresholdTextBox;

    // Элемент для выбора метода
    private ComboBox methodComboBox;

    private void CreateInterface()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.White,
            Padding = new Padding(40, 12, 40, 12),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var title = new Label
        {
            Text = "🔒 MetaHide",
            ForeColor = ColorTranslator.FromHtml("#444444"),
            Font = new Font("Inter", 12, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var status = new Label
        {
            Text = "Версия 1.0",
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Inter", 12),
            AutoSize = true,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight
        };

        // ========== ВЫБОР МЕТОДА ==========
        var methodPanel = new FlowLayoutPanel
        {
            Size = new Size(320, 280),
            Location = new Point(30, 60),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        var methodLabel = new Label
        {
            Text = "Метод скрытия:",
            Font = new Font("Inter", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true
        };

        var mlabel2 = new Label
        {
            Text = "Сначала выберите изображение",
            Font = new Font("Inter", 11),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            AutoSize = true,
        };

        methodComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false,
            Size = new Size(260, 25),
            Font = new Font("Inter", 10)
        };
        methodComboBox.Items.Add("Сначала выберите файл");

        methodComboBox.SelectedIndexChanged += (s, e) =>
        {
            int index = methodComboBox.SelectedIndex;
            string selectedText = methodComboBox.SelectedItem.ToString();
            string methodType;

            if (selectedText.Contains("Обычный"))
                methodType = "exif";
            else if (selectedText.Contains("Скрытый") && !selectedText.Contains("LSB"))
                methodType = "marker";
            else if (selectedText.Contains("LSB"))
                methodType = "lsb";
            else if (selectedText.Contains("GIF"))
                methodType = "gif";
            else
                methodType = "exif";

            MethodTypeChanged?.Invoke(methodType);

            bool isHidden = (methodType == "marker");
            ModeChangedRequested?.Invoke(isHidden);

            UpdateStatus($"Метод: {methodComboBox.Text}");
        };

        var encryptionLabel = new Label
        {
            Text = "Шифрование:",
            Font = new Font("Inter", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        encryptionComboBox = new ComboBox
        {
            Size = new Size(220, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 0, 10)
        };
        encryptionComboBox.Items.AddRange(new object[] { "Без шифрования", "XOR", "AES-128", "AES-256" });
        encryptionComboBox.SelectedIndex = 0;

        var passwordLabel = new Label
        {
            Text = "Пароль:",
            Font = new Font("Inter", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        passwordTextBox = new TextBox
        {
            Size = new Size(150, 25),
            PasswordChar = '*',
            PlaceholderText = "Введите пароль",
            Margin = new Padding(0, 0, 0, 10)
        };

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
        // ========== LOG PANEL ==========
        var logPanel = new TableLayoutPanel
        {
            Size = new Size(320, 160),
            Location = new Point(30, 350),
            BackColor = ColorTranslator.FromHtml("#EEF2FF"),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20, 10, 20, 10),
            ColumnCount = 2,  // ← ДВЕ КОЛОНКИ
            RowCount = 4
        };

        // Стили колонок
        logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // левая
        logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // правая

        // Стили строк
        logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        // compressionCheckBox (занимает обе колонки)
        compressionCheckBox = new CheckBox
        {
            Text = "Включить сжатие",
            Font = new Font("Inter", 13, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true
        };
        logPanel.Controls.Add(compressionCheckBox, 0, 0);
        logPanel.SetColumnSpan(compressionCheckBox, 2);  // ← растянуть на 2 колонки

        // instructButton (левая колонка)
        var instructButton = new Button
        {
            Text = "📖 Инструкция",
            BackColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            Font = new Font("Inter", 11),
            Size = new Size(150, 30),
            Cursor = Cursors.Hand
        };
        logPanel.Controls.Add(instructButton, 0, 1);

        // logButton (левая колонка)
        var logButton = new Button
        {
            Text = "📜 Журнал логов",
            BackColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            Font = new Font("Inter", 11),
            Size = new Size(150, 30),
            Cursor = Cursors.Hand
        };

        logButton.Click += (s, e) =>
        {
            var path = Path.Combine(Application.StartupPath, "metahide.log");
            if (File.Exists(path))
                Process.Start("notepad.exe", path);
            else
                MessageBox.Show($"Файл не найден: {path}");
        };

        logPanel.Controls.Add(logButton, 0, 2);

        // ========== НОВАЯ КНОПКА ОЧИСТКИ (правая колонка) ==========
        var clearLogButton = new Button
        {
            Text = "Очистить",
            BackColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#DC2626"),
            Font = new Font("Inter", 11),
            Size = new Size(100, 30),
            Cursor = Cursors.Hand
        };

        clearLogButton.Click += (s, e) =>
        {
            var path = Path.Combine(Application.StartupPath, "metahide.log");
            try
            {
                if (File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty);
                    MessageBox.Show("Лог очищен", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        };

        logPanel.Controls.Add(clearLogButton, 1, 2);  // ← колонка 1, строка 2

        // testButton (левая колонка)
        var testButton = new Button
        {
            Text = "Запустить тесты",
            BackColor = ColorTranslator.FromHtml("#2196F3"),
            ForeColor = Color.White,
            Font = new Font("Inter", 11),
            Size = new Size(150, 30),
            Cursor = Cursors.Hand
        };

        testButton.Click += (s, e) =>
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var tester = new MetaHide.tests.SteganographyTester();
                    tester.RunTests();
                    MessageBox.Show("Тесты завершены! Проверьте консоль.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            });
        };


        // ========== DROP PANEL ==========
        MainPanel = new Panel
        {
            AllowDrop = true,
            Location = new Point(375, 60),
            Size = new Size(790, 450),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };

        MainWindow();

        var image = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(345, 100),
            Image = Image.FromFile("imgs/SVG.png")
        };

        var text = new Label
        {
            Location = new Point(230, 200),
            Text = "Перетащите файл сюда",
            Font = new Font("Inter", 20),
            ForeColor = Color.Black,
            AutoSize = true
        };

        btnSelectImage = new Button
        {
            Text = "Выберите файл",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 11),
            Size = new Size(160, 50),
            Location = new Point(305, 250),
            Cursor = Cursors.Hand
        };
        btnSelectImage.Click += (s, e) => SelectFile();

        // ========== FOOTER ==========
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 120,
            BackColor = Color.White,
            Padding = new Padding(40, 10, 40, 20),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var footerFile = new Panel
        {
            Dock = DockStyle.Top,
            Size = new Size(1000, 40),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 2, 10, 2)
        };

        lblStatus = new Label
        {
            Text = "Файл — не выбран",
            Dock = DockStyle.Top,
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Inter", 9, FontStyle.Bold),
            AutoSize = true,
        };

        var t1 = new Label
        {
            Text = "Статус: ожидание загрузки",
            Dock = DockStyle.Top,
            ForeColor = ColorTranslator.FromHtml("#888888"),
            Font = new Font("Inter", 8),
            AutoSize = true,
        };

        btnHide = new Button
        {
            Text = "Зашифровать",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 13),
            Size = new Size(500, 50),
            Location = new Point(38, 60),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnHide.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            TextWindow();
        };

        btnExtract = new Button
        {
            Text = "Расшифровать",
            BackColor = Color.WhiteSmoke,
            ForeColor = Color.Black,
            Font = new Font("Inter", 13),
            Size = new Size(550, 50),
            Location = new Point(593, 60),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExtract.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("Сначала выберите изображение!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ExtractRequested?.Invoke(_selectedImagePath);
        };

        // ========== СБОРКА ==========
        header.Controls.Add(title);
        header.Controls.Add(status);

        methodPanel.Controls.Add(methodLabel);
        methodPanel.Controls.Add(mlabel2);
        methodPanel.Controls.Add(methodComboBox);
        methodPanel.Controls.Add(encryptionLabel);
        methodPanel.Controls.Add(encryptionComboBox);
        methodPanel.Controls.Add(passwordLabel);
        methodPanel.Controls.Add(passwordTextBox);

        logPanel.Controls.Add(compressionCheckBox, 0, 0);
        logPanel.Controls.Add(instructButton, 0, 1);
        logPanel.Controls.Add(testButton, 0, 3);

        footer.Controls.Add(footerFile);
        footer.Controls.Add(btnHide);
        footer.Controls.Add(btnExtract);
        footerFile.Controls.Add(t1);
        footerFile.Controls.Add(lblStatus);

        form.Controls.Add(MainPanel);
        form.Controls.Add(footer);
        form.Controls.Add(methodPanel);
        form.Controls.Add(logPanel);
        form.Controls.Add(header);
        
        // ========== СЖАТИЕ ==========

        var thresholdLabel = new Label
        {
            Text = "Порог (КБ):",
            Font = new Font("Inter", 11),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(10, 40)
        };

        thresholdTextBox = new TextBox
        {
            Location = new Point(100, 38),
            Size = new Size(50, 25),
            Text = "1"
        };

        compressionCheckBox.CheckedChanged += (s, e) =>
        {
            int threshold = 1;
            int.TryParse(thresholdTextBox.Text, out threshold);
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };

        thresholdTextBox.TextChanged += (s, e) =>
        {
            int threshold = 1;
            int.TryParse(thresholdTextBox.Text, out threshold);
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };
    }

    // Метод обновления доступных методов в зависимости от типа файла
    private void UpdateAvailableMethods(string filePath)
    {
        string ext = System.IO.Path.GetExtension(filePath).ToLower();

        methodComboBox.Items.Clear();

        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
                methodComboBox.Items.Add("Обычный (видно в свойствах)");
                methodComboBox.Items.Add("Скрытый (маркер в конец)");
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                MethodTypeChanged?.Invoke("exif");
                break;

            case ".png":
                methodComboBox.Items.Add("Обычный (видно в свойствах)");
                methodComboBox.Items.Add("Скрытый (маркер в конец)");
                methodComboBox.Items.Add("LSB (в пикселях)");
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                MethodTypeChanged?.Invoke("exif");
                break;

            case ".bmp":
                methodComboBox.Items.Add("LSB (в пикселях)");
                methodComboBox.Items.Add("Скрытый (маркер в конец)");
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                MethodTypeChanged?.Invoke("lsb");
                break;

            case ".gif":
                methodComboBox.Items.Add("Скрытый (маркер в конец)");
                methodComboBox.Items.Add("GIF (комментарий)");
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = true;
                MethodTypeChanged?.Invoke("marker");
                break;

            default:
                methodComboBox.Items.Add("Формат не поддерживается");
                methodComboBox.SelectedIndex = 0;
                methodComboBox.Enabled = false;
                break;
        }
    }

    private void MainWindow()
    {
        MainPanel.Controls.Clear();
        MainPanel.DragEnter += (s, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        };

        MainPanel.DragDrop += (s, e) =>
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                string ext = System.IO.Path.GetExtension(files[0]).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    _selectedImagePath = files[0];
                    UpdateStatus($"Выбран файл: {System.IO.Path.GetFileName(files[0])}");
                    ChosenStatus = true;

                    // Обновляем доступные методы
                    UpdateAvailableMethods(files[0]);

                    SelectedFileWindow();
                }
                else
                {
                    UpdateStatus("Пожалуйста, выберите JPG, PNG, BMP или GIF файл");
                }
            }
        };

        var image = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(345, 100),
            Image = Image.FromFile("imgs/SVG.png")
        };

        var text = new Label
        {
            Location = new Point(230, 200),
            Text = "Перетащите файл сюда",
            Font = new Font("Inter", 20),
            ForeColor = Color.Black,
            AutoSize = true
        };

        btnSelectImage = new Button
        {
            Text = "Выберите файл",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 11),
            Size = new Size(160, 50),
            Location = new Point(305, 250),
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
            ofd.Title = "Выберите изображение";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                SetSelectedImagePath(ofd.FileName);
                UpdateStatus($"Выбран файл: {System.IO.Path.GetFileName(ofd.FileName)}");
                ChosenStatus = true;

                // Обновляем доступные методы
                UpdateAvailableMethods(ofd.FileName);

                SelectedFileWindow();
            }
        }
    }

    private void SelectedFileWindow()
    {
        if (ChosenStatus)
        {
            MainPanel.Controls.Clear();
            MainPanel.Padding = new Padding(0, 130, 0, 0);
            var image = new PictureBox
            {
                Dock = DockStyle.Top,
                Image = Image.FromFile("imgs/chosen.png"),
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            var text = new Label
            {
                Dock = DockStyle.Top,
                Text = lblStatus.Text.Length > 36
                   ? lblStatus.Text.Substring(13, 23) + "..."
                   : (lblStatus.Text.Length > 13 ? lblStatus.Text.Substring(13) : ""),
                Height = 50,
                Font = new Font("Inter", 20),
                ForeColor = Color.Black,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Text = "Выберите действие в нижней панели управления",
                Height = 50,
                Font = new Font("Inter", 13),
                ForeColor = Color.Gray,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Panel buttonContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
            };

            btnSelectImage = new Button
            {
                Text = "Заменить файл",
                Size = new Size(160, 50),
                BackColor = ColorTranslator.FromHtml("#DDDDDD"),
                ForeColor = ColorTranslator.FromHtml("#666666"),
                Font = new Font("Inter", 11),
                Location = new Point((buttonContainer.Width - 160) / 2, 10),
                Anchor = AnchorStyles.Top
            };
            btnSelectImage.Click += (s, e) => SelectFile();
            buttonContainer.Controls.Add(btnSelectImage);

            MainPanel.Controls.Add(buttonContainer);
            MainPanel.Controls.Add(label);
            MainPanel.Controls.Add(text);
            MainPanel.Controls.Add(image);
        }
    }

    public void TextWindow()
    {
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);

        var label = new Label
        {
            Text = "Скрытое сообщение",
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Inter", 14),
            Width = 500,
            Height = 40,
            Dock = DockStyle.Top
        };

        txt = new TextBox
        {
            PlaceholderText = "Введите текст, который нужно скрыть в файле...",
            Dock = DockStyle.Top,
            Height = 330,
            Multiline = true,
        };

        cancel = new Label
        {
            Text = "Отмена",
            ForeColor = ColorTranslator.FromHtml("#888888"),
            Font = new Font("Inter", 11),
            AutoSize = true,
            Dock = DockStyle.Top,
            Cursor = Cursors.Hand
        };

        txt.KeyDown += (e, s) =>
        {
            if (s.KeyCode == Keys.Enter)
            {
                var encryptionType = GetSelectedEncryptionType();
                var password = GetPassword();
                EncryptionSettingsChanged?.Invoke(encryptionType, password);
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
        if (MainPanel.InvokeRequired)
        {
            MainPanel.Invoke((MethodInvoker)(() => ShowExtractedData(data)));
            return;
        }

        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);

        var label = new Label
        {
            Text = "Извлечённое сообщение",
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Inter", 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40
        };

        var textBox = new TextBox
        {
            Text = string.IsNullOrEmpty(data) ? "Данные не найдены" : data,
            Dock = DockStyle.Top,
            Height = 330,
            Multiline = true,
            ReadOnly = true,
            Font = new Font("Inter", 11),
            ScrollBars = ScrollBars.Vertical
        };

        var backButton = new Button
        {
            Text = "Назад",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 11),
            Size = new Size(120, 35),
            Location = new Point(330, 380),
            FlatStyle = FlatStyle.Flat,
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

    private string GetPassword()
    {
        return passwordTextBox?.Text ?? "";
    }
} 