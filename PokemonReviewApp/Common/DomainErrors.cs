namespace PokemonReviewApp.Common
{
    /// <summary>
    /// Every failure the application can return, in one place. Keeping the codes here
    /// rather than inline at each throw site means a client can switch on a stable string
    /// and the wording can change without breaking anyone.
    /// </summary>
    public static class DomainErrors
    {
        public static class General
        {
            public static readonly Error EmptyBody =
                Error.Validation("General.EmptyBody", "The request body was empty.");

            public static Error IdMismatch(string routeName) =>
                Error.Validation("General.IdMismatch",
                    $"The id in the route does not match the id in the body ({routeName}).");

            public static readonly Error SaveFailed =
                Error.Failure("General.SaveFailed", "The changes could not be saved.");
        }

        public static class Category
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Category.NotFound", $"No category with id {id} exists.");

            public static Error DuplicateName(string name) =>
                Error.Conflict("Category.DuplicateName", $"A category named '{name}' already exists.");
        }

        public static class Country
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Country.NotFound", $"No country with id {id} exists.");

            public static Error DuplicateName(string name) =>
                Error.Conflict("Country.DuplicateName", $"A country named '{name}' already exists.");
        }

        public static class Owner
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Owner.NotFound", $"No owner with id {id} exists.");

            public static Error DuplicateName(string name) =>
                Error.Conflict("Owner.DuplicateName", $"An owner named '{name}' already exists.");
        }

        public static class Pokemon
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Pokemon.NotFound", $"No pokemon with id {id} exists.");

            public static Error DuplicateName(string name) =>
                Error.Conflict("Pokemon.DuplicateName", $"A pokemon named '{name}' already exists.");
        }

        public static class Review
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Review.NotFound", $"No review with id {id} exists.");

            public static Error DuplicateTitle(string title) =>
                Error.Conflict("Review.DuplicateTitle", $"A review titled '{title}' already exists.");
        }

        public static class Reviewer
        {
            public static Error NotFound(int id) =>
                Error.NotFound("Reviewer.NotFound", $"No reviewer with id {id} exists.");

            public static Error DuplicateName(string lastName) =>
                Error.Conflict("Reviewer.DuplicateName", $"A reviewer named '{lastName}' already exists.");
        }

        public static class Users
        {
            public static readonly Error InvalidCredentials =
                Error.Unauthorized("Users.InvalidCredentials", "The username or password is incorrect.");

            public static readonly Error LockedOut =
                Error.Forbidden("Users.LockedOut", "The account is locked out. Try again later.");

            public static Error DuplicateUserName(string userName) =>
                Error.Conflict("Users.DuplicateUserName", $"The username '{userName}' is already taken.");

            public static Error DuplicateEmail(string email) =>
                Error.Conflict("Users.DuplicateEmail", $"The email '{email}' is already registered.");

            public static readonly Error NotFound =
                Error.NotFound("Users.NotFound", "No user matches the supplied credentials.");
        }
    }
}
