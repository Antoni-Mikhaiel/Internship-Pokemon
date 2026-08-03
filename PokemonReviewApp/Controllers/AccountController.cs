using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;

namespace PokemonReviewApp.Controllers
{
    /// <summary>
    /// Registration and sign-in. The only controller reachable without a bearer token —
    /// it is where callers get one.
    /// </summary>
    [AllowAnonymous]
    public class AccountController : ApiControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto request,
            CancellationToken cancellationToken)
        {
            var result = await _accountService.RegisterAsync(request, cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto request,
            CancellationToken cancellationToken)
        {
            var result = await _accountService.LoginAsync(request, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// Echoes back who the bearer token says you are — the quickest way to confirm the
        /// token, the signing key and the role claims all line up.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me() => Ok(new
        {
            UserName = User.Identity?.Name,
            Email = User.FindFirstValue(ClaimTypes.Email),
            Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
        });
    }
}
