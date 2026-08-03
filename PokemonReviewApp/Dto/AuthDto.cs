using System.ComponentModel.DataAnnotations;

namespace PokemonReviewApp.Dto
{
    public class RegisterDto
    {
        [Required]
        [StringLength(64, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Length only. Complexity is Identity's call, and duplicating those rules here
        // would let the two drift apart.
        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>What a successful register or login hands back to the caller.</summary>
    public class AuthResponseDto
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
