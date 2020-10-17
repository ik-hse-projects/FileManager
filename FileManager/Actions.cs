using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FileManager.Safety;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    public class Actions
    {
        private FileManager manager;

        public Actions(FileManager manager)
        {
            this.manager = manager;
        }

        public void Attach()
        {
            manager.RootContainer.AsIKeyHandler()
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
            manager.RootContainer.Loop.OnPaused = () =>
            {
                // TODO: Ask encoding
                encoding ??= Encoding.Default;

                var errors = new List<(string filename, string message)>();
                foreach (var path in manager.SelectedFiles)
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
                    ("UTF8", Encoding.UTF8),
                    ("ASCII", Encoding.ASCII)
                },
                OnAnswered = ReadFiles
            }.Show(manager.RootContainer);
        }

        private void CreateFile(Encoding encoding = null)
        {
            // TODO
        }

        private void CopyFiles()
        {
            new Dialog<bool>
            {
                Question = "Как вы относитель к перезаписи файлов?",
                Answers = new[]
                {
                    ("Не перезаписывать", false),
                    ("Перезаписывать", true),
                },
                OnAnswered = CopyFiles
            }.Show(manager.RootContainer);
        }

        private void CopyFiles(bool overwrite)
        {
            var target = SafeIO.DirectoryInfo(manager.CurrentDirectory?.FullName);
            if (target.State == ResultState.Error)
            {
                new Dialog<object>
                {
                    Question = $"Невозможно скопировать файлы в текущую директорию: {target.ErrorMessage}",
                }.Show(manager.RootContainer);
                return;
            }
            var errors = new List<(string path, string message)>();
            foreach (var selectedFile in manager.SelectedFiles)
            {
                var newLocation = Path.Combine(target.Value.FullName, Path.GetFileName(selectedFile));
                if (SafeIO.CopyFile(selectedFile, newLocation, overwrite) is {State: ResultState.Error, ErrorMessage: var message})
                {
                    errors.Add((selectedFile, message));
                }
            }

            new Dialog<object>
            {
                Question = $"Не удалось скопировать {errors.Count} файлов",
                Answers = errors.Select(e => ($"{e.path}: {e.message}", new object())).ToArray()
            }.Show(manager.RootContainer);
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