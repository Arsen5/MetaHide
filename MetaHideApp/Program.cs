using System;
using System.Windows.Forms;
using MetaHide.controller;
using test;

namespace test
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            var MVC = new MVC();
            MVC.Run();
        }
    }
}