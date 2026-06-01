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
    private InstructionForm _instructionWindow = null;

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
            BorderColor = Color.LightGray,
            BorderThickness = 1,
            // Задаем внутренние отступы: 30 слева/справа, 0 сверху/снизу
            Padding = new Padding(30, 0, 30, 0)
        };

        var img = new PictureBox
        {
            Image = Image.FromFile("imgs/ico.png"),
            BackColor = Color.Transparent,
            // Меняем на Normal, чтобы контролировать размер вручную
            SizeMode = PictureBoxSizeMode.Normal,
            Dock = DockStyle.Left,
            // Растягиваем PictureBox на всю высоту панели (50px)
            Height = 50,
            // Задаем ширину, равную ширине иконки (например, 16px или 24px)
            Width = 16,
            // Центрируем саму картинку внутри PictureBox:
            // 17px сверху и снизу ( (50 высоты - 16 иконки) / 2 = 17 )
            Padding = new Padding(0, 17, 0, 17)
        };

        var title = new Label
        {
            Text = "  MetaHide", // Пробелы слева сделают отступ от иконки
            ForeColor = ColorTranslator.FromHtml("#444444"),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.Transparent,
            Dock = DockStyle.Left,
            // Выключаем AutoSize, чтобы лейбл занял всю доступную высоту и сработало выравнивание
            AutoSize = false,
            Width = 150, // Задайте ширину с запасом под текст
            TextAlign = ContentAlignment.MiddleLeft // Выравнивание по левому краю, но по центру высоты
        };

        var version = new Label
        {
            Text = "Версия 1.0",
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Transparent,
            Dock = DockStyle.Right,
            // Выключаем AutoSize для вертикального центрирования
            AutoSize = false,
            Width = 100, // Задайте ширину с запасом под текст версии
            TextAlign = ContentAlignment.MiddleRight // Выравнивание по правому краю, по центру высоты
        };

        // Важен правильный порядок добавления контролов на панель
        header.Controls.Add(title);   // Вторым слева
        header.Controls.Add(img);     // Самым первым слева
        header.Controls.Add(version); // Справа

        // ========== ЛЕВАЯ ПАНЕЛЬ (МЕТОДЫ) ==========
        var methodPanel = new Guna2Panel
        {
            Size = new Size(340, 115),
            Location = new Point(20, 60),
            FillColor = Color.White,
            BorderRadius = 20,
            BorderColor = Color.LightGray,
            BorderThickness = 1,
            Padding = new Padding(20, 12, 20, 12)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White
        };

        // Настройка строк
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Фиксированная ширина колонки
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));

        var methodLabel = new Label
        {
            Text = "Метод скрытия",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            BackColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        table.Controls.Add(methodLabel, 0, 0);
        var mlabel2 = new Label
        {
            Text = "Сначала выберите изображение",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            BackColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        table.Controls.Add(mlabel2, 0, 1);

        methodComboBox = new Guna2ComboBox
        {
            Size = new Size(280, 30),
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            BorderColor = Color.LightGray,
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false,
            Anchor = AnchorStyles.Left
        };
        methodComboBox.Items.Add("Сначала выберите файл");
        methodComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (methodComboBox.SelectedIndex == -1 || methodComboBox.SelectedItem == null) return;
            string selectedText = methodComboBox.SelectedItem.ToString();
            string methodType;

            if (selectedText.Contains("Обычный"))
                methodType = "exif";
            else if (selectedText.Contains("Скрытый") && !selectedText.Contains("JSteg"))
                methodType = "marker";
            else if (selectedText.Contains("LSB"))
                methodType = "lsb";
            else if (selectedText.Contains("GIF"))
                methodType = "gif";
            else if (selectedText.Contains("JSteg"))
                methodType = "jsteg";
            else
                methodType = "exif";

            // ДОБАВЬТЕ ЭТУ СТРОКУ ДЛЯ ОТЛАДКИ
            MessageBox.Show($"Выбран метод: {methodType}");

            MethodTypeChanged?.Invoke(methodType);
            ModeChangedRequested?.Invoke(methodType == "marker");
            UpdateStatus($"Метод: {methodComboBox.Text}");
        };
        table.Controls.Add(methodComboBox, 0, 2);

        methodPanel.Controls.Add(table);

        // ========== ПАНЕЛЬ ШИФРОВАНИЯ ==========
        var methodPanel2 = new Guna2Panel
        {
            Size = new Size(340, 165),
            Location = new Point(20, 185),
            FillColor = Color.White,
            BorderRadius = 20,
            BorderColor = Color.LightGray,
            BorderThickness = 1,
            Padding = new Padding(20, 12, 20, 12)
        };

        var table2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.White
        };

        // Настройка строк
        table2.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table2.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table2.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table2.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table2.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Фиксированная ширина колонки
        table2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));

        var crypto = new Label
        {
            Text = "Криптозащита",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            BackColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        table2.Controls.Add(crypto, 0, 0);

        var encrypt = new Label
        {
            Text = "Шифрование",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            BackColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        table2.Controls.Add(encrypt, 0, 1);

        encryptionComboBox = new Guna2ComboBox
        {
            Size = new Size(280, 30),
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 0, 2),
            Anchor = AnchorStyles.Left
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
        table2.Controls.Add(encryptionComboBox, 0, 2);

        var passwordLabel = new Label
        {
            Text = "Криптографический пароль",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
            BackColor = Color.White
        };
        table2.Controls.Add(passwordLabel, 0, 3);

        passwordTextBox = new Guna2TextBox
        {
            Size = new Size(275, 25),
            BorderRadius = 7,
            FillColor = Color.WhiteSmoke,
            PasswordChar = '*',
            PlaceholderText = "Введите пароль",
            Font = new Font("Segoe UI", 10),
            Anchor = AnchorStyles.Left
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
        table2.Controls.Add(passwordTextBox, 0, 4);

        methodPanel2.Controls.Add(table2);

        // ========== ПАНЕЛЬ ЛОГОВ ==========
        var logPanel = new Guna2Panel
        {
            Size = new Size(340, 165),
            Location = new Point(20, 360),
            FillColor = ColorTranslator.FromHtml("#EEF2FF"),
            BorderRadius = 20,
            BorderColor = Color.LightGray,
            BorderThickness = 1,
            Padding = new Padding(20, 15, 20, 15)
        };

        var table3 = new TableLayoutPanel
        {
            Size = new Size(300, 140),
            Location = new Point(20, 15),
            ColumnCount = 2,
            RowCount = 4,
            BackColor = ColorTranslator.FromHtml("#EEF2FF"),
            Margin = new Padding(0)
        };

        // Настройка колонок
        table3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        table3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // Настройка строк
        table3.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        table3.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        table3.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        table3.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        // ========== СТРОКА 0: Включить сжатие (слева) + Порог (справа) ==========

        compressionCheckBox = new Guna2CheckBox
        {
            Text = "Включить сжатие",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(5, 0, 0, 0)
        };
        compressionCheckBox.CheckedChanged += (s, e) =>
        {
            int threshold = int.TryParse(thresholdTextBox.Text, out int t) ? t : 1;
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };
        table3.Controls.Add(compressionCheckBox, 0, 0);

        var thresholdPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = ColorTranslator.FromHtml("#EEF2FF")
        };

        var thresholdLabel = new Label
        {
            Text = "Порог (КБ):",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#555555"),
            AutoSize = true,
            Margin = new Padding(0, 5, 5, 0)
        };

        thresholdTextBox = new Guna2TextBox
        {
            Size = new Size(60, 25),
            BorderRadius = 8,
            FillColor = Color.White,
            Text = "1",
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 2, 0, 0)
        };
        thresholdTextBox.TextChanged += (s, e) =>
        {
            int threshold = int.TryParse(thresholdTextBox.Text, out int t) ? t : 1;
            CompressionSettingsChanged?.Invoke(compressionCheckBox.Checked, threshold);
        };

        thresholdPanel.Controls.Add(thresholdLabel);
        thresholdPanel.Controls.Add(thresholdTextBox);
        table3.Controls.Add(thresholdPanel, 1, 0);

        // ========== СТРОКА 1: Инструкция ==========
        var instructButton = new Guna2Button
        {
            Text = "Инструкция",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            BorderRadius = 10,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            Size = new Size(140, 35),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Left
        };
        instructButton.Click += (s, e) =>
        {
            if (_instructionWindow == null || _instructionWindow.IsDisposed)
            {
                _instructionWindow = new InstructionForm();
                _instructionWindow.Show(); // Открывает как отдельное независимое окно
            }
            else
            {
                // Если окно уже открыто, просто выводим его на передний план
                _instructionWindow.BringToFront();
            }
        };
        instructButton.HoverState.FillColor = ColorTranslator.FromHtml("#E2E8F0");
        table3.Controls.Add(instructButton, 0, 1);

        // ========== СТРОКА 2: Журнал логов (слева) + Очистить лог (справа) ==========

        var logButton = new Guna2Button
        {
            Text = "Журнал логов",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#334155"),
            BorderRadius = 10,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            Size = new Size(140, 35),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Left
        };
        logButton.Click += (s, e) =>
        {
            string path = Path.Combine(Application.StartupPath, "metahide.log");
            if (File.Exists(path)) Process.Start("notepad.exe", path);
            else MessageBox.Show($"Файл не найден: {path}");
        };
        logButton.HoverState.FillColor = ColorTranslator.FromHtml("#E2E8F0");
        table3.Controls.Add(logButton, 0, 2);

        var clearLogButton = new Guna2Button
        {
            Text = "Очистить лог",
            FillColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#DC2626"),
            BorderRadius = 10,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            Size = new Size(140, 35),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Right
        };
        clearLogButton.Click += (s, e) =>
        {
            string path = Path.Combine(Application.StartupPath, "metahide.log");
            try { if (File.Exists(path)) File.WriteAllText(path, string.Empty); MessageBox.Show("Лог очищен", "Успех"); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        };
        clearLogButton.HoverState.FillColor = ColorTranslator.FromHtml("#FEE2E2");
        table3.Controls.Add(clearLogButton, 1, 2);

        // ========== СТРОКА 3: Запустить тесты ==========
        var testButton = new Guna2Button
        {
            Text = "Запустить тесты",
            FillColor = ColorTranslator.FromHtml("#3B82F6"),
            ForeColor = Color.White,
            BorderRadius = 10,
            Size = new Size(140, 30),
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            Anchor = AnchorStyles.Left
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
        table3.Controls.Add(testButton, 0, 3);

        logPanel.Controls.Add(table3);

        // ========== ЦЕНТРАЛЬНАЯ PANEL (DROP) ==========
        MainPanel = new Guna2Panel
        {
            AllowDrop = true,
            Location = new Point(380, 60),
            Size = new Size(780, 465),
            FillColor = Color.White,
            BorderRadius = 20,
            BorderColor = Color.LightGray,
            BorderThickness = 1,
        };
        MainWindow();
        // ========== ФУТЕР ==========
        var footer = new Guna2Panel
        {
            Dock = DockStyle.Bottom,
            Height = 120,
            FillColor = Color.White,
            BorderRadius = 15,
            Padding = new Padding(20)
        };

        var footerFile = new Guna2Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FillColor = ColorTranslator.FromHtml("#F8FAFC"),
            BorderRadius = 10,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            BackColor = Color.Transparent,
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
            BorderRadius = 15,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            Size = new Size(570, 45),
            Location = new Point(20, 65),
            BackColor = Color.Transparent,
            BorderThickness = 1,
            BorderColor = Color.LightGray,
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
            BorderRadius = 15,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            Size = new Size(570, 45),
            BorderThickness = 1,
            BorderColor = Color.LightGray,
            BackColor = Color.Transparent,
            Location = new Point(595, 65),
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
        form.Controls.Add(methodPanel2);
        form.Controls.Add(logPanel);
        form.Controls.Add(header);
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
                methodComboBox.Items.AddRange(new[] { "Обычный (видно в свойствах)", "Скрытый (маркер в конец)", "JSteg (DCT)"});
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
        // Очищаем старые элементы управления, чтобы избежать дублирования
        MainPanel.Controls.Clear();

        // Настройка событий Drag & Drop
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

        // 1. ИКОНКА (Явно задаем размер 64x64 для точного расчета координат)
        var image = new PictureBox
        {
            Size = new Size(64, 64),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Image.FromFile("imgs/SVG.png"),
            BackColor = Color.Transparent
        };
        // Позиционирование иконки по центру горизонтали
        image.Location = new Point((MainPanel.Width - image.Width) / 2, 90);

        // 2. ТЕКСТ (Выравнивание через фиксированную ширину контейнера)
        var text = new Label
        {
            Text = "Перетащите файл сюда",
            Font = new Font("Segoe UI", 18),
            ForeColor = Color.Gray,
            BackColor = Color.Transparent,
            AutoSize = false,                       // Отключаем, чтобы контролировать ширину
            Width = MainPanel.Width,                // Растягиваем во всю ширину панели
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter // Центрируем сам текст внутри блока
        };
        // Размещаем строго под иконкой с отступом в 15 пикселей
        text.Location = new Point(0, image.Bottom + 15);

        // 3. КНОПКА ВЫБОРА ФАЙЛА
        btnSelectImage = new Guna2Button
        {
            Text = "Выберите файл",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BorderRadius = 25,
            Font = new Font("Segoe UI", 11),
            Size = new Size(180, 50),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent,
            UseTransparentBackground = true // Корректное отображение Guna-компонента
        };
        btnSelectImage.Click += (s, e) => SelectFile();
        // Позиционируем кнопку по центру под текстом с отступом в 20 пикселей
        btnSelectImage.Location = new Point((MainPanel.Width - btnSelectImage.Width) / 2, text.Bottom + 20);
        var text2 = new Label
        {
            Text = "Поддерживаемые форматы: png/jpg/bmp/gif",
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.Gray,
            BackColor = Color.Transparent,
            AutoSize = false,                       // Отключаем, чтобы контролировать ширину
            Width = MainPanel.Width,                // Растягиваем во всю ширину панели
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter // Центрируем сам текст внутри блока
        };
        // Размещаем строго под иконкой с отступом в 15 пикселей
        text2.Location = new Point(0, btnSelectImage.Bottom + 15);
        // Добавляем все настроенные элементы на панель
        MainPanel.Controls.Add(image);
        MainPanel.Controls.Add(text);
        MainPanel.Controls.Add(btnSelectImage);
        MainPanel.Controls.Add(text2);
    }


    private void SelectFile()
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                SetSelectedImagePath(ofd.FileName);
                UpdateAvailableMethods(ofd.FileName);
                UpdateStatus($"Выбран файл: {Path.GetFileName(ofd.FileName)}");
                ChosenStatus = true;
                SelectedFileWindow();
            }
        }
    }

    private void SelectedFileWindow()
    {
        if (!ChosenStatus) return;
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(0, 130, 0, 0);
        var image = new PictureBox { Dock = DockStyle.Top, BackColor = Color.Transparent, Image = Image.FromFile("imgs/chosen.png"), SizeMode = PictureBoxSizeMode.CenterImage };
        var text = new Label
        {
            Dock = DockStyle.Top,
            Text = lblStatus.Text.Length > 36 ? lblStatus.Text.Substring(13, 23) + "..." : (lblStatus.Text.Length > 13 ? lblStatus.Text.Substring(13) : ""),
            Height = 50,
            BackColor = Color.Transparent,
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
            BackColor = Color.Transparent,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };
        var buttonContainer = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Top, Height = 70 };
        btnSelectImage = new Guna2Button
        {
            Text = "Заменить файл",
            Size = new Size(180, 50),
            FillColor = ColorTranslator.FromHtml("#DDDDDD"),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            BackColor = Color.Transparent,
            BorderRadius = 25,
            Font = new Font("Segoe UI", 11),
            Cursor = Cursors.Hand,
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
    public string GetSelectedMethod()
    {
        if (methodComboBox.SelectedItem == null) return "exif";
        string selectedText = methodComboBox.SelectedItem.ToString();
        if (selectedText.Contains("JSteg")) return "jsteg";
        if (selectedText.Contains("LSB")) return "lsb";
        if (selectedText.Contains("GIF")) return "gif";
        if (selectedText.Contains("Скрытый") && !selectedText.Contains("JSteg")) return "marker";
        return "exif";
    }
    public void TextWindow()
    {
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);
        var label = new Label { BackColor = Color.Transparent, Text = "Скрытое сообщение", Font = new Font("Segoe UI", 14, FontStyle.Bold), Width = 500, Height = 40, Dock = DockStyle.Top };
        txt = new Guna2TextBox
        {
            PlaceholderText = "Введите текст, который нужно скрыть в файле...",
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Height = 330,
            Multiline = true,
            BorderRadius = 10,
            FillColor = Color.WhiteSmoke
        };
        cancel = new Label { Text = "Отмена", BackColor = Color.Transparent, ForeColor = Color.Gray, Font = new Font("Segoe UI", 11), AutoSize = true, Dock = DockStyle.Top, Cursor = Cursors.Hand };
        var enterButton = new Guna2Button
        {
            Text = "Зашифровать",
            FillColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            BorderRadius = 17,
            Font = new Font("Segoe UI", 11),
            Size = new Size(125, 40),
            Location = new Point(330, 380),
            Cursor = Cursors.Hand
        };
        enterButton.Click += (s, e) =>
        {
            var type = GetSelectedEncryptionType();
            var pass = GetPassword();
            EncryptionSettingsChanged?.Invoke(type, pass);
            HideRequested?.Invoke(_selectedImagePath, txt.Text);
        };
        cancel.Click += (e, s) => MainWindow();
        MainPanel.Controls.Add(cancel);
        MainPanel.Controls.Add(enterButton);
        MainPanel.Controls.Add(txt);
        MainPanel.Controls.Add(label);
    }

    public void ShowExtractedData(string data)
    {
        if (MainPanel.InvokeRequired) { MainPanel.Invoke((MethodInvoker)(() => ShowExtractedData(data))); return; }
        MainPanel.Controls.Clear();
        MainPanel.Padding = new Padding(30);
        var label = new Label { BackColor = Color.Transparent, Text = "Извлечённое сообщение", Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Top, Height = 40 };
        var textBox = new Guna2TextBox
        {
            Text = string.IsNullOrEmpty(data) ? "Данные не найдены" : data,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
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
            BackColor = Color.Transparent,
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