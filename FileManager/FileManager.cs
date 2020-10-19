using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileManager.Safety;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    /// <summary>
    /// Файловый менеджер.
    /// </summary>
    public class FileManager
    {
        /// <summary>
        /// Текстовое поле, содержащее путь к текущей директории.
        /// </summary>
        private readonly Label header;

        /// <summary>
        /// Список файлов в текущей директории.
        /// </summary>
        private readonly StackContainer list;

        /// <summary>
        /// Ширина каждой из панелей.
        /// </summary>
        private readonly int panelWidth;
        
        /// <summary>
        /// Множество выбранных файлов и директорий.
        /// </summary>
        private readonly OrderedSet<string> selectedSet;
        
        /// <summary>
        /// Виджет, в котором отображается список выбранных файлов и директорий.
        /// </summary>
        private readonly StackContainer selectedWidget;

        /// <summary>
        /// Создает новый экземпляр файлового менеджера.
        /// </summary>
        /// <param name="maxWidth">Максимальная используемая ширина.</param>
        /// <param name="maxHeight">Максимальная используемая высота.</param>
        public FileManager(int maxWidth, int maxHeight)
        {
            CurrentDirectory = null;
            panelWidth = maxWidth / 2 - 2;
            list = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            header = new Label("", maxWidth);
            selectedWidget = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            selectedSet = new OrderedSet<string>();

            var wrappedList = new RelativePosition(0, 1)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(list));
            var wrappedSelected = new RelativePosition(40, 1, 1)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(selectedWidget));
            RootContainer = new BaseContainer()
                .Add(new RelativePosition(0, 0, 1)
                    .Add(header))
                .AddFocused(wrappedList)
                .Add(wrappedSelected);
            RootContainer.AsIKeyHandler()
                .Add(new[] {new KeySelector(ConsoleKey.F10), new KeySelector(ConsoleKey.Q)},
                    () => RootContainer.Loop.OnStop = () => Console.WriteLine("До новых встреч!"));
            list.AsIKeyHandler()
                .Add(new[] {new KeySelector('/'), new KeySelector('\\')}, () => ChangeDir(null))
                .Add(new KeySelector(ConsoleKey.Tab), () => RootContainer.Focused = wrappedSelected);
            selectedWidget.AsIKeyHandler()
                .Add(new KeySelector(ConsoleKey.Tab), () => RootContainer.Focused = wrappedList);
        }

        /// <summary>
        ///     Информация о текущей директории
        /// </summary>
        public DirectoryInfo? CurrentDirectory { get; private set; }

        /// <summary>
        /// Коллекция выбранных пользователем файлов.
        /// </summary>
        public IReadOnlyCollection<string> SelectedFiles => selectedSet;

        /// <summary>
        /// Контейнер, в которомо находятся все виджеты менеджера.
        /// </summary>
        public BaseContainer RootContainer { get; }

        /// <summary>
        /// Регистрирует и настраивает все возможные команды.
        /// </summary>
        public void AttachActions()
        {
            new Actions(this).Attach();
        }

        /// <summary>
        /// Возвращает действие, котороео добавит указанную запись в список выбранных объектов.
        /// </summary>
        /// <param name="entry">Запись, которую потребуется добавить.</param>
        /// <returns>Действие, которое добавит запись.</returns>
        private Action CreateAddToSelectedAction(FileSystemInfo entry)
        {
            return () =>
            {
                if (!selectedSet.Contains(entry.FullName))
                {
                    IKeyHandler btn = new Button(entry.FullName);
                    btn.Add(new[]
                    {
                        new KeySelector(ConsoleKey.Spacebar),
                        new KeySelector(ConsoleKey.Enter),
                        new KeySelector(ConsoleKey.Insert),
                        new KeySelector(ConsoleKey.Delete),
                    }, () =>
                    {
                        selectedSet.Remove(entry.FullName);
                        selectedWidget.Remove(btn);
                    });
                    selectedSet.Add(entry.FullName);
                    selectedWidget.Add(btn);
                }
            };
        }

        /// <summary>
        /// Обновляет список файлов в панели со списком файлов в текущей директории.
        /// </summary>
        private void UpdateCurrentDir()
        {
            if (CurrentDirectory == null)
            {
                return;
            }

            list.Add(new Button("..")
                .AsIKeyHandler()
                .Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(CurrentDirectory.Parent?.FullName)));
            var infos = CurrentDirectory.SafeFileSystemInfos();
            if (infos.State == ResultState.Error)
            {
                ShowError(infos, "Не удалось получить список файлов",
                    "Попробуйте перейти в директорию выше или нажмите «/», чтобы вернуться к списку дисков");
                return;
            }

            foreach (var entry in infos.Value.OrderBy(i => i?.Name))
            {
                var action = CreateAddToSelectedAction(entry);
                var button = new Button("", panelWidth);
                button.AsIKeyHandler()
                    .Add(new KeySelector(ConsoleKey.Insert), action)
                    .Add(new KeySelector(ConsoleKey.Spacebar), action);

                if (entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    button.AsIKeyHandler()
                        .Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(entry.FullName));
                    button.Text = $"{entry.Name}/";
                }
                else
                {
                    button.AsIKeyHandler()
                        .Add(new KeySelector(ConsoleKey.Enter), () => ShowInfo(entry.FullName));
                    button.Text = $"{entry.Name}";
                }

                list.Add(button);
            }
        }

        /// <summary>
        /// Отображает некоторые свойства файла.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        private void ShowInfo(string path)
        {
            var info = new FileInfo(path);
            var isReadonly = info.IsReadOnly ? "да" : "нет";
            new Popup()
                .Add(new Label($"{info.Name}") {CurrentStyle = Style.Active})
                .Add(new Label($"Размер: {info.Length}"))
                .Add(new Label($"Только для чтения: {isReadonly}"))
                .Add(new Label($"Расширение: {info.Extension}"))
                .Add(new Label($"Создан: {info.CreationTime}"))
                .Add(new Label($"Изменён: {info.LastWriteTime}"))
                .AddClose("Закрыть").AndFocus()
                .Show(RootContainer);
        }

        /// <summary>
        /// Меняет текущую директорию.
        /// </summary>
        /// <param name="to">В какую директорию нужно перейти. null, если к списку дисков.</param>
        public void ChangeDir(string? to)
        {
            if (to == null)
            {
                CurrentDirectory = null;
            }
            else
            {
                var isExists = SafeIO
                    .GetFullPath(to)
                    .AndThen(fullPath => SafeIO
                        .Exists(fullPath)
                        .AndThen(isExists => isExists
                            ? Result<string>.Ok(fullPath)
                            : Result<string>.Error("Невозможно перейти в указанную директорию"))
                    )
                    .AndThen(SafeIO.DirectoryInfo);
                var chDir = isExists
                    .AndThen(dirInfo => SafeIO.ChangeCurrentDirectory(dirInfo.FullName));
                ShowError(isExists, "Не удалось сменить директорию", "Нажмите «/», чтобы перейти к списку дисков.");
                if (isExists is { State: ResultState.Ok, Value: var directoryInfo })
                {
                    ShowError(chDir, "Не удалось сменить директорию", "Нажмите «/», чтобы перейти к списку дисков.");
                    if (chDir.State == ResultState.Ok)
                    {
                        CurrentDirectory = directoryInfo;
                    }
                }
            }

            header.Text = CurrentDirectory?.FullName ?? "";
            Refresh();
        }

        /// <summary>
        /// Обновляет левую панель.
        /// </summary>
        public void Refresh()
        {
            list.Clear();
            if (CurrentDirectory != null)
            {
                UpdateCurrentDir();
            }
            else
            {
                var maybeDrives = SafeIO.GetLogicalDrives();
                ShowError(maybeDrives, "Не удалось получить список дисков", "Нажмите «/», чтобы попробовать ещё раз.");
                if (maybeDrives is {State: ResultState.Ok, Value: var drives})
                {
                    foreach (var drive in drives)
                    {
                        var button = new Button(drive, panelWidth)
                            .AsIKeyHandler()
                            .Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(drive));
                        list.Add(button);
                    }
                }
            }
        }

        /// <summary>
        /// Отображает пользователю диалог с информацией об ошибке, если таковая произошла.
        /// </summary>
        /// <param name="result">Результат выполнения некоторого действия, которое, возможно, обернулось неудачей.</param>
        /// <param name="description">Описание ошибки.</param>
        /// <param name="recommendation">Рекомендации: что может помочь пользователю.</param>
        /// <typeparam name="T">Некоторый тип.</typeparam>
        private void ShowError<T>(Result<T> result, string description, string recommendation = "")
        {
            if (result is {State: ResultState.Error, ErrorMessage: var error})
            {
                new Popup()
                    .Add(new MultilineLabel($"{description}: {error}"))
                    .Add(new MultilineLabel(recommendation))
                    .AddClose("Понятно.").AndFocus()
                    .Show(RootContainer);
            }
        }
    }
}