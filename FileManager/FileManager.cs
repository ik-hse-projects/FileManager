using System;
using System.IO;
using Thuja.Widgets;

namespace FileManager
{
    public class FileManager
    {
        /// <summary>
        /// Информация о текущей директории
        /// </summary>
        private DirectoryInfo currentDirectory;

        private readonly StackContainer list;

        public FileManager(StackContainer list)
        {
            currentDirectory = null;
            this.list = list;
        }

        private Action CreateAddAction(FileSystemInfo entry)
        {
            // TODO
            return () => { };
        }

        private void UpdateCurrentDir()
        {
            if (currentDirectory == null)
            {
                return;
            }

            list.Add(new Button("..")
            {
                {new KeySelector(ConsoleKey.Enter), () => ChangeDir(currentDirectory.Parent?.FullName)}
            });
            foreach (var entry in currentDirectory.GetFileSystemInfos())
            {
                var action = CreateAddAction(entry);
                var button = new Button("", 38)
                {
                    {new KeySelector(ConsoleKey.Insert), action},
                    {new KeySelector(ConsoleKey.Spacebar), action}
                };

                if (entry.Attributes.HasFlag(FileAttributes.Normal))
                {
                    button.Text = $"{entry.Name}";
                }
                else if (entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    button.Add(new KeySelector(ConsoleKey.Enter), () => ChangeDir(entry.FullName));
                    button.Text = $"{entry.Name}/";
                }
                else
                {
                    button.Text = $"{entry.Name} (?)";
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
                var fullPath = Path.GetFullPath(to);
                if (Directory.Exists(fullPath))
                {
                    currentDirectory = new DirectoryInfo(fullPath);
                }
            }

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
                    var button = new Button(drive, 38)
                    {
                        {new KeySelector(ConsoleKey.Enter), () => ChangeDir(drive)}
                    };
                    list.Add(button);
                }
            }
        }
    }
}