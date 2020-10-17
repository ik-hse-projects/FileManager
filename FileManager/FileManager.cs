using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly StackContainer List;
        private readonly StackContainer SelectedWidget;
        private readonly HashSet<string> selectedList;
        private readonly Label Header;

        private readonly int panelWidth;
        private readonly BaseContainer rootContainer;

        public FileManager(int maxWidth, int maxHeight)
        {
            currentDirectory = null;
            panelWidth = (maxWidth / 2) - 2;
            List = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            Header = new Label("", maxWidth);
            SelectedWidget = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            selectedList = new HashSet<string>();

            var wrappedList = new RelativePosition(0, 1, 0)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(List));
            var wrappedSelected = new RelativePosition(40, 1, 1)
                .Add(new Frame(Style.DarkGrayOnDefault)
                    .Add(SelectedWidget));
            rootContainer = new BaseContainer()
                .Add(new RelativePosition(0, 0, 1)
                    .Add(Header))
                .AddFocused(wrappedList)
                .Add(wrappedSelected);
            List.AsIKeyHandler().Add(new KeySelector(ConsoleKey.Tab), () => rootContainer.Focused = wrappedSelected);
            SelectedWidget.AsIKeyHandler().Add(new KeySelector(ConsoleKey.Tab), () => rootContainer.Focused = wrappedList);
        }

        private Action CreateAddAction(FileSystemInfo entry)
        {
            return () =>
            {
                if (selectedList.Add(entry.FullName))
                {
                    IKeyHandler btn = new Button(entry.FullName);
                    var delKey = new KeySelector(ConsoleKey.Delete);
                    btn.Add(delKey, () =>
                    {
                        selectedList.Remove(entry.FullName);
                        SelectedWidget.Remove(btn);
                    });
                    SelectedWidget.Add(btn);
                }
            };
        }

        private void UpdateCurrentDir()
        {
            if (currentDirectory == null)
            {
                return;
            }

            List.Add(new Button("..")
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

                List.Add(button);
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
                var fullPath = Path.GetFullPath(to);
                if (Directory.Exists(fullPath))
                {
                    currentDirectory = new DirectoryInfo(fullPath);
                }
            }

            Header.Text = currentDirectory?.FullName ?? "";
            UpdateList();
        }

        private void UpdateList()
        {
            List.Clear();
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
                    List.Add(button);
                }
            }
        }
    }
}