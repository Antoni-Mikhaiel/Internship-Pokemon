using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class OwnerRepository : GenericRepository<Owner>, IOwnerRepository
    {
        public OwnerRepository(DataContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Pokemon>> GetPokemonByOwnerAsync(
            int ownerId,
            CancellationToken cancellationToken = default) =>
            await Context.PokemonOwners
                .AsNoTracking()
                .Where(po => po.OwnerId == ownerId)
                .Select(po => po.Pokemon)
                .ToListAsync(cancellationToken);

        public Task<Owner?> GetByLastNameAsync(string lastName, CancellationToken cancellationToken = default)
        {
            var normalized = lastName.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.LastName.Trim().ToUpper() == normalized, cancellationToken);
        }
    }
}
