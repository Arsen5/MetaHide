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
    public event Action<string>? ShowAllFieldsRequested;
    public event Action<bool>? ModeChangedRequested;

    // Элементы управления
    private Button? btnSelectImage;
    private Button? btnHide;
    private Button? btnExtract;
    private Button? btnShowAllFields;
    private TextBox? txtText;
    private TextBox? txtResult;
    private Label? lblStatus;
    private RadioButton? rbVisibleMode;
    private RadioButton? rbHiddenMode;

    //элементы интерфейса

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
}
