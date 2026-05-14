using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.ComponentModel;

namespace MetaHide.view;

public partial class View
{
    public Panel MainPanel;
    public bool ChosenStatus;
    private Label cancel;
    private MyCustomToggleBtn checkBox;
    

    private void CreateInterface()
    {
        var shifr = new Label
        {
            Location = new Point(30, 280),
            AutoSize = true,
            Text = "Использовать шифр?",
            Font = new Font("Inter", 11, FontStyle.Bold),
            ForeColor = Color.Gray
        };
        checkBox = new MyCustomToggleBtn
        {
            Size = new Size(50, 22),
            Location = new Point(220, 280),
            Cursor = Cursors.Hand,
        };
        checkBox.CheckedChanged += (s, e) =>
        {
            bool isHidden = checkBox.Checked; // true = скрытый, false = видимый
            ModeChangedRequested?.Invoke(isHidden);
            UpdateStatus(isHidden ? "Режим: Скрытый (не видно в свойствах)" : "Режим: Обычный (видно в свойствах)");
        };
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

        var instruction = new Panel
        {
            Size = new Size(320, 200),
            Location = new Point(30, 60),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20, 20, 20, 20)
        };

        var label = new Label
        {
            Text = "Инструкция",
            Font = new Font("Inter", 13, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            AutoSize = true,
            Dock = DockStyle.Top,
        };

        var instr = new Label
        {
            Location = new Point(20, 50),
            Text = "Перетащите файл в поле справа или нажмите\n" +
            "«выбор файла», чтобы загрузить документ для\n" +
            "шифрования/расшифровки.\n" +
            "Снизу нажмите кнопку\nЗашифровать/расшифровать.\n" +
            "Выберите режим: шифровать сообщение или нет.\n" +
            "• Поддерживаемые форматы: .png, .jpg\n" +
            "• Максимум ??? МБ\n" +
            "• Конфиденциальность: локальная обработка\n",
            Font = new Font("Inter", 9),
            ForeColor = ColorTranslator.FromHtml("#555555"),
            AutoSize = true,
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
            Image = Image.FromFile("SVG.png")
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

        // ========== КНОПКИ В ФУТЕРЕ (ТОЧНО КАК В ОРИГИНАЛЕ) ==========
        btnHide = new Button
        {
            Text = "Зашифровать",  // ← исправлено название
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

        instruction.Controls.Add(instr);
        instruction.Controls.Add(label);

        footer.Controls.Add(footerFile);
        footer.Controls.Add(btnHide);
        footer.Controls.Add(btnExtract);
        footerFile.Controls.Add(t1);
        footerFile.Controls.Add(lblStatus);

        form.Controls.Add(shifr);
        form.Controls.Add(checkBox);
        form.Controls.Add(MainPanel);
        form.Controls.Add(footer);
        form.Controls.Add(instruction);
        form.Controls.Add(header);
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
            if (files != null && files.Length > 0 && (files[0].EndsWith(".jpg") || files[0].EndsWith(".jpeg") || files[0].EndsWith(".png")))
            {
                _selectedImagePath = files[0];
                UpdateStatus($"Выбран файл: {System.IO.Path.GetFileName(files[0])}");
                ChosenStatus = true;
                SelectedFileWindow();
            }
            else
            {
                UpdateStatus("Пожалуйста, выберите JPG или PNG файл");
            }
        };

        var image = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(345, 100),
            Image = Image.FromFile("SVG.png")
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

        form.Controls.Add(MainPanel);
    }

    private void SelectFile()
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            ofd.Title = "Выберите изображение";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                SetSelectedImagePath(ofd.FileName);
                UpdateStatus($"Выбран файл: {System.IO.Path.GetFileName(ofd.FileName)}");
                ChosenStatus = true;
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
                Image = Image.FromFile("chosen.png"),
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
                Height = 70, // Высота контейнера (чуть больше кнопки для отступов)
            };

            btnSelectImage = new Button
            {
                Text = "Заменить файл",
                Size = new Size(160, 50),
                BackColor = ColorTranslator.FromHtml("#DDDDDD"),
                ForeColor = ColorTranslator.FromHtml("#666666"),
                Font = new Font("Inter", 11),
                // Вместо Dock используем Location и Anchor
                Location = new Point((buttonContainer.Width - 160) / 2, 10),
                Anchor = AnchorStyles.Top // Чтобы кнопка не "улетала" при изменении размеров
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
            PlaceholderText = "Введите текст, который нужно скрыть в файле, или здесь появится извлеченный текст...",
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
                HideRequested?.Invoke(_selectedImagePath, txt.Text);
        };
        cancel.Click += (e, s) => MainWindow();
        MainPanel.Controls.Add(cancel);
        MainPanel.Controls.Add(txt);
        MainPanel.Controls.Add(label);
    }
}