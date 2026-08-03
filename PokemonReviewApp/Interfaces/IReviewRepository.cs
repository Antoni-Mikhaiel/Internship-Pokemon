using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        /// <inheritdoc cref="IGenericRepository{TEntity}.GetAllAsync"/>
        Task<IReadOnlyList<Review>> GetByPokemonAsync(
            int pokemonId,
            bool tracked = false,
            CancellationToken cancellationToken = default);

        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Review?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
    }
}
