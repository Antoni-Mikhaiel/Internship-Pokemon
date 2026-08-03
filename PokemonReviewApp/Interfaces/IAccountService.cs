using PokemonReviewApp.Common;
using PokemonReviewApp.Dto;

namespace PokemonReviewApp.Interfaces
{
    /// <summary>
    /// Registration and sign-in. Both return a <see cref="Result{TValue}"/>: a rejected
    /// password or a taken username is an expected answer, not an exception.
    /// </summary>
    public interface IAccountService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(
            RegisterDto request,
            CancellationToken cancellationToken = default);

        Task<Result<AuthResponseDto>> LoginAsync(
            LoginDto request,
            CancellationToken cancellationToken = default);
    }
}
