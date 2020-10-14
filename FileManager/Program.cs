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
            var help = new StackContainer
            {
                new Label("         Немного про интерфейс:         ") {CurrentStyle = Style.Active},
                new Label("Текущий путь отображается слева вверху. "),
                new Label("Список дисков можно найти в самом корне."),
                new Label("Для навигации есть стрелочки и Enter.   "),
                new Label("Справа находится список выбранных файлов"),
                new Label(" Он может перекрыть справку, если велик,"),
                new Label(" но это удобно, ведь значит пользователь"),
                new Label(" освоился, раз смог выбрать много файлов"),
                new Label(" и значит справка ему больше не нужна.  "),
                new Label("           Выбор файлов:                ") {CurrentStyle = Style.Active},
                new Label("Большинство действий затрагивают только "),
                new Label("  явно выбранные файлы. Используйте Ins!"),
                new Label("Tab - перейти к списку выбранных файлов "),
                new Label("Del - отменить выбор сфокусированного.  "),
                new Label("Enter - открыть директорию с этим файлом"),
                new Label("             Команды:                   ") {CurrentStyle = Style.Active},
                new Label("Enter — прочитать выбранные файлы в UTF8"),
                new Label("F3 — выбрать кодировку и прочитать файлы"),
                new Label("F4 — выбрать кодировку и создать файл.  "),
                new Label("Ins или Пробел — выбрать файл.          "),
                new Label("F5 — вставить в текущую папку файлы.    "),
                new Label("F6 — переместить выбранные файлы.       "),
                new Label("F8 или Del — удалить все выбранные файлы"),
                new Label("F10 — закрыть файловый менеджер.        "),
            };
            
            var header = new StackContainer
            {
                Orientation = Orientation.Horizontal,
                Margin = 1,
            };
            header.Add(new Label("HEADER")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });
            header.Add(new Label("H")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });
            
            var list = new StackContainer
            {
                MaxVisibleCount = 21
            };
            list.Add(new Label("LIST")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });
            list.Add(new Label("L")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });

            var selected = new StackContainer
            {
                MaxVisibleCount = 21
            };
            selected.Add(new Label("SELECTED")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });
            selected.Add(new Label("S")
            {
                CurrentStyle = new Style(MyColor.Default, MyColor.Transparent)
            });

            var root = new BaseContainer
            {
                new RelativePosition(0, 0, 1)
                {
                    header
                },
                new RelativePosition(0, 1, 0)
                {
                    new Frame(Style.DarkGrayOnDefault)
                    {
                        list
                    }
                },
                new RelativePosition(40, 1, -1)
                {
                    help
                },
                new RelativePosition(40, 1, 1)
                {
                    new Frame(Style.DarkGrayOnDefault)
                    {
                        selected
                    }
                },
            };

            new MainLoop(root).Start();
        }
    }
}