using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

public class InstructionForm : Form
{
    public InstructionForm()
    {
        // 1. НАСТРОЙКИ ОКНА
        this.Text = "Справочник и Инструкция пользователя";
        this.Size = new Size(560, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MinimizeBox = true;
        this.Icon = new Icon("imgs/_.ico");
        this.MaximizeBox = false;
        this.BackColor = Color.White;

        // 2. ГЛАВНЫЙ СКОЛЛ-КОНТЕЙНЕР (Заменяем обычную Panel на FlowLayoutPanel)
        var mainScrollPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,             // Включаем автоскролл
            WrapContents = false,          // Запрещаем перенос элементов в новые столбцы (только строго вниз)
            FlowDirection = FlowDirection.TopDown, // Элементы идут сверху вниз
            Padding = new Padding(25, 25, 10, 25) // Небольшой отступ справа под скроллбар
        };
        this.Controls.Add(mainScrollPanel);

        // Расчитываем чистую ширину элементов с учетом отступов панели (560 - 25 - 25 - 20)
        int contentWidth = 475;

        // 3. Создаем и добавляем заголовок темы через метод
        var mainTitle = CreateSectionHeader("Инструкция по использованию", contentWidth);
        mainScrollPanel.Controls.Add(mainTitle);
        // 4. Создаем и добавляем подзаголовок (описание) через метод
        string descText = "MetaHide реализует передовые алгоритмы локальной стеганографии. Процесс обработки полностью конфиденциален и не отправляет ваши файлы на сторонние серверы.";
        var mainDescription = CreateSectionDescription(descText, contentWidth, 55);
        mainScrollPanel.Controls.Add(mainDescription);


        // 5. БЛОК 1: АРХИТЕКТУРА СОКРЫТИЯ ДАННЫХ
        string block1Text = "На главной панели добавьте файл одного из поддерживаемых форматов: png/jpg/bmp/gif. Слева в строке \"Метод скрытия\" появятся поддерживаемые режимы, вы можете выбрать один из них.\n\n" +
                            "В левой панели \"Криптозащита\" выберите шифр и напишите криптографический пароль(запомните его). Снизу нажмите на кнопку \"Зашифровать\", если хотите спрятать информацию в файле и в открывшемся текстовом окне напишите ваше сообщение. После нажатия кнопки \"зашифровать\" новое изображение появится у вас на рабочем столе.\n\n" +
                            "Если вы хотите расшифровать информацию из картинки, то следуйте инструкции: слева выберите шифр, который вы или кто-то другой использовал для шифровки; нажмите на соответствующую кнопку \"Расшифровать\"; в открышемся окне введите криптографический пароль; в открывшемся текстовом поле будет скрытое сообщение.\n\n" +
                            "1.Чекбокс \"Использовать сжатие\" отвечает за то, будет ли использоваться сжатие начиная от определённого объёма данных\n" +
                            "2.Кнопка \"Журнал логов\" открывает log файл со всеми совершёнными операциями.\n" +
                            "3.Кнопка \"Очистить лог\" очищает log файл.\n" +
                            "4.Кнопка \"Запустить тесты\" работает так: На рабочем столе создается папка TestImages, в которую вы можете положить какие-нибудь свои файлы поддерживаемого формата, а после повторного нажатия на кнопку проведутся тесты, результаты которых вы можете посмотреть в log файле.";

        var block1 = CreateInstructionBlock("1. Основное положение", block1Text, contentWidth, 610);
        mainScrollPanel.Controls.Add(block1);

        var Title2 = CreateSectionHeader("Архитектурные вопросы", contentWidth);
        mainScrollPanel.Controls.Add(Title2);
        // 6. БЛОК 2: КРИПТОГРАФИЧЕСКАЯ ЗАЩИТА
        string block2Text = "• EXIF (Exchangeable Image File Format): Стандарт метаданных, который автоматически сохраняет внутри файлов (JPG/TIFF) скрытую информацию о снимке: дату, модель камеры, настройки выдержки и GPS-координаты. Запись данных происходит в специальные теги-контейнеры, которые игнорируются обычными просмотрщиками, благодаря чему картинка не искажается визуально.\n\n" +
            "• LSB (Least Significant Bit): Алгоритм заменяет самые младшие биты в байтах цвета пикселей (RGB) на биты скрываемого файла. Так как изменение значения цвета на 1 единицу (например, с 255 до 254 в красном канале) создает разницу, которую человеческий глаз физически не способен заметить на экране, изображение визуально остается абсолютно идентичным оригиналу.";

        var block2 = CreateInstructionBlock("1. Методы сокрытия", block2Text, contentWidth, 330);
        mainScrollPanel.Controls.Add(block2);

        string block3Text = "• XOR: Быстрое поточное симметричное шифрование с использованием ключевого слова. Подходит для базовой защиты данных от случайного извлечения. Побитовое исключающее «ИЛИ». Алгоритм последовательно накладывает символы вашего пароля на байты файла. Если применить пароль один раз — данные превратятся в хаос (зашифруются), а если применить тот же пароль к хаосу еще раз — они мгновенно вернутся в исходный вид (расшифруются).\n\n" +
            "• AES-128: Облегченная версия стандарта блочного шифрования с длиной ключа 128 бит. Обеспечивает оптимальный баланс между высокой скоростью обработки данных и криптографической стойкостью, достаточной для защиты конфиденциальной информации. Блочный алгоритм шифрования. Он разбивает шифруемые данные на равные блоки по 16 байт (128 бит) и перемешивает их с секретным ключом в течение 10 раундов сложных математических преобразований (замен и перестановок). Взломать такой шифр прямой атакой невозможно, так как количество комбинаций ключа составляет 2 в 128-й степени.\n\n" +
            "• AES-256: Промышленный стандарт блочного шифрования с длиной ключа 256 бит. Данные невозможно расшифровать без точного знания мастер-пароля, даже если факт наличия стеганографии будет раскрыт. Использует ту же блочную архитектуру, что и AES-128, но увеличивает длину ключа до 256 бит и количество раундов перемешивания до 14. Это создаёт астрономическое число комбинаций для взлома, делая алгоритм криптостойким даже против теоретических квантовых компьютеров будущего.";

        var block3 = CreateInstructionBlock("2. Криптографическая защита", block3Text, contentWidth, 630);
        mainScrollPanel.Controls.Add(block3);

        string block4Text = "• Алгоритм DEFLATE (LZ77 + Хаффман): Двухэтапное сжатие данных. Сначала алгоритм Лемпеля — Зива (LZ77) находит в файле повторяющиеся последовательности байт и заменяет их короткими ссылками. Затем полученный результат кодируется по методу Хаффмана, который заменяет часто встречающиеся символы короткими битовыми кодами, а редкие — длинными. Это позволяет уменьшить размер внедряемого файла до минимума.";

        var block4 = CreateInstructionBlock("3. Сжатие", block4Text, contentWidth, 210);
        mainScrollPanel.Controls.Add(block4);

        mainScrollPanel.Controls.Add(new Panel { Width = contentWidth, Height = 20, BackColor = Color.Transparent });
    }

