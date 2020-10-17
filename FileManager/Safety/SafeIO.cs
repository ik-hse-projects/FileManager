using System;
using System.IO;
using System.Security;

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
        }
    }
}