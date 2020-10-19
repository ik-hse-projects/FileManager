using System;

namespace FileManager.Safety
{
    /// <summary>
    ///     Состояние содержимого Result: успешно ли было выполнено действие или же произошла ошибка.
    /// </summary>
    public enum ResultState
    {
        /// <summary>
        ///     Всё хорошо.
        /// </summary>
        Ok,

        /// <summary>
        ///     Произошла ошибка
        /// </summary>
        Error
    }

    /// <summary>
    ///     Обёртка вокруг произвольног значения, которая либо содержит его, либо содержит информацию об ошибке.
    /// </summary>
    /// <typeparam name="T">Тип внутреннего значения.</typeparam>
    public class Result<T> : IDisposable
    {
        /// <summary>
        ///     Сообщение об ошибке, если таковая произошла. Иначе null.
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        ///     Состояние содержимого.
        /// </summary>
        public readonly ResultState State;

        /// <summary>
        ///     Внутреннее значение, если ошибка не произошла. Иначе null.
        /// </summary>
        public readonly T Value;

        /// <summary>
        ///     Приватный конструктор, который заполняет все поля
        /// </summary>
        private Result(ResultState state, T value, string errorMessage)
        {
            State = state;
            Value = value;
            ErrorMessage = errorMessage;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (State == ResultState.Ok && Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        ///     Создает новый экземпляр Result, который содержит успешно вычисленное значение и не содержит ошибки.
        /// </summary>
        /// <param name="value">Внутренее значение.</param>
        /// <returns>Новый экземпляр Result.</returns>
        public static Result<T> Ok(T value)
        {
            return new Result<T>(ResultState.Ok, value, null!);
        }

        /// <summary>
        ///     Создает новый экземпляр Result, который содержит сообщение об ошибке и не содержит значения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <returns>Новый экземпляр Result.</returns>
        public static Result<T> Error(string message)
        {
            return new Result<T>(ResultState.Error, default!, message);
        }

        /// <summary>
        ///     Применяет переданную функцию к внутреннему значению, если оно есть.
        ///     Если ранее произошла ошибка, то оставляет всё как есть.
        /// </summary>
        /// <param name="func">Функция, которая совершает вычисление над внутренним значением.</param>
        /// <typeparam name="U">Тип нового значения.</typeparam>
        /// <returns>Новый экземпляр Result с другим внутренним значением.</returns>
        public Result<U> Map<U>(Func<T, U> func)
        {
            return State switch
            {
                ResultState.Ok => Result<U>.Ok(func(Value)),
                ResultState.Error => Result<U>.Error(ErrorMessage)
            };
        }

        /// <summary>
        ///     Если раннее ошибки не произошло, то возвращает результат применения функции к внутреннему значению.
        ///     Иначе возвращает Result, который содержит старое сообщение об ошибке.
        /// </summary>
        /// <param name="func">Функция, которая будет применена к значению, если оно есть.</param>
        /// <typeparam name="U">Тип нового значения.</typeparam>
        /// <returns>Новый экземпляр Result с другим внутренним значением и, возможно, другим сообщением об ошибке.</returns>
        public Result<U> AndThen<U>(Func<T, Result<U>> func)
        {
            return State switch
            {
                ResultState.Ok => func(Value),
                ResultState.Error => Result<U>.Error(ErrorMessage)
            };
        }
    }
}