using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Issues a signed bearer token for the user, carrying the supplied roles as claims.
        /// </summary>
        (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user, IEnumerable<string> roles);
    }
}
