using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(DataContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Pokemon>> GetPokemonByCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken = default) =>
            await Context.PokemonCategories
                .AsNoTracking()
                .Where(pc => pc.CategoryId == categoryId)
                .Select(pc => pc.Pokemon)
                .ToListAsync(cancellationToken);

        public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.Trim().ToUpper() == normalized, cancellationToken);
        }
    }
}
