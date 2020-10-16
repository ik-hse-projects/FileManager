using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using Thuja.Widgets;

namespace FileManager
{
    public class FileManager
    {
        /// <summary>
        /// Информация о текущей директории
        /// </summary>
        private DirectoryInfo currentDirectory;

        public StackContainer List { get; }
        public StackContainer SelectedWidget { get; }
        private HashSet<string> selectedList;
        public Label Header { get; }

        private readonly int panelWidth;

        public FileManager(int maxWidth, int maxHeight)
        {
            currentDirectory = null;
            panelWidth = (maxWidth / 2) - 2;
            List = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            Header = new Label("", maxWidth);
            SelectedWidget = new StackContainer(Orientation.Vertical, maxVisibleCount: maxHeight - 3);
            selectedList = new HashSet<string>();
        }

        private Action CreateAddAction(FileSystemInfo entry)
        {
            return () =>
            {
                if (selectedList.Add(entry.FullName))
                {
                    var btn = new Button(entry.FullName);
                    var delKey = new KeySelector(ConsoleKey.Delete);
                    btn.Add(delKey, () =>
                    {
                        selectedList.Remove(entry.FullName);
                        btn.Actions.Remove(delKey);
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
            {
                {new KeySelector(ConsoleKey.Enter), () => ChangeDir(currentDirectory.Parent?.FullName)}
            });
            foreach (var entry in currentDirectory.GetFileSystemInfos())
            {
                var action = CreateAddAction(entry);
                var button = new Button("", panelWidth)
                {
                    {new KeySelector(ConsoleKey.Insert), action},
                    {new KeySelector(ConsoleKey.Spacebar), action}
                };

                if (entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    button.Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(entry.FullName));
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
                    {
                        {new KeySelector(ConsoleKey.Enter), () => ChangeDir(drive)}
                    };
                    List.Add(button);
                }
            }
        }
    }
}