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
        /// <summary>
        /// Информация о текущей директории
        /// </summary>
        private DirectoryInfo currentDirectory;

        public IFocusable RootContainer => rootContainer;

        private readonly StackContainer list;
        private readonly StackContainer selectedWidget;
        private readonly OrderedSet<string> selectedSet;
        private readonly Label header;

        private readonly int panelWidth;
        private readonly BaseContainer rootContainer;

        public FileManager(int maxWidth, int maxHeight)
        {
            currentDirectory = null;
            panelWidth = (maxWidth / 2) - 2;
            list = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            header = new Label("", maxWidth);
            selectedWidget = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            selectedSet = new OrderedSet<string>();

            var wrappedList = new RelativePosition(0, 1, 0)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(list));
            var wrappedSelected = new RelativePosition(40, 1, 1)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(selectedWidget));
            rootContainer = new BaseContainer()
                .Add(new RelativePosition(0, 0, 1)
                    .Add(header))
                .AddFocused(wrappedList)
                .Add(wrappedSelected);
            rootContainer.AsIKeyHandler()
                .Add(new[] {new KeySelector(ConsoleKey.F10), new KeySelector(ConsoleKey.Escape)},
                    () => rootContainer.Loop.OnStop = () => Console.WriteLine("До новых встреч!"));
            list.AsIKeyHandler()
                .Add(new[] {new KeySelector('/'), new KeySelector('\\')}, () => ChangeDir(null))
                .Add(new KeySelector(ConsoleKey.Tab), () => rootContainer.Focused = wrappedSelected);
            selectedWidget.AsIKeyHandler()
                .Add(new KeySelector(ConsoleKey.Tab), () => rootContainer.Focused = wrappedList);
        }

        public void AttachActions()
        {
            new Actions(rootContainer, selectedSet).Attach();
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
                        new KeySelector(ConsoleKey.Backspace),
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
            if (currentDirectory == null)
            {
                return;
            }

            list.Add(new Button("..")
                .AsIKeyHandler()
                .Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(currentDirectory.Parent?.FullName)));
            foreach (var entry in currentDirectory.GetFileSystemInfos())
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

        public void ChangeDir(string to)
        {
            if (to == null)
            {
                currentDirectory = null;
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
                    // Exists
                    case { State: ResultState.Ok, Value: var directoryInfo }:
                    {
                        currentDirectory = directoryInfo;
                        break;
                    }
                    case { State: ResultState.Error, ErrorMessage: var message}:
                        // TODO: Show error message
                        break;
                }
            }

            header.Text = currentDirectory?.FullName ?? "";
            UpdateList();
        }

        private void UpdateList()
        {
            list.Clear();
            if (currentDirectory != null)
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
    }
}