using System;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    class Program
    {
        static void Main(string[] args)
        {
            // Краткость — сестра таланта.
            // Нужно уложиться в 40 символов (половину ширины терминала) и 23 строки (высота терминала - 1).
            var help = new StackContainer()
                .Add(new Label("         Немного про интерфейс:         ") {CurrentStyle = Style.Active})
                .Add(new Label("Текущий путь отображается слева вверху. "))
                .Add(new Label("Список дисков можно найти в самом корне."))
                .Add(new Label("Для навигации есть стрелочки и Enter.   "))
                .Add(new Label("Справа находится список выбранных файлов"))
                .Add(new Label(" Он может перекрыть справку, если велик,"))
                .Add(new Label(" но это удобно, ведь значит пользователь"))
                .Add(new Label(" освоился, раз смог выбрать много файлов"))
                .Add(new Label(" и значит справка ему больше не нужна.  "))
                .Add(new Label("           Выбор файлов:                ") {CurrentStyle = Style.Active})
                .Add(new Label("!Не забывайте выбирать файлы через Ins! "))
                .Add(new Label("Tab - перейти к списку выбранных файлов "))
                .Add(new Label("Del - отменить выбор сфокусированного.  "))
                .Add(new Label("Enter - открыть директорию с этим файлом"))
                .Add(new Label("             Команды:                   ") {CurrentStyle = Style.Active})
                .Add(new Label("F2 — прочитать выбранные файлы в UTF8.  "))
                .Add(new Label("F3 — выбрать кодировку и прочитать файлы"))
                .Add(new Label("F4 — выбрать кодировку и создать файл.  "))
                .Add(new Label("Ins или Пробел — выбрать файл.          "))
                .Add(new Label("F5 — вставить в текущую папку файлы.    "))
                .Add(new Label("F6 — переместить выбранные файлы.       "))
                .Add(new Label("F8 или Del — удалить все выбранные файлы"))
                .Add(new Label("F10 — закрыть файловый менеджер.        "));

            var manager = new FileManager(80, 24);
            manager.AttachActions();
            manager.ChangeDir(null);

            var root = new BaseContainer()
                .AddFocused(manager.RootContainer)
                .Add(new RelativePosition(40, 1, -1)
                    .Add(help));

            var loop = new MainLoop(root);
            loop.Start();
        }
    }
}