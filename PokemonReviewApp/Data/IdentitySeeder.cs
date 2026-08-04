using Microsoft.AspNetCore.Identity;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Data
{
    /// <summary>
    /// Creates the roles the API's <c>[Authorize(Roles = ...)]</c> attributes rely on, and
    /// optionally a first administrator. Runs at startup and is safe to run repeatedly.
    /// </summary>
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(IdentitySeeder));

            foreach (var role in AppRoles.All)
            {
                if (await roleManager.RoleExistsAsync(role))
                    continue;

                var result = await roleManager.CreateAsync(new IdentityRole(role));

                if (result.Succeeded)
                    logger.LogInformation("Created role {Role}.", role);
                else
                    logger.LogError("Could not create role {Role}: {Errors}",
                        role, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await SeedAdministratorAsync(userManager, configuration, logger);
        }

        private static async Task SeedAdministratorAsync(
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            var section = configuration.GetSection("Identity:AdminUser");
            var userName = section["UserName"];
            var email = section["Email"];
            var password = section["Password"];

            // No credentials configured means no seeded admin. Inventing a default password
            // here would be worse than having no admin at all, so the app simply says so.
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation(
                    "No Identity:AdminUser configured; skipping administrator seeding. " +
                    "Set Identity:AdminUser:UserName and :Password (user-secrets or environment) to create one.");
                return;
            }

            if (await userManager.FindByNameAsync(userName) is not null)
                return;

            var admin = new AppUser
            {
                UserName = userName,
                Email = email ?? $"{userName}@localhost",
                EmailConfirmed = true,
                FirstName = "Site",
                LastName = "Administrator"
            };

            var created = await userManager.CreateAsync(admin, password);

            if (!created.Succeeded)
            {
                logger.LogError("Could not seed administrator {UserName}: {Errors}",
                    userName, string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRolesAsync(admin, AppRoles.All);

            logger.LogInformation("Seeded administrator {UserName}.", userName);
        }
    }
}
