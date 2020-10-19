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
            foreach (var entry in CurrentDirectory.GetFileSystemInfos())
            {
                var action = CreateAddAction(entry);
                var button = (Button) new Button("", panelWidth)
                    .AsIKeyHandler()
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
                switch (isExists)
                {
                    case { State: ResultState.Ok, Value: var directoryInfo }:
                    {
                        Environment.CurrentDirectory = directoryInfo.FullName;
                        CurrentDirectory = directoryInfo;
                        break;
                    }
                    case { State: ResultState.Error, ErrorMessage: var message}:
                    {
                        var popup = new Popup()
                            .Add(new MultilineLabel($"Не удалось сменить директорию: {message}."))
                            .Add(new MultilineLabel("Нажмите «/», чтобы перейти к списку дисков."));
                        var button = new Button("Понятно.");
                        button.AsIKeyHandler().Add(KeySelector.SelectItem, popup.Close);
                        popup.Add(button).AndFocus()
                            .Show(RootContainer);
                        break;
                    }
                }
            }

            header.Text = CurrentDirectory?.FullName ?? "";
            UpdateList();
        }

        private void UpdateList()
        {
            list.Clear();
            if (CurrentDirectory != null)
            {
                UpdateCurrentDir();
            }
            else
            {
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    var button = new Button(drive, panelWidth)
                        .AsIKeyHandler()
                        .Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(drive));
                    list.Add(button);
                }
            }
        }

        public void Refresh()
        {
            UpdateList();
        }
    }
}