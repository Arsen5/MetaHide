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
        form.Icon = new Icon("_.ico");
        form.MaximizeBox = false;
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

    public void ShowExtractedData(string data)
    {
        if (txt != null)
            txt.Invoke((MethodInvoker)(() => txt.Text = data));
    }

    public string GetSelectedImagePath() => _selectedImagePath;

    public void SetSelectedImagePath(string path)
    {
        _selectedImagePath = path;
        UpdateFileName(Path.GetFileName(path));
    }
}