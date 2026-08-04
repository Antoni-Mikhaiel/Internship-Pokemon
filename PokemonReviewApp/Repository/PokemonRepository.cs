using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class PokemonRepository : GenericRepository<Pokemon>, IPokemonRepository
    {
        public PokemonRepository(DataContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Pokemon>> GetAllAsync(
            bool tracked = false,
            CancellationToken cancellationToken = default) =>
            await Query(tracked).OrderBy(p => p.Id).ToListAsync(cancellationToken);

        public Task<Pokemon?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.Trim().ToUpper() == normalized, cancellationToken);
        }

        public async Task<decimal> GetRatingAsync(int pokemonId, CancellationToken cancellationToken = default)
        {
            var ratings = Context.Reviews
                .AsNoTracking()
                .Where(r => r.Pokemon.Id == pokemonId)
                .Select(r => r.Rating);

            // Sum and count rather than AverageAsync: SQLite refuses to average a decimal,
            // and averaging as a double would round the result before it ever became one.
            // The reviews themselves still never leave the database.
            var count = await ratings.CountAsync(cancellationToken);

            if (count == 0)
                return 0m;

            var total = await ratings.SumAsync(cancellationToken);

            // The decimal cast is what keeps a 5, 5, 2 spread at 4 rather than integer 4.
            return (decimal)total / count;
        }

        public async Task AddWithOwnerAndCategoryAsync(
            Pokemon pokemon,
            int ownerId,
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            // Reference the owner and category by id rather than by loaded entity: EF fills the
            // foreign keys once the pokemon gets its identity on save, and a bad id surfaces as
            // a constraint violation the exception middleware turns into a 500 rather than a
            // silent row with a null owner.
            await DbSet.AddAsync(pokemon, cancellationToken);

            await Context.PokemonOwners.AddAsync(
                new PokemonOwner { Owner = null!, OwnerId = ownerId, Pokemon = pokemon },
                cancellationToken);

            await Context.PokemonCategories.AddAsync(
                new PokemonCategory { Category = null!, CategoryId = categoryId, Pokemon = pokemon },
                cancellationToken);
        }
    }
}
