using System;

namespace FileManager.Safety
{
    public enum ResultState
    {
        Ok,
        Error
    }

    public class Result<T> : IDisposable
    {
        public readonly string ErrorMessage;
        public readonly ResultState State;
        public readonly T Value;

        private Result(ResultState state, T value, string errorMessage)
        {
            State = state;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public void Dispose()
        {
            if (State == ResultState.Ok && Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public static Result<T> Ok(T value)
        {
            return new Result<T>(ResultState.Ok, value, null!);
        }

        public static Result<T> Error(string message)
        {
            return new Result<T>(ResultState.Error, default!, message);
        }

        public Result<U> Map<U>(Func<T, U> func)
        {
            return State switch
            {
                ResultState.Ok => Result<U>.Ok(func(Value)),
                ResultState.Error => Result<U>.Error(ErrorMessage)
            };
        }

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