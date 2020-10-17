using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileManager.Safety;
using Thuja;

namespace FileManager
{
    public class Actions
    {
        private ICollection<string> selectedFiles;
        private BaseContainer root;

        public Actions(BaseContainer root, ICollection<string> selectedFiles)
        {
            this.root = root;
            this.selectedFiles = selectedFiles;
        }

        public void Attach()
        {
            root.AsIKeyHandler()
                // F2 | Ctrl+R: Прочитать файл в UTF8.
                .Add(new[] {new KeySelector(ConsoleKey.F2), new KeySelector(ConsoleKey.R, ConsoleModifiers.Control)},
                    () => ReadFiles())
                // F3 | Ctrl+O: Прочитать файл в выбранной кодировке.
                .Add(new[] {new KeySelector(ConsoleKey.F3), new KeySelector(ConsoleKey.O, ConsoleModifiers.Control)},
                    () => ReadFiles())
                // F4 | Ctrl+N: Создать UTF-8.
                .Add(new[] {new KeySelector(ConsoleKey.F4), new KeySelector(ConsoleKey.N, ConsoleModifiers.Control)},
                    () => CreateFile(Encoding.UTF8))
                // F5 | Ctrl+C: Скопировать файлы.
                .Add(new[] {new KeySelector(ConsoleKey.F5), new KeySelector(ConsoleKey.C, ConsoleModifiers.Control)},
                    CopyFiles)
                // F6 | Ctrl+X: Переместить файлы.
                .Add(new[] {new KeySelector(ConsoleKey.F6), new KeySelector(ConsoleKey.X, ConsoleModifiers.Control)},
                    MoveFiles)
                // F8 | Delete: Удалить файлы.
                .Add(new[] {new KeySelector(ConsoleKey.F8), new KeySelector(ConsoleKey.Delete)},
                    DeleteFiles);
        }

        private void ReadFiles(Encoding encoding = null)
        {
            root.Loop.OnPaused = () =>
            {
                // TODO: Ask encoding
                encoding ??= Encoding.Default;

                foreach (var path in selectedFiles)
                {
                    if (SafeIO.GetFullPath(path) is { State: ResultState.Ok, Value: var fullPath })
                    {
                        // FIXME: Not safe
                        using var reader = new StreamReader(path, encoding);
                        var buffer = new char[255];
                        reader.ReadBlock(buffer, 0, buffer.Length);
                        Console.Write(buffer);
                    }
                }

                Console.WriteLine("\n\nНажмите Enter, чтобы вернуться в менеджер.");
                Console.ReadLine();
            };
        }

        private void CreateFile(Encoding encoding = null)
        {
            // TODO
        }

        private void CopyFiles()
        {
            // TODO
        }

        private void MoveFiles()
        {
            // TODO
        }

        private void DeleteFiles()
        {
            // TODO
        }
    }
}