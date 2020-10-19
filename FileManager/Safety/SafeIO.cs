using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace FileManager.Safety
{
    public static class SafeIO
    {
        /// <summary>
        ///     Определяет, указывает ли заданный путь на существующий каталог на диске.
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <returns>
        ///     true, если path ссылается на существующий каталог; значение false, если каталог не существует
        ///     или если при попытке определить, существует ли указанный каталог, произошла ошибка.
        /// </returns>
        public static Result<bool> Exists(string path)
        {
            return Result<bool>.Ok(Directory.Exists(path));
        }

        /// <summary>
        ///     Возвращает для указанной строки пути абсолютный путь.
        /// </summary>
        /// <param name="path">Файл или каталог, для которых нужно получить сведения об абсолютном пути.</param>
        /// <returns>Полное расположение path, например "C:\MyFile.txt".</returns>
        public static Result<string> GetFullPath(string path)
        {
            try
            {
                return Result<string>.Ok(Path.GetFullPath(path));
            }
            catch (ArgumentNullException)
            {
                return Result<string>.Error("Некорректный путь");
            }
            catch (ArgumentException)
            {
                return Result<string>.Error("Не удалось извлечь абсолютный путь.");
            }
            catch (SecurityException)
            {
                return Result<string>.Error("Отсутствуют необходимые разрешения.");
            }
            catch (NotSupportedException)
            {
                return Result<string>.Error(
                    "Путь содержит двоеточие (:), которое не является частью идентификатора диска.");
            }
            catch (PathTooLongException)
            {
                return Result<string>.Error("Путь слишком велик.");
            }
            catch (Exception)
            {
                return Result<string>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Выполняет инициализацию нового экземпляра класса DirectoryInfo для заданного пути.
        /// </summary>
        /// <param name="path">Строка, содержащая путь, для которого создается класс DirectoryInfo.</param>
        /// <returns>Новый экземпляра класса DirectoryInfo для заданного пути.</returns>
        public static Result<DirectoryInfo> DirectoryInfo(string? path)
        {
            if (path == null)
            {
                return Result<DirectoryInfo>.Error("Некорректный путь");
            }

            try
            {
                return Result<DirectoryInfo>.Ok(new DirectoryInfo(path));
            }
            catch (ArgumentException)
            {
                return Result<DirectoryInfo>.Error("Некорректный путь");
            }
            catch (SecurityException)
            {
                return Result<DirectoryInfo>.Error("Отсутствует необходимое разрешение.");
            }
            catch (PathTooLongException)
            {
                return Result<DirectoryInfo>.Error("Путь слишком велик.");
            }
            catch (Exception)
            {
                return Result<DirectoryInfo>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Инициализирует новый экземпляр класса StreamReader для заданного имени файла, используя указанную кодировку
        ///     символов.
        /// </summary>
        /// <param name="path">Полный путь к файлу для чтения.</param>
        /// <param name="encoding">Кодировка символов, которую нужно использовать.</param>
        /// <returns>Новый экземпляр класса StreamReader для заданного имени файла, используя указанную кодировку символов.</returns>
        public static Result<StreamReader> StreamReader(string path, Encoding encoding)
        {
            try
            {
                return Result<StreamReader>.Ok(new StreamReader(path, encoding));
            }
            catch (ArgumentException)
            {
                return Result<StreamReader>.Error("Некорректный путь");
            }
            catch (NotSupportedException)
            {
                return Result<StreamReader>.Error("Некорректный путь");
            }
            catch (FileNotFoundException)
            {
                return Result<StreamReader>.Error("Файл не найден");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<StreamReader>.Error("Файл не найден");
            }
            catch (IOException)
            {
                return Result<StreamReader>.Error("Не удалось открыть файл");
            }
            catch (Exception)
            {
                return Result<StreamReader>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Читает блоки (итераторы по символам) из потока.
        /// </summary>
        public static IEnumerable<Result<IEnumerable<char>>> ReadBlocks(this StreamReader reader)
        {
            var buffer = new char[16 * 1024];
            var count = 0;
            while (true)
            {
                int bytes;
                try
                {
                    bytes = reader.ReadBlock(buffer, 0, buffer.Length);
                }
                catch (Exception)
                {
                    bytes = -1;
                }

                switch (bytes)
                {
                    case -1:
                        yield return Result<IEnumerable<char>>.Error("Невозможно прочесть файл");
                        yield break;
                    case 0:
                        yield break;
                }

                count++;
                if (bytes == buffer.Length)
                {
                    yield return Result<IEnumerable<char>>.Ok(buffer);

                    // Ограничение в примерно 100MB.
                    if (count > 6400)
                    {
                        yield return Result<IEnumerable<char>>.Error("Файл слишком большой, часть была пропущена.");
                        yield break;
                    }
                }
                else
                {
                    var smallerBuffer = buffer.Take(bytes);
                    yield return Result<IEnumerable<char>>.Ok(smallerBuffer);
                    yield break;
                }
            }
        }

        /// <summary>
        ///     Копирует существующий файл в новый файл. Перезаписывает файлы с тем же именем в зависимости от overwrite.
        /// </summary>
        /// <param name="from">Копируемый файл.</param>
        /// <param name="to">Имя целевого файла.</param>
        /// <param name="overwrite">true, если конечный файл можно перезаписывать; в противном случае — false.</param>
        public static Result<object> CopyFile(string from, string to, bool overwrite)
        {
            if (to == from)
            {
                return Result<object>.Error("Нельзя копировать файл из одного места в то же самое.");
            }

            try
            {
                File.Copy(from, to, overwrite);
                return Result<object>.Ok(new object());
            }
            catch (UnauthorizedAccessException)
            {
                return Result<object>.Error("Отсутствует необходимое разрешение.");
            }
            catch (ArgumentException)
            {
                return Result<object>.Error("Некорректный путь.");
            }
            catch (PathTooLongException)
            {
                return Result<object>.Error("Путь слишком велик.");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<object>.Error("Путь не существует.");
            }
            catch (FileNotFoundException)
            {
                return Result<object>.Error($"Не удалось найти путь `{from}`.");
            }
            catch (IOException)
            {
                return Result<object>.Error("Произошла ошибка ввода-вывода или невозможно перезаписать.");
            }
            catch (NotSupportedException)
            {
                return Result<object>.Error("Путь имеет недопустимый формат.");
            }
            catch (Exception)
            {
                return Result<object>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Удаляет объект по указанному пути полностью.
        /// </summary>
        public static Result<object> DeleteRecursively(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return Result<object>.Ok(new object());
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                    return Result<object>.Ok(new object());
                }

                return Result<object>.Error("Файл не найден или он не поддерживается.");
            }
            catch (UnauthorizedAccessException)
            {
                return Result<object>.Error("Отсутствует необходимое разрешение.");
            }
            catch (ArgumentException)
            {
                return Result<object>.Error("Некорректный путь.");
            }
            catch (PathTooLongException)
            {
                return Result<object>.Error("Путь слишком велик.");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<object>.Error("Путь не существует.");
            }
            catch (IOException)
            {
                return Result<object>.Error("Невозможно удалить.");
            }
            catch (NotSupportedException)
            {
                return Result<object>.Error("Неподдерживаемый файл.");
            }
            catch (Exception)
            {
                return Result<object>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Перемещает файл или директорию из одного места в другое.
        /// </summary>
        public static Result<object> Move(string? from, string? to)
        {
            try
            {
                if (Directory.Exists(from))
                {
                    Directory.Move(from, to);
                    return Result<object>.Ok(new object());
                }

                if (File.Exists(from))
                {
                    File.Move(from, to);
                    return Result<object>.Ok(new object());
                }

                return Result<object>.Error("Файл не найден или он не поддерживается.");
            }
            catch (UnauthorizedAccessException)
            {
                return Result<object>.Error("Отсутствует необходимое разрешение.");
            }
            catch (ArgumentException)
            {
                return Result<object>.Error("Некорректный путь.");
            }
            catch (PathTooLongException)
            {
                return Result<object>.Error("Путь слишком велик.");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<object>.Error("Путь не существует.");
            }
            catch (IOException)
            {
                return Result<object>.Error("Невозможно переместить.");
            }
            catch (Exception)
            {
                return Result<object>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Создаёт файл по указанному пути и возвращает поток для записи в него.
        /// </summary>
        public static Result<StreamWriter> CreateFile(string path, Encoding encoding)
        {
            try
            {
                return Result<StreamWriter>.Ok(new StreamWriter(File.Create(path), encoding));
            }
            catch (UnauthorizedAccessException)
            {
                return Result<StreamWriter>.Error("Отсутствует необходимое разрешение.");
            }
            catch (ArgumentException)
            {
                return Result<StreamWriter>.Error("Некорректный путь.");
            }
            catch (PathTooLongException)
            {
                return Result<StreamWriter>.Error("Путь слишком велик.");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<StreamWriter>.Error("Путь не существует.");
            }
            catch (IOException)
            {
                return Result<StreamWriter>.Error("Невозможно удалить.");
            }
            catch (NotSupportedException)
            {
                return Result<StreamWriter>.Error("Неподдерживаемый файл.");
            }
            catch (Exception)
            {
                return Result<StreamWriter>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Записывает в поток переданную строчку.
        /// </summary>
        public static Result<object> WriteLineSafe(this StreamWriter writer, string line)
        {
            try
            {
                writer.WriteLine(line);
                return Result<object>.Ok(new object());
            }
            catch (Exception)
            {
                return Result<object>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Получает информацию об объектах в указанной директории.
        /// </summary>
        public static Result<FileSystemInfo[]> SafeFileSystemInfos(this DirectoryInfo info)
        {
            try
            {
                return Result<FileSystemInfo[]>.Ok(info.GetFileSystemInfos());
            }
            catch (DirectoryNotFoundException)
            {
                return Result<FileSystemInfo[]>.Error("Путь не существует.");
            }
            catch (Exception)
            {
                return Result<FileSystemInfo[]>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Меняет текущую директорию на указанную.
        /// </summary>
        public static Result<object> ChangeCurrentDirectory(string path)
        {
            try
            {
                Environment.CurrentDirectory = path;
                return Result<object>.Ok(new object());
            }
            catch (UnauthorizedAccessException)
            {
                return Result<object>.Error("Отсутствует необходимое разрешение.");
            }
            catch (Exception)
            {
                return Result<object>.Error("Неопознанная ошибка");
            }
        }

        /// <summary>
        ///     Получает список дисков.
        /// </summary>
        public static Result<string[]> GetLogicalDrives()
        {
            try
            {
                return Result<string[]>.Ok(Environment.GetLogicalDrives());
            }
            catch (UnauthorizedAccessException)
            {
                return Result<string[]>.Error("Отсутствует необходимое разрешение.");
            }
            catch (IOException)
            {
                return Result<string[]>.Error("Ошибка ввода-вывода.");
            }
            catch (Exception)
            {
                return Result<string[]>.Error("Неопознанная ошибка");
            }
        }
    }
}