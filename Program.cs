using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FingerScreensaver
{
    internal static class Program
    {
        // Windows API для работы с окнами
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_SHOW = 5;
        /// <summary>
        /// Точка входа приложения
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Разбор аргументов командной строки
            if (args.Length > 0)
            {
                string command = args[0].ToLower().Trim();
                
                // Обрабатываем первые два символа (могут быть /s, -s, /c, -c и т.д.)
                if (command.Length > 1)
                {
                    command = command.Substring(0, 2);
                }

                switch (command)
                {
                    case "/s": // Запуск скринсейвера
                    case "-s":
                        Application.Run(new ScreensaverForm(false));
                        break;

                    case "/p": // Предпросмотр
                    case "-p":
                        // Для предпросмотра показываем миниатюру
                        if (args.Length > 1 && int.TryParse(args[1], out int parentHandle))
                        {
                            // Устанавливаем родительское окно для встраивания в диалог настроек
                            var previewForm = new ScreensaverForm(true);
                            
                            // Устанавливаем родительское окно
                            SetParent(previewForm.Handle, new IntPtr(parentHandle));
                            
                            // Позиционируем окно внутри родительского
                            SetWindowPos(previewForm.Handle, IntPtr.Zero, 0, 0, 320, 240, 
                                SWP_NOZORDER | SWP_NOACTIVATE);
                            
                            // Показываем окно
                            ShowWindow(previewForm.Handle, SW_SHOW);
                            
                            // Запускаем цикл сообщений только для этого окна
                            Application.Run(previewForm);
                        }
                        else
                        {
                            // Если нет родительского окна, просто выходим
                            return;
                        }
                        break;

                    case "/c": // Настройки
                    case "-c":
                        // У нас нет настроек, показываем сообщение
                        MessageBox.Show(
                            "Этот скринсейвер не имеет настроек.\n\nПросто наслаждайтесь эффектом звездного тунеля! 🖕",
                            "Finger Screensaver",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        break;

                    default:
                        // Неизвестная команда - запускаем обычный режим
                        Application.Run(new ScreensaverForm(false));
                        break;
                }
            }
            else
            {
                // Без аргументов - запускаем в обычном режиме
                Application.Run(new ScreensaverForm(false));
            }
        }
    }
}