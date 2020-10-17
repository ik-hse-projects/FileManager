using System;

namespace FileManager.Safety
{
    public enum ResultState
    {
        Ok,
        Error
    }

    public class Result<T>
    {
        public readonly ResultState State;
        public readonly T Value;
        public readonly string ErrorMessage;

        private Result(ResultState state, T value, string errorMessage)
        {
            State = state;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Ok(T value)
        {
            return new Result<T>(ResultState.Ok, value, null);
        }

        public static Result<T> Error(string message)
        {
            return new Result<T>(ResultState.Error, default, message);
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