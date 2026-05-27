using MetaHide.model;
using Microsoft.VisualBasic.Logging;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace MetaHide.view;

public partial class View
{
    private Model model;
    private Form form;

    // События для Controller
    public event Action<string, string>? HideRequested;
    public event Action<string>? ExtractRequested;
    public event Action<bool>? ModeChangedRequested;

    // События для шифрования и сжатия
    public event Action<EncryptionModel.EncryptionType, string>? EncryptionSettingsChanged;
    public event Action<bool, int>? CompressionSettingsChanged;

    // Событие для выбора метода стеганографии
    public event Action<string>? MethodTypeChanged;

    private Button? btnSelectImage;
    private Button? btnHide;
    private Button? btnExtract;
    private TextBox? txt;
    private Label? lblStatus;

    private string _selectedImagePath = "";

    public View(Model model, Form form)
    {
        this.model = model;
        this.form = form;
        form.Size = new Size(1200, 700);
        form.Text = "MetaHide";
        form.Icon = new Icon("imgs/_.ico");
        form.MaximizeBox = false;
        form.FormBorderStyle = FormBorderStyle.FixedSingle;
        CreateInterface();
    }

    public void UpdateStatus(string message)
    {
        if (lblStatus != null)
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = message));
    }

    public void UpdateFileName(string fileName)
    {
        if (lblStatus != null)
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = $"Файл: {fileName}"));
    }


    public string GetSelectedImagePath() => _selectedImagePath;

    public void SetSelectedImagePath(string path)
    {
        _selectedImagePath = path;
        UpdateFileName(Path.GetFileName(path));
    }

    public string? ShowPasswordDialog()
    {
        using (Form passwordForm = new Form())
        {
            passwordForm.Text = "Введите пароль";
            passwordForm.Size = new Size(300, 150);
            passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            passwordForm.StartPosition = FormStartPosition.CenterParent;
            passwordForm.MaximizeBox = false;
            passwordForm.MinimizeBox = false;

            var label = new Label
            {
                Text = "Пароль для дешифрования:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(240, 25),
                PasswordChar = '*',
                PlaceholderText = "Введите пароль"
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(100, 80),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                Location = new Point(185, 80),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            passwordForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            passwordForm.AcceptButton = okButton;
            passwordForm.CancelButton = cancelButton;

            if (passwordForm.ShowDialog() == DialogResult.OK)
            {
                return textBox.Text;
            }
            else
            {
                return null;
            }
        }
    }
}