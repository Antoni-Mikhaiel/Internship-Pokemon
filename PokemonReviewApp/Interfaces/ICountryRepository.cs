using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface ICountryRepository : IGenericRepository<Country>
    {
        Task<Country?> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default);

        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Country?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
