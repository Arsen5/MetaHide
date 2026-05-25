using System;
using System.Windows.Forms;
using MetaHide.controller;
using test;

namespace test
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Если передан аргумент "test", запускаем тесты
            if (args.Length > 0 && args[0] == "test")
            {
                RunTests();
            }
            else
            {
                // Иначе запускаем основное приложение
                ApplicationConfiguration.Initialize();
                var MVC = new MVC();
                MVC.Run();
            }
        }

        static void RunTests()
        {
            Console.WriteLine("=== MetaHide Тестер ===");
            Console.WriteLine("Тестирует стеганографию на изображениях из папки TestImages");
            Console.WriteLine();

            try
            {
                var tester = new MetaHide.tests.SteganographyTester();
                tester.RunTests();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}