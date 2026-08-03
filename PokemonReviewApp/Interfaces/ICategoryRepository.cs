using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<IReadOnlyList<Pokemon>> GetPokemonByCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken = default);

        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
