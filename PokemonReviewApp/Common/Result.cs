namespace PokemonReviewApp.Common
{
    /// <summary>
    /// Outcome of an operation that either succeeded or failed with a known <see cref="Error"/>.
    /// Expected failures ("this category already exists") travel back as values, which keeps
    /// exceptions — and the global exception handler — for the genuinely unexpected.
    /// </summary>
    public class Result
    {
        protected Result(bool isSuccess, Error error)
        {
            // A success carrying an error, or a failure carrying none, means the caller
            // built the result wrong. Fail loudly here rather than shipping a lie.
            if (isSuccess && error != Error.None)
                throw new InvalidOperationException("A successful result cannot carry an error.");

            if (!isSuccess && error == Error.None)
                throw new InvalidOperationException("A failed result must carry an error.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public Error Error { get; }

        public static Result Success() => new(true, Error.None);

        public static Result Failure(Error error) => new(false, error);

        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

        public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

        /// <summary>
        /// Wraps a possibly-null value: null becomes a failure instead of a NullReferenceException
        /// three frames later.
        /// </summary>
        public static Result<TValue> Create<TValue>(TValue? value, Error? notFound = null) =>
            value is not null
                ? Success(value)
                : Failure<TValue>(notFound ?? Error.NullValue);
    }

    /// <summary>
    /// A <see cref="Result"/> that carries a value when it succeeds.
    /// </summary>
    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        protected internal Result(TValue? value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        /// <summary>
        /// The produced value. Reading it on a failed result is a bug in the caller —
        /// check <see cref="Result.IsSuccess"/> first.
        /// </summary>
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

        /// <summary>Lets a handler `return someValue;` where a Result&lt;T&gt; is expected.</summary>
        public static implicit operator Result<TValue>(TValue value) => Success(value);
    }
}
