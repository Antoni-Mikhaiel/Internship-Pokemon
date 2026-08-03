using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Repository
{
    public class ReviewerRepository : GenericRepository<Reviewer>, IReviewerRepository
    {
        public ReviewerRepository(DataContext context) : base(context)
        {
        }

        public Task<Reviewer?> GetWithReviewsAsync(int reviewerId, CancellationToken cancellationToken = default) =>
            DbSet
                .AsNoTracking()
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.Id == reviewerId, cancellationToken);

        public async Task<IReadOnlyList<Review>> GetReviewsByReviewerAsync(
            int reviewerId,
            bool tracked = false,
            CancellationToken cancellationToken = default)
        {
            var reviews = tracked ? Context.Reviews : Context.Reviews.AsNoTracking();

            return await reviews
                .Where(r => r.Reviewer.Id == reviewerId)
                .ToListAsync(cancellationToken);
        }

        public Task<Reviewer?> GetByLastNameAsync(string lastName, CancellationToken cancellationToken = default)
        {
            var normalized = lastName.Trim().ToUpper();

            return DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.LastName.Trim().ToUpper() == normalized, cancellationToken);
        }
    }
}
