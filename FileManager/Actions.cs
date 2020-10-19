using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileManager.Safety;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    public class Actions
    {
        private readonly FileManager manager;

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
                    ReadFiles)
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
                Answers = new[]
                {
                    ("UTF8", Encoding.UTF8),
                    ("ASCII", Encoding.ASCII)
                },
                OnAnswered = ReadFiles
            }.Show(manager.RootContainer);
        }

        private void CopyFiles()
        {
            new Dialog<bool>
            {
                Question = "Как вы относитель к перезаписи файлов?",
                Answers = new[]
                {
                    ("Не перезаписывать", false),
                    ("Перезаписывать", true)
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
                    Question = $"Невозможно копировать файлы в текущую директорию: {target.ErrorMessage}"
                }.Show(manager.RootContainer);
                return;
            }

            manager.RootContainer.Loop.OnPaused = () =>
            {
                Console.WriteLine("Копируем...");
                foreach (var selectedFile in manager.SelectedFiles)
                {
                    var newLocation = Path.Combine(target.Value.FullName, Path.GetFileName(selectedFile));
                    if (SafeIO.CopyFile(selectedFile, newLocation, overwrite) is {State: ResultState.Error, ErrorMessage
                        :
                        var message})
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{selectedFile}: {message}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{selectedFile}: OK");
                    }
                }

                Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер");
                Console.ReadLine();
                manager.Refresh();
            };
        }

        private void MoveFiles()
        {
            void Delete()
            {
                Console.WriteLine("Перемещаем...");

                foreach (var file in manager.SelectedFiles)
                {
                    if (SafeIO.Move(file, manager.CurrentDirectory?.FullName) is {State: ResultState.Error, ErrorMessage
                        : var message})
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{file}: {message}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{file}: OK");
                    }
                }

                Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер");
                Console.ReadLine();
                manager.Refresh();
            }

            var target = SafeIO.DirectoryInfo(manager.CurrentDirectory?.FullName);
            if (target.State == ResultState.Error)
            {
                new Dialog<object>
                {
                    Question = $"Невозможно копировать файлы в текущую директорию: {target.ErrorMessage}"
                }.Show(manager.RootContainer);
                return;
            }

            new Dialog<object>
            {
                Question = $"Точно переместить {manager.SelectedFiles.Count} объектов?",
                Answers = new[] {("Да", new object())},
                OnAnswered = _ => manager.RootContainer.Loop.OnPaused = Delete
            }.Show(manager.RootContainer);
        }

        private void DeleteFiles()
        {
            void Delete()
            {
                Console.WriteLine("Удаляем...");

                foreach (var file in manager.SelectedFiles)
                {
                    if (SafeIO.DeleteRecursively(file) is {State: ResultState.Error, ErrorMessage : var message})
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{file}: {message}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{file}: OK");
                    }
                }

                Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер");
                Console.ReadLine();
                manager.Refresh();
            }

            new Dialog<object>
            {
                Question = $"Точно удалить {manager.SelectedFiles.Count} объектов?",
                Answers = new[] {("Да", new object())},
                OnAnswered = _ => manager.RootContainer.Loop.OnPaused = Delete
            }.Show(manager.RootContainer);
        }

        private void ConcatFile(string targetPath, Encoding encoding)
        {
            using var destination = SafeIO.CreateFile(targetPath, encoding);

            if (destination.State == ResultState.Error)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine($"Невозможно создать файл: {destination.ErrorMessage}");
                Console.ResetColor();
                return;
            }

            foreach (var path in manager.SelectedFiles)
            {
                using var maybeReader = SafeIO.GetFullPath(path)
                    .AndThen(fullPath => SafeIO.StreamReader(fullPath, encoding));
                if (maybeReader.State == ResultState.Error)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"{path}: {maybeReader.ErrorMessage}");
                    Console.ResetColor();
                    goto EndOfFile;
                }

                foreach (var maybeBlock in maybeReader.Value.ReadBlocks())
                {
                    if (maybeBlock.State == ResultState.Error)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{path}: {maybeBlock.ErrorMessage}");
                        Console.ResetColor();
                        goto EndOfFile;
                    }

                    foreach (var c in maybeBlock.Value)
                    {
                        destination.Value.Write(c);
                    }
                }

                Console.WriteLine($"{path}: OK");

                EndOfFile: ;
            }
        }

        private IEnumerable<string> ReadLinesFromUser()
        {
            var emptyLinesCounter = 0;
            while (true)
            {
                var key = Console.ReadLine();
                if (key != "")
                {
                    while (emptyLinesCounter != 0)
                    {
                        yield return "";
                        emptyLinesCounter--;
                    }

                    if (key != null)
                    {
                        yield return key;
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    emptyLinesCounter++;
                    if (emptyLinesCounter == 3)
                    {
                        yield break;
                    }
                }
            }
        }

        private void NewFile(string targetPath, Encoding encoding)
        {
            using var destination = SafeIO.CreateFile(targetPath, encoding);

            if (destination.State == ResultState.Error)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine($"Невозможно создать файл: {destination.ErrorMessage}");
                Console.ResetColor();

                Console.WriteLine("Нажмите Enter, чтобы закончить");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Введите строки файла. Чтобы завершить ввод, три раза подряд нажмите Enter.");
            foreach (var line in ReadLinesFromUser())
            {
                if (destination.Value.WriteLineSafe(line) is {State: ResultState.Error, ErrorMessage: var message})
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"О ужас! {message}");
                    Console.ResetColor();

                    Console.WriteLine("Нажмите Enter, чтобы закончить");
                    Console.ReadLine();
                    return;
                }
            }
        }

        private void CreateFile(Encoding? encoding = null)
        {
            void AskMode(Action<int> then)
            {
                new Dialog<int>
                {
                    Question = "Как вы хотите создать файл?",
                    Answers = new[]
                    {
                        ("Конкатенировать существующие", 1),
                        ("Ввести вручную", 2)
                    },
                    OnAnswered = then
                }.Show(manager.RootContainer);
            }

            void AskEncoding(Action<Encoding> then)
            {
                if (encoding != null)
                {
                    then(encoding);
                }
                else
                {
                    new Dialog<Encoding>
                    {
                        Question = "Как вы хотите создать файл?",
                        Answers = new[]
                        {
                            ("UTF8", Encoding.UTF8),
                            ("ASCII", Encoding.ASCII)
                        },
                        OnAnswered = then
                    }.Show(manager.RootContainer);
                }
            }

            void AskFilename(Action<string> then)
            {
                var popup = new Popup()
                    .Add(new MultilineLabel("Внимание: существующие файлы будут очищены и перезаписаны!"))
                    .Add(new MultilineLabel("Введите имя новго файла:"));

                var input = new InputField
                {
                    Placeholder = new Placeholder(Style.Decoration, "Имя файла")
                };
                input.AllowedChars.Add(CharRange.FilenameChars);
                input.AsIKeyHandler()
                    .Add(KeySelector.SelectItem, () =>
                    {
                        popup.Close();
                        then(Path.Combine(manager.CurrentDirectory.FullName, input.Text.ToString()));
                    });
                popup.Add(input)
                    .AndFocus()
                    .Show(manager.RootContainer);
            }

            AskMode(mode => AskEncoding(encoding => AskFilename(filename => manager.RootContainer.Loop.OnPaused = () =>
            {
                switch (mode)
                {
                    case 1:
                        ConcatFile(filename, encoding);
                        break;
                    case 2:
                        NewFile(filename, encoding);
                        break;
                }

                manager.Refresh();
            })));
        }
    }
}