using System.Linq.Expressions;

namespace PokemonReviewApp.Interfaces
{
    /// <summary>
    /// The CRUD half of every repository, written once. Entity-specific interfaces inherit
    /// from it and add only the queries that are actually specific to that entity.
    /// </summary>
    /// <remarks>
    /// Nothing here writes to the database. Persisting is <see cref="IUnitOfWork.SaveChangesAsync"/>'s
    /// job, so a request that touches several repositories still commits as one transaction.
    /// </remarks>
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <param name="tracked">
        /// Reads are untracked by default, which is faster and cannot accidentally write.
        /// Pass <c>true</c> when the results are about to be updated or deleted: handing a
        /// detached copy to <see cref="Remove"/> throws if the context already tracks that row.
        /// </param>
        Task<IReadOnlyList<TEntity>> GetAllAsync(
            bool tracked = false,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="GetAllAsync"/>
        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool tracked = false,
            CancellationToken cancellationToken = default);

        Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        void Update(TEntity entity);

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// An untracked queryable for read paths that need shaping the interface does not
        /// cover (projections, includes, paging). Kept read-only on purpose: composing a
        /// query is fine, writing through it is not.
        /// </summary>
        IQueryable<TEntity> AsQueryable();
    }
}
