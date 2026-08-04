namespace PokemonReviewApp.Common
{
    /// <summary>
    /// Combinators for chaining work onto a <see cref="Result{TValue}"/>. Each one runs only
    /// on success and passes an existing failure straight through, which is what lets a
    /// handler read as a single expression instead of a ladder of null checks.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>Transforms the value of a successful result — an entity into its DTO, typically.</summary>
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper) =>
            result.IsSuccess
                ? Result.Success(mapper(result.Value))
                : Result.Failure<TOut>(result.Error);

        /// <inheritdoc cref="Map{TIn,TOut}(Result{TIn}, Func{TIn,TOut})"/>
        public static async Task<Result<TOut>> Map<TIn, TOut>(
            this Task<Result<TIn>> resultTask,
            Func<TIn, TOut> mapper) =>
            (await resultTask).Map(mapper);

        /// <summary>Turns a successful result into a failure when the value fails a check.</summary>
        public static Result<TValue> Ensure<TValue>(
            this Result<TValue> result,
            Func<TValue, bool> predicate,
            Error error) =>
            result.IsFailure || predicate(result.Value)
                ? result
                : Result.Failure<TValue>(error);

        /// <summary>Chains another result-returning step onto a successful result.</summary>
        public static async Task<Result<TOut>> Bind<TIn, TOut>(
            this Result<TIn> result,
            Func<TIn, Task<Result<TOut>>> next) =>
            result.IsSuccess
                ? await next(result.Value)
                : Result.Failure<TOut>(result.Error);
    }
}
