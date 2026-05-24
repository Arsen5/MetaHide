using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaHide.view;

public class MyCustomToggleBtn : CheckBox
{
    private Color ofBack = Color.Gray;
    private Color onBack = Color.Coral;
    private Color toggle = Color.White;

    public MyCustomToggleBtn()
    {
        this.MinimumSize = new Size(25, 22);
    }

    private GraphicsPath GetFigure()
    {
        var arcSize = this.Height - 1;
        var leftArc = new Rectangle(0, 0, arcSize, arcSize);
        var rightArc = new Rectangle(this.Width - arcSize - 2, 0, arcSize, arcSize);
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(leftArc, 90, 180);
        path.AddArc(rightArc, 270, 180);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var toggleSize = this.Height - 5;
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(this.Parent.BackColor);
        if (this.Checked)
        {
            pevent.Graphics.FillPath(new SolidBrush(onBack), GetFigure());
            pevent.Graphics.FillEllipse(new SolidBrush(toggle),
                new Rectangle(this.Width - this.Height + 1, 2, toggleSize, toggleSize));
        }
        else
        {
            pevent.Graphics.FillPath(new SolidBrush(ofBack), GetFigure());
            pevent.Graphics.FillEllipse(new SolidBrush(toggle),
                new Rectangle(2, 2, toggleSize, toggleSize));
        }
    }
}
