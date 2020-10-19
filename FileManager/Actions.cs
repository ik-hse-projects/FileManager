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
    /// <summary>
    /// Реализация всех команд.
    /// </summary>
    public class Actions
    {
        /// <summary>
        /// Файловый менеджер, к которому эти команды относятся.
        /// </summary>
        private readonly FileManager manager;

        /// <summary>
        /// Создает новый экземпляр. Рекомендуется вызывать Attach сразу после этого.
        /// </summary>
        /// <param name="manager">Файловый менеджер, к которому относятся эти команды.</param>
        public Actions(FileManager manager)
        {
            this.manager = manager;
        }

        /// <summary>
        /// Настраивает клавиши, чтобы нажатие на них вызывало команды.
        /// </summary>
        public void Attach()
        {
            manager.RootContainer.AsIKeyHandler()
                // F1 | H: Помощь.
                .Add(new[] {new KeySelector(ConsoleKey.F1), new KeySelector(ConsoleKey.H)}, Help)
                // F2 | R: Прочитать файл в UTF8.
                .Add(new[] {new KeySelector(ConsoleKey.F2), new KeySelector(ConsoleKey.R)},
                    () => ReadFiles(Encoding.UTF8))
                // Shift+R: Прочитать файл в выбранной кодировке.
                .Add(new[] {new KeySelector(ConsoleKey.R, ConsoleModifiers.Shift)},
                    () => ReadFiles())
                // F4 | N: Создать файл в UTF8.
                .Add(new[] {new KeySelector(ConsoleKey.F4), new KeySelector(ConsoleKey.N)},
                    () => CreateFile(Encoding.UTF8))
                // Shift+N: Создать файл в выбранной кодировке.
                .Add(new[] {new KeySelector(ConsoleKey.N, ConsoleModifiers.Shift)},
                    () => CreateFile())
                // F5 | C: Копировать файлы.
                .Add(new[] {new KeySelector(ConsoleKey.F5), new KeySelector(ConsoleKey.C)},
                    CopyFiles)
                // F6 | M: Переместить файлы.
                .Add(new[] {new KeySelector(ConsoleKey.F6), new KeySelector(ConsoleKey.M)},
                    MoveFiles)
                // F8 | Delete | D: Удалить файлы.
                .Add(new[]
                    {
                        new KeySelector(ConsoleKey.F8),
                        new KeySelector(ConsoleKey.Delete),
                        new KeySelector(ConsoleKey.D),
                    },
                    DeleteFiles);
        }

        /// <summary>
        /// Выводит подробную справку в консоль.
        /// </summary>
        private void Help()
        {
            manager.RootContainer.Loop.OnPaused = () =>
            {
                Console.WriteLine("В менеджере есть две панели:");
                Console.WriteLine("    В левой находится список файлов в теущей директории.");
                Console.WriteLine("    В правой список выбранных файлов. Это аргументы команд.");
                Console.WriteLine("    А над ними отображается текущая директория.");
                Console.WriteLine("Для навигации можно и нужно использовать стрелочки и клавиши PageUp, PageDown, Home, End");
                Console.WriteLine("Чтобы выполнить команду, необходимо сначала выбрать файлы или директории при помощи" +
                                  " клавиш Пробел или Ins, после чего нажать соответствующую клавишу.");
                Console.WriteLine("Если необходимо отменить выбор того или иного файла, то нужно перейти в правую панель" +
                                  " при помощи Tab, после чего выделить желаемый файл и нажать Enter или Пробел." +
                                  " После этого можно вернуться в левую панель повторным нажатием Tab.");
                Console.WriteLine("Чтобы перейти в другую директорию, нужно выделить папку в левой панели и нажать Enter." +
                                  " Если выделить **файл** и нажать Enter, то отобразятся некоторые его свойства.");
                Console.WriteLine();
                Console.WriteLine("Правая панель может закрывать список команд. Но всегда можно нажать F1, чтобы увидеть их снова.");
                foreach (var command in Commands)
                {
                    Console.WriteLine($"    {command}");
                }
                Console.WriteLine("\nНажмите Enter чтобы вернуться в файловый менеджер.");
                Console.ReadLine();
            };
        }

        /// <summary>
        /// Список команд и их краткое описание.
        /// </summary>
        public static string[] Commands = {
            "F1 / H — подробная справка",
            "F2 / R — прочитать выбранные файлы в UTF8",
            "Shift+R — выбрать кодировку и прочитать файлы",
            "N — создать файл в UTF8",
            "Shift+N — выбрать кодировку и создать файл",
            "Ins или Пробел — выбрать или отменить выбор",
            "F5 / C — вставить в текущую папку файлы.",
            "F6 / M — переместить выбранные файлы.",
            "F8 / Del / D — удалить все выбранные файлы",
            "F10 / Q — закрыть файловый менеджер",
        };

        /// <summary>
        /// Читает и выводит в консоль файл в выбранной кодировке.
        /// </summary>
        /// <param name="encoding">Выбранная кодировка. Если null, то просит пользователя выбрать.</param>
        private void ReadFiles(Encoding encoding = null)
        {
            AskEncoding(encoding, encoding => manager.RootContainer.Loop.OnPaused = () =>
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
            });
        }

        /// <summary>
        /// Коиприует все выбранные файлы в текущую директорию.
        /// </summary>
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

        /// <summary>
        /// Проверяет, корректна ли текущая директория.
        /// </summary>
        /// <returns>Возвращает информацию о текущей директории.</returns>
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

        /// <summary>
        /// Вспомогательный метод, который выводит сообщение об ошибке в консоль.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        private static void WriteErrorToConsole(string message)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Вспомогательный метод, который выполняет некоторое действие для всех выделенных файлов
        /// и выводит информацию о выполнении в консоль.
        /// </summary>
        /// <param name="action">Действие. В качестве аргмуента ему передается путь к файлу.</param>
        /// <typeparam name="T">Некоторый тип.</typeparam>
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

        /// <summary>
        /// Копирует все выбранные файлы в текущую директорию.
        /// </summary>
        /// <param name="overwrite">Перезаписывать ли существующие файлы.</param>
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

        /// <summary>
        /// Перемещает все выбранные файлы в текущую директорию.
        /// </summary>
        private void MoveFiles()
        {
            var target = CheckCurrentDir();
            if (target.State == ResultState.Error)
            {
                return;
            }

            new Dialog<object>
            {
                Question = $"Точно переместить {manager.SelectedFiles.Count} объектов?",
                Answers = new[] {("Да", new object())},
                OnAnswered = _ => manager.RootContainer.Loop.OnPaused = () =>
                {
                    Console.WriteLine("Перемещаем...");

                    ForAllFiles(file =>
                    {
                        var newLocation = Path.Combine(target.Value.FullName, Path.GetFileName(file));
                        return SafeIO.Move(file, newLocation);
                    });

                    Console.WriteLine("Нажмите Enter, чтобы вернуться в менеджер");
                    Console.ReadLine();
                    manager.Refresh();
                }
            }.Show(manager.RootContainer);
        }

        /// <summary>
        /// Удаляает все выбранные файлы.
        /// </summary>
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

        /// <summary>
        /// Конкатенирует все выбранные файлы и записывает реузльтат в другой файл с выбранной кодировкой.
        /// </summary>
        /// <param name="targetPath">Файл, в который нужно всё записать.</param>
        /// <param name="encoding">Выбранная кодировка.</param>
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

        /// <summary>
        /// Запрашивает у пользователя содержимое файла.
        /// </summary>
        /// <returns>Возвращает итератор по введённым строкам.</returns>
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

        /// <summary>
        /// Создает новый файл с указанной кодировкой на основании введённого пользователем текста.
        /// </summary>
        /// <param name="targetPath">Куда необходимо записать файл.</param>
        /// <param name="encoding">Выбранная кодировка.</param>
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

        /// <summary>
        /// Запрашивает у пользователя кодировку, если необходимо, после чего передаёт её в указанную функцию. 
        /// </summary>
        /// <param name="encoding">Кодировка, если она уже известна. Если null, то спрашивает пользователя.</param>
        /// <param name="then">Действие, которое необходимо выполнить после того, как станет известна кодировка.</param>
        private void AskEncoding(Encoding? encoding, Action<Encoding> then)
        {
            if (encoding != null)
            {
                then(encoding);
            }
            else
            {
                // UTF8 and all other available
                var encodings = new[] {("UTF8", Encoding.UTF8)}
                    .Concat(
                        Encoding.GetEncodings()
                            .Where(info => info.CodePage != Encoding.UTF8.CodePage)
                            .Select(info => (info.DisplayName.ToString(), info.GetEncoding()))
                    ).ToArray();
                new Dialog<Encoding>
                {
                    Question = "Выберите кодировку",
                    Answers = encodings,
                    OnAnswered = then
                }.Show(manager.RootContainer);
            }
        }

        /// <summary>
        /// Создает новый файл, спрашивая у пользователя все необходимые детали.
        /// </summary>
        /// <param name="encoding">Кодировка файла, если она известна. Иначе null.</param>
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

            AskMode(mode => AskEncoding(encoding,
                encoding => AskFilename(
                    filename => manager.RootContainer.Loop.OnPaused = () =>
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