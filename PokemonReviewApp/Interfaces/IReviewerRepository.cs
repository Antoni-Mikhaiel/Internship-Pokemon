using PokemonReviewApp.Models;

namespace PokemonReviewApp.Interfaces
{
    public interface IReviewerRepository : IGenericRepository<Reviewer>
    {
        /// <summary>The reviewer with their reviews eagerly loaded.</summary>
        Task<Reviewer?> GetWithReviewsAsync(int reviewerId, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="IGenericRepository{TEntity}.GetAllAsync"/>
        Task<IReadOnlyList<Review>> GetReviewsByReviewerAsync(
            int reviewerId,
            bool tracked = false,
            CancellationToken cancellationToken = default);

        /// <summary>Case- and whitespace-insensitive lookup used to reject duplicates.</summary>
        Task<Reviewer?> GetByLastNameAsync(string lastName, CancellationToken cancellationToken = default);
    }
}
