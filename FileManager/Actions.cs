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
                    () => ReadFiles(Encoding.UTF8))
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

        private void ReadFiles(Encoding encoding)
        {
            root.Loop.OnPaused = () =>
            {
                // TODO: Ask encoding
                encoding ??= Encoding.Default;

                var errors = new List<(string filename, string message)>();
                foreach (var path in selectedFiles)
                {
                    using var maybeReader = SafeIO.GetFullPath(path)
                        .AndThen(fullPath => SafeIO.StreamReader(fullPath, encoding));
                    switch (maybeReader)
                    {
                        case { State: ResultState.Ok, Value: var reader }:
                        {
                            foreach (var maybeBlock in reader.ReadBlocks())
                            {
                                switch (maybeBlock)
                                {
                                    case {State: ResultState.Ok, Value: var block}:
                                        foreach (var c in block)
                                        {
                                            Console.Write(c);
                                        }

                                        break;
                                    case { State: ResultState.Error, ErrorMessage: var message }:
                                        errors.Add((path, message));
                                        goto EndOfFile;
                                }
                            }

                            break;
                        }
                        case { State: ResultState.Error, ErrorMessage: var message }:
                        {
                            errors.Add((path, message));
                            break;
                        }
                    }
                    EndOfFile: ;
                }

                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
                Console.WriteLine();
                if (errors.Count != 0)
                {
                    Console.WriteLine("Некоторые файлы не были прочитаны или были прочитаны не полностью:");
                    foreach (var (filename, message) in errors)
                    {
                        Console.WriteLine($"{filename}: {message}");
                    }
                }

                Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер.");
                Console.ReadLine();
            };
        }
        
        private void ReadFiles()
        {
            new Dialog<Encoding>
            {
                Question = "Выберите кодировку",
                Answers = new []
                {
                    Encoding.UTF8,
                    Encoding.ASCII,
                },
                OnAnswered = ReadFiles
            }.Show(root);
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