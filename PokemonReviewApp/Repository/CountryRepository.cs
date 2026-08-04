using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class CountryRepository : GenericRepository<Country>, ICountryRepository
    {
        public CountryRepository(DataContext context) : base(context)
        {
        }

        public Task<Country?> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default) =>
            Context.Owners
                .AsNoTracking()
                .Where(o => o.Id == ownerId)
                .Select(o => o.Country)
                .FirstOrDefaultAsync(cancellationToken);

        public Task<Country?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.Trim().ToUpper() == normalized, cancellationToken);
        }
    }
}
