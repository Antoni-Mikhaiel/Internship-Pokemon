using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface IOwnerRepository : IGenericRepository<Owner>
    {
        Task<IReadOnlyList<Pokemon>> GetPokemonByOwnerAsync(
            int ownerId,
            CancellationToken cancellationToken = default);

        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Owner?> GetByLastNameAsync(string lastName, CancellationToken cancellationToken = default);
    }
}
