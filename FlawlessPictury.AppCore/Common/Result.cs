using System;

namespace FlawlessPictury.AppCore.Common
{
    /// <summary>
    /// Represents the outcome of an operation that can either succeed or fail with a structured <see cref="Error"/>.
    /// </summary>
    public sealed class Result
    {
        private Result(bool isSuccess, Error error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>Gets a value indicating whether the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>Gets a value indicating whether the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the error when <see cref="IsFailure"/> is true; otherwise null.
        /// </summary>
        public Error Error { get; }

        /// <summary>Creates a successful result.</summary>
        public static Result Ok()
        {
            return new Result(true, null);
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="error">The structured error.</param>
        public static Result Fail(Error error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            return new Result(false, error);
        }
    }

    /// <summary>
    /// Represents the outcome of an operation that returns a value on success or an <see cref="Error"/> on failure.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    public sealed class Result<T>
    {
        private Result(bool isSuccess, T value, Error error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        /// <summary>Gets a value indicating whether the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>Gets a value indicating whether the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the returned value when <see cref="IsSuccess"/> is true.
        /// </summary>
        /// <remarks>
        /// Accessing <see cref="Value"/> when <see cref="IsFailure"/> is true is a caller bug.
        /// </remarks>
        public T Value { get; }

        /// <summary>
        /// Gets the error when <see cref="IsFailure"/> is true; otherwise null.
        /// </summary>
        public Error Error { get; }

        /// <summary>Creates a successful result with a value.</summary>
        public static Result<T> Ok(T value)
        {
            return new Result<T>(true, value, null);
        }

        /// <summary>Creates a failed result.</summary>
        public static Result<T> Fail(Error error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            return new Result<T>(false, default(T), error);
        }
    }
}
