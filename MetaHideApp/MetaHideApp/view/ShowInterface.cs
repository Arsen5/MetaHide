using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaHide.view;

public partial class View
{
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
            Location = new Point(20, 60),
            Text = "Перетащите файл в поле справа или нажмите\n" +
            "«выбор файла», чтобы загрузить документ для\n" +
            "шифрования/расшифровки.\n" +
            "• Поддерживаемые форматы: .png, .jpg\n" +
            "• Максимум ??? МБ\n" +
            "• Конфиденциальность: локальная обработка\n",
            Font = new Font("Inter", 9),
            ForeColor = ColorTranslator.FromHtml("#555555"),
            AutoSize = true,
        };

        txtText = new TextBox
        {
            Location = new Point(30, 280),
            Multiline = true,
            PlaceholderText = "Введите сообщение",
            Size = new Size(320, 50)

        };

        txtResult = new TextBox
        {
            Location = new Point(30, 340),
            Multiline = true,
            Size = new Size(320, 50),
            PlaceholderText = "Здесь будет расшифрованое сообщение"
        };

        var dropPanel = new Panel
        {
            AllowDrop = true,
            Location = new Point(375, 60),
            Size = new Size(790, 450),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
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
        };

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

        var t1 = new Label //вот тут надо поле делать
        {
            Text = "Статус: ожидание загрузки",
            Dock = DockStyle.Top,
            ForeColor = ColorTranslator.FromHtml("#888888"),
            Font = new Font("Inter", 8),
            AutoSize = true,
        };

        btnHide = new Button
        {
            Text = "Расшифровать",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 13),
            Size = new Size(500, 50),
            Location = new Point(38, 60),
        };

        btnHide = new Button
        {
            Text = "Расшифровать",
            BackColor = ColorTranslator.FromHtml("#FF7F50"),
            ForeColor = Color.White,
            Font = new Font("Inter", 13),
            Size = new Size(550, 50),
            Location = new Point(38, 60),
        };

        btnExtract = new Button
        {
            Text = "Зашифровать",
            BackColor = Color.WhiteSmoke,
            ForeColor = Color.Black,
            Font = new Font("Inter", 13),
            Size = new Size(550, 50),
            Location = new Point(593, 60),
        };


        header.Controls.Add(title);
        header.Controls.Add(status);

        instruction.Controls.Add(instr);
        instruction.Controls.Add(label);

        footer.Controls.Add(footerFile);
        footer.Controls.Add(btnHide);
        footer.Controls.Add(btnExtract);
        footerFile.Controls.Add(t1);
        footerFile.Controls.Add(lblStatus);

        dropPanel.Controls.Add(image);
        dropPanel.Controls.Add(text);
        dropPanel.Controls.Add(btnSelectImage);


        form.Controls.Add(dropPanel);
        form.Controls.Add(txtResult);
        form.Controls.Add(txtText);
        form.Controls.Add(footer);
        form.Controls.Add(instruction);
        form.Controls.Add(header);
    }
}