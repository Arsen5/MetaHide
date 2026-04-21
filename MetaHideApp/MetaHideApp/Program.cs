using System;
using System.Windows.Forms;
using MetaHide;
using test;

namespace test
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Form1 view = new Form1();
            Controller controller = new Controller(view);  // Связываем View и Controller
            Application.Run(view);
        }
    }
}