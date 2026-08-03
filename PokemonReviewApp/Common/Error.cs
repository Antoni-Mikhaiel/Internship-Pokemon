namespace PokemonReviewApp.Common
{
    /// <summary>
    /// The kind of failure an <see cref="Error"/> describes. The API layer translates
    /// this into an HTTP status code, so the domain never has to know about HTTP.
    /// </summary>
    public enum ErrorType
    {
        Failure = 0,
        Validation = 1,
        NotFound = 2,
        Conflict = 3,
        Unauthorized = 4,
        Forbidden = 5
    }

    /// <summary>
    /// A single, self-describing failure: a machine-readable <see cref="Code"/>, a
    /// human-readable <see cref="Description"/>, and a <see cref="Type"/> that says how
    /// severe it is. Returned inside a <see cref="Result"/> instead of being thrown.
    /// </summary>
    public record Error(string Code, string Description, ErrorType Type)
    {
        /// <summary>Placeholder carried by every successful result.</summary>
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

        /// <summary>A value that was required turned out to be null.</summary>
        public static readonly Error NullValue =
            new("General.NullValue", "A required value was null.", ErrorType.Validation);

        public static Error Failure(string code, string description) =>
            new(code, description, ErrorType.Failure);

        public static Error Validation(string code, string description) =>
            new(code, description, ErrorType.Validation);

        public static Error NotFound(string code, string description) =>
            new(code, description, ErrorType.NotFound);

        public static Error Conflict(string code, string description) =>
            new(code, description, ErrorType.Conflict);

        public static Error Unauthorized(string code, string description) =>
            new(code, description, ErrorType.Unauthorized);

        public static Error Forbidden(string code, string description) =>
            new(code, description, ErrorType.Forbidden);
    }

    /// <summary>
    /// Several validation failures reported together — Identity, for instance, rejects a
    /// password for every rule it breaks at once, and the caller deserves the whole list.
    /// </summary>
    public sealed record ValidationError : Error
    {
        public ValidationError(IReadOnlyCollection<Error> errors)
            : base("General.Validation", "One or more validation errors occurred.", ErrorType.Validation)
        {
            Errors = errors;
        }

        public IReadOnlyCollection<Error> Errors { get; }
    }
}
