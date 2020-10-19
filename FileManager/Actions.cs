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

                foreach (var file in manager.SelectedFiles)
                {
                    var result = SafeIO
                        .GetFullPath(file)
                        .AndThen(fullPath => SafeIO.StreamReader(fullPath, encoding))
                        .AndThen(reader =>
                        {
                            foreach (var maybeBlock in reader.ReadBlocks())
                            {
                                if (maybeBlock.State == ResultState.Error)
                                {
                                    return maybeBlock.Map(_ => new object());
                                }

                                foreach (var c in maybeBlock.Value)
                                {
                                    Console.Write(c);
                                }
                            }

                            return Result<object>.Ok(new object());
                        });
                    if (result is {State: ResultState.Error, ErrorMessage: var error})
                    {
                        errors.Add((file, error));
                    }
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
                Console.ResetColor();
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

        private Result<DirectoryInfo> CheckCurrentDir()
        {
            var result = SafeIO.DirectoryInfo(manager.CurrentDirectory?.FullName);
            if (result.State == ResultState.Error)
            {
                new Dialog<object>
                {
                    Question = $"Невозможно работать с текущей директорией: {result.ErrorMessage}"
                }.Show(manager.RootContainer);
            }

            return result;
        }

        private static void WriteErrorToConsole(string message)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void ForAllFiles<T>(Func<string, Result<T>> action)
        {
            foreach (var selectedFile in manager.SelectedFiles)
            {
                switch (action(selectedFile))
                {
                    case {State: ResultState.Error, ErrorMessage: var error}:
                        WriteErrorToConsole($"{selectedFile}: {error}");
                        break;
                    case {State: ResultState.Ok}:
                        Console.WriteLine($"{selectedFile}: OK");
                        break;
                }
            }
        }

        private void CopyFiles(bool overwrite)
        {
            var target = CheckCurrentDir();
            if (target.State == ResultState.Error)
            {
                return;
            }

            manager.RootContainer.Loop.OnPaused = () =>
            {
                Console.WriteLine("Копируем...");

                ForAllFiles(file =>
                {
                    var newLocation = Path.Combine(target.Value.FullName, Path.GetFileName(file));
                    return SafeIO.CopyFile(file, newLocation, overwrite);
                });

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

                ForAllFiles(file => SafeIO.Move(file, manager.CurrentDirectory?.FullName));

                Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер");
                Console.ReadLine();
                manager.Refresh();
            }

            var target = CheckCurrentDir();
            if (target.State == ResultState.Error)
            {
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

                ForAllFiles(SafeIO.DeleteRecursively);

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
                WriteErrorToConsole($"Невозможно создать файл: {destination.ErrorMessage}");
                return;
            }

            ForAllFiles(file => SafeIO
                .GetFullPath(file)
                .AndThen(fullPath => SafeIO.StreamReader(fullPath, encoding))
                .AndThen(reader =>
                {
                    foreach (var maybeBlock in reader.ReadBlocks())
                    {
                        if (maybeBlock.State == ResultState.Error)
                        {
                            return maybeBlock.Map(_ => new object());
                        }

                        foreach (var c in maybeBlock.Value)
                        {
                            destination.Value.Write(c);
                        }
                    }

                    return Result<object>.Ok(new object());
                }));
        }

        private static IEnumerable<string> ReadLinesFromUser()
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
                WriteErrorToConsole($"Невозможно создать файл: {destination.ErrorMessage}");

                Console.WriteLine("Нажмите Enter, чтобы закончить");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Введите строки файла. Чтобы завершить ввод, три раза подряд нажмите Enter.");
            foreach (var line in ReadLinesFromUser())
            {
                if (destination.Value.WriteLineSafe(line) is {State: ResultState.Error, ErrorMessage: var message})
                {
                    WriteErrorToConsole($"О ужас! {message}");

                    Console.WriteLine("Нажмите Enter, чтобы закончить");
                    Console.ReadLine();
                    return;
                }
            }
        }

        private void CreateFile(Encoding? encoding = null)
        {
            if (CheckCurrentDir().State == ResultState.Error)
            {
                return;
            }

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