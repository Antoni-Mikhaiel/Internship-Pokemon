using System.ComponentModel.DataAnnotations;

namespace PokemonReviewApp.Common
{
    /// <summary>
    /// Bearer-token settings bound from the <c>Jwt</c> configuration section and validated
    /// at startup, so a missing signing key stops the app immediately instead of producing
    /// tokens nobody can verify.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// HMAC-SHA256 signing key. Must be at least 32 characters (256 bits) or the
        /// handler refuses to sign. Supply it through user-secrets or an environment
        /// variable outside development — never a checked-in appsettings.json.
        /// </summary>
        [Required]
        [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters for HMAC-SHA256.")]
        public string Key { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpiryMinutes { get; set; } = 60;
    }
}
