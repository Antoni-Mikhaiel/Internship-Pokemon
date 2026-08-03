using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(DataContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Review>> GetByPokemonAsync(
            int pokemonId,
            bool tracked = false,
            CancellationToken cancellationToken = default) =>
            await Query(tracked)
                .Where(r => r.Pokemon.Id == pokemonId)
                .ToListAsync(cancellationToken);

        public Task<Review?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            var normalized = title.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Title.Trim().ToUpper() == normalized, cancellationToken);
        }
    }
}
