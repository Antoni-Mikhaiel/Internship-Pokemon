using Microsoft.AspNetCore.Identity;

namespace PokemonReviewApp.Models
{
    /// <summary>
    /// The application's user. Derives from <see cref="IdentityUser"/> so Identity keeps
    /// owning credentials, lockout and security stamps, and only the fields this app
    /// actually needs are added on top.
    /// </summary>
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// The roles the API ships with. Constants rather than loose strings so a typo in an
    /// <c>[Authorize(Roles = ...)]</c> attribute fails the build instead of silently
    /// locking everybody out.
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static IReadOnlyList<string> All { get; } = new[] { Admin, User };
    }
}
