using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface IPokemonRepository : IGenericRepository<Pokemon>
    {
        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Pokemon?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Mean of every review's rating, or 0 when the pokemon has no reviews yet.</summary>
        Task<decimal> GetRatingAsync(int pokemonId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages the pokemon together with its owner and category join rows.
        /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to commit all three at once.
        /// </summary>
        Task AddWithOwnerAndCategoryAsync(
            Pokemon pokemon,
            int ownerId,
            int categoryId,
            CancellationToken cancellationToken = default);
    }
}