    private Guna2Panel CreateInstructionBlock(string title, string content, int width, int height)
    {
        var card = new Guna2Panel
        {
            Width = width,
            Height = height,
            FillColor = ColorTranslator.FromHtml("#F8F9FA"), // Возвращаем светло-серый как на макете
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.Gray,
            Padding = new Padding(20, 15, 20, 15),
            Margin = new Padding(0, 0, 0, 15)
        };

        var blockTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#111111"),
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            AutoSize = false, // 1. Выключаем AutoSize
            Height = 28,      // 2. Жестко задаем высоту строки заголовка (хватит для 11 шрифта)
            TextAlign = ContentAlignment.MiddleLeft // Выравнивание внутри строки
        };

        var blockContent = new Label
        {
            Text = content,
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#444444"),
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent, // Добавляем прозрачность фона на всякий случай
            AutoSize = false
        };

        // ВАЖНО: Соблюдаем правильную очередность добавления в Controls
        card.Controls.Add(blockContent); // Сначала контент (он займет всю оставшуюся область)
        card.Controls.Add(blockTitle);   // Затем заголовок (он прижмется к самому верху)

        return card;
    }

    private Label CreateSectionDescription(string text, int width, int height = 55)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTranslator.FromHtml("#555555"),
            Width = width,
            Height = height,
            AutoSize = false,
            Margin = new Padding(0, 0, 0, 15),
            BackColor = Color.Transparent
        };
    }


    private Panel CreateSectionHeader(string text, int width)
    {
        // Главный контейнер для заголовка темы
        var headerContainer = new Panel
        {
            Width = width,
            Height = 35,
            Margin = new Padding(0, 10, 0, 10), // Отступы сверху и снизу от других блоков
            BackColor = Color.Transparent
        };

        // Оранжевая черточка слева
        var orangeBorder = new Panel
        {
            Width = 4,
            Height = 22,
            BackColor = Color.Orange
        };
        // Центрируем черточку по вертикали относительно высоты контейнера
        orangeBorder.Location = new Point(0, (headerContainer.Height - orangeBorder.Height) / 2);

        // Текст заголовка
        var titleLabel = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#222222"),
            BackColor = Color.Transparent,
            Location = new Point(12, 3), // Сдвигаем вправо от оранжевой линии
            AutoSize = true
        };

        // Собираем компоненты вместе
        headerContainer.Controls.Add(titleLabel);
        headerContainer.Controls.Add(orangeBorder);

        return headerContainer;
    }

}
