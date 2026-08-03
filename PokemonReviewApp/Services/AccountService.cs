using Microsoft.AspNetCore.Identity;
using PokemonReviewApp.Common;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Services
{
    /// <summary>
    /// Wraps Identity's managers behind the Result pattern, so controllers deal in
    /// <see cref="Result{TValue}"/> rather than in <see cref="IdentityResult"/> and
    /// <see cref="SignInResult"/>.
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<AuthResponseDto>> RegisterAsync(
            RegisterDto request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = new AppUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
                return Result.Failure<AuthResponseDto>(ToError(createResult, request));

            // Everyone starts as a plain User; Admin is granted out of band. Failing to
            // attach the role would leave an account that authenticates but authorizes
            // nowhere, so it is worth a log line even though registration itself succeeded.
            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);

            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "User {UserName} was created but could not be added to the {Role} role: {Errors}",
                    user.UserName, AppRoles.User, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

                return Result.Failure<AuthResponseDto>(DomainErrors.General.SaveFailed);
            }

            _logger.LogInformation("Registered new user {UserName}.", user.UserName);

            return await BuildAuthResponseAsync(user);
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(
            LoginDto request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByNameAsync(request.UserName);

            // An unknown username and a wrong password return the same error on purpose:
            // distinguishing them would let anyone enumerate who has an account.
            if (user is null)
                return Result.Failure<AuthResponseDto>(DomainErrors.Users.InvalidCredentials);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("Login refused for locked-out user {UserName}.", request.UserName);
                return Result.Failure<AuthResponseDto>(DomainErrors.Users.LockedOut);
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for {UserName}.", request.UserName);
                return Result.Failure<AuthResponseDto>(DomainErrors.Users.InvalidCredentials);
            }

            return await BuildAuthResponseAsync(user);
        }

        private async Task<Result<AuthResponseDto>> BuildAuthResponseAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = _tokenService.CreateToken(user, roles);

            return Result.Success(new AuthResponseDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                Roles = roles.ToList()
            });
        }

        /// <summary>
        /// Translates Identity's error codes into domain errors. A taken username is a
        /// conflict; everything else — password complexity, invalid characters — is a
        /// validation failure, and Identity reports every broken rule at once, so they all
        /// travel back together.
        /// </summary>
        private static Error ToError(IdentityResult identityResult, RegisterDto request)
        {
            var errors = identityResult.Errors.ToList();

            if (errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                return DomainErrors.Users.DuplicateUserName(request.UserName);

            if (errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
                return DomainErrors.Users.DuplicateEmail(request.Email);

            return new ValidationError(
                errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description)).ToList());
        }
    }
}
