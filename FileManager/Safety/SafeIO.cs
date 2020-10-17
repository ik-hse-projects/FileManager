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
        /// Определяет, указывает ли заданный путь на существующий каталог на диске.
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <returns>
        /// true, если path ссылается на существующий каталог; значение false, если каталог не существует
        /// или если при попытке определить, существует ли указанный каталог, произошла ошибка.
        /// </returns>
        public static Result<bool> Exists(string path)
        {
            return Result<bool>.Ok(Directory.Exists(path));
        }

        /// <summary>
        /// Возвращает для указанной строки пути абсолютный путь.
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
        /// Выполняет инициализацию нового экземпляра класса DirectoryInfo для заданного пути.
        /// </summary>
        /// <param name="path">Строка, содержащая путь, для которого создается класс DirectoryInfo.</param>
        /// <returns>Новый экземпляра класса DirectoryInfo для заданного пути.</returns>
        public static Result<DirectoryInfo> DirectoryInfo(string path)
        {
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
        /// Инициализирует новый экземпляр класса StreamReader для заданного имени файла, используя указанную кодировку символов.
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

        public static IEnumerable<Result<IEnumerable<char>>> ReadBlocks(this StreamReader reader)
        {
            var buffer = new char[16 * 1024];
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

                if (bytes == buffer.Length)
                {
                    yield return Result<IEnumerable<char>>.Ok(buffer);
                }
                else
                {
                    var smallerBuffer = buffer.Take(bytes);
                    yield return Result<IEnumerable<char>>.Ok(smallerBuffer);
                    yield break;
                }
            }
        }
    }
}