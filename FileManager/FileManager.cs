using System;
using System.Collections.Generic;
using System.IO;
using FileManager.Safety;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    public class FileManager
    {
        private readonly Label header;

        private readonly StackContainer list;

        private readonly int panelWidth;
        private readonly OrderedSet<string> selectedSet;
        private readonly StackContainer selectedWidget;

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
                .Add(new[] {new KeySelector(ConsoleKey.F10), new KeySelector(ConsoleKey.Escape)},
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

        public IReadOnlyCollection<string> SelectedFiles => selectedSet;

        public BaseContainer RootContainer { get; }

        public void AttachActions()
        {
            new Actions(this).Attach();
        }

        private Action CreateAddAction(FileSystemInfo entry)
        {
            return () =>
            {
                if (!selectedSet.Contains(entry.FullName))
                {
                    IKeyHandler btn = new Button(entry.FullName);
                    btn.Add(new[]
                    {
                        new KeySelector(ConsoleKey.Delete),
                        new KeySelector(ConsoleKey.Backspace)
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

            foreach (var entry in infos.Value)
            {
                var action = CreateAddAction(entry);
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
                    button.Text = $"{entry.Name}";
                }

                list.Add(button);
            }
        }

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

        private Result<T> ShowError<T>(Result<T> result, string description, string recommendation = "")
        {
            if (result is {State: ResultState.Error, ErrorMessage: var error})
            {
                var popup = new Popup()
                    .Add(new MultilineLabel($"{description}: {error}"))
                    .Add(new MultilineLabel(recommendation));
                var button = new Button("Понятно.");
                button.AsIKeyHandler().Add(KeySelector.SelectItem, popup.Close);
                popup.Add(button).AndFocus()
                    .Show(RootContainer);
            }

            return result;
        }
    }
}