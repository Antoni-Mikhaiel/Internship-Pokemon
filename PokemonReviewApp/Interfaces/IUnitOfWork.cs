namespace PokemonReviewApp.Interfaces
{
    /// <summary>
    /// One <see cref="Data.DataContext"/>, one change tracker, one commit. Controllers take
    /// this instead of six separate repositories, so a request that spans entities — deleting
    /// a pokemon along with its reviews — either lands completely or not at all.
    /// </summary>
    /// <remarks>
    /// Deliberately not <see cref="IDisposable"/>: the DI container created the
    /// <see cref="Data.DataContext"/> and disposes it at the end of the request scope.
    /// Disposing it from here too would give the context two owners.
    /// </remarks>
    public interface IUnitOfWork
    {
        ICategoryRepository Categories { get; }

        ICountryRepository Countries { get; }

        IOwnerRepository Owners { get; }

        IPokemonRepository Pokemon { get; }

        IReviewRepository Reviews { get; }

        IReviewerRepository Reviewers { get; }

        /// <summary>
        /// A repository for an entity that has no hand-written one yet. Cached per instance,
        /// so repeated calls share the same change tracker.
        /// </summary>
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;

        /// <summary>Writes everything staged so far and returns the number of rows affected.</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
