using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;

namespace PokemonReviewApp.Repository
{
    /// <summary>
    /// EF Core implementation of <see cref="IGenericRepository{TEntity}"/>.
    /// </summary>
    /// <remarks>
    /// Every method stops at the change tracker; none of them call SaveChanges. That is what
    /// lets <see cref="UnitOfWork"/> commit a whole request — deleting a pokemon and its
    /// reviews, say — in one transaction instead of two half-applied ones.
    /// </remarks>
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly DataContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public GenericRepository(DataContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await DbSet.FindAsync(new object?[] { id }, cancellationToken);

        public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
            bool tracked = false,
            CancellationToken cancellationToken = default) =>
            await Query(tracked).ToListAsync(cancellationToken);

        public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool tracked = false,
            CancellationToken cancellationToken = default) =>
            await Query(tracked).Where(predicate).ToListAsync(cancellationToken);

        /// <summary>
        /// The starting point for every read in this class and its subclasses: untracked for
        /// plain queries, tracked when the caller intends to mutate what comes back.
        /// </summary>
        protected IQueryable<TEntity> Query(bool tracked) =>
            tracked ? DbSet : DbSet.AsNoTracking();

        public virtual async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

        // EF.Property keeps this usable for any entity with an int "Id" without forcing a
        // shared base class on the models, and it answers from the database rather than
        // pulling the row into the change tracker the way Find would.
        public virtual Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            DbSet.AsNoTracking().AnyAsync(entity => EF.Property<int>(entity, "Id") == id, cancellationToken);

        public virtual Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            DbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);

        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default) =>
            DbSet.AsNoTracking().CountAsync(cancellationToken);

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            await DbSet.AddAsync(entity, cancellationToken);

        public virtual async Task AddRangeAsync(
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) =>
            await DbSet.AddRangeAsync(entities, cancellationToken);

        public virtual void Update(TEntity entity) => DbSet.Update(entity);

        public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

        public virtual void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);

        public virtual IQueryable<TEntity> AsQueryable() => DbSet.AsNoTracking();
    }
}
