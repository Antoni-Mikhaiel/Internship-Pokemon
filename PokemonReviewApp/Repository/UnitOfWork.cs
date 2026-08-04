using PokemonReviewApp.Data;
using PokemonReviewApp.Interfaces;

namespace PokemonReviewApp.Repository
{
    /// <summary>
    /// Hands out repositories that all share this instance's <see cref="DataContext"/>, and
    /// owns the single call that commits their staged changes.
    /// </summary>
    /// <remarks>
    /// Repositories are created on first use rather than in the constructor: most requests
    /// touch one or two, and there is no reason to build six.
    /// </remarks>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        private ICategoryRepository? _categories;
        private ICountryRepository? _countries;
        private IOwnerRepository? _owners;
        private IPokemonRepository? _pokemon;
        private IReviewRepository? _reviews;
        private IReviewerRepository? _reviewers;

        public UnitOfWork(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);

        public ICountryRepository Countries => _countries ??= new CountryRepository(_context);

        public IOwnerRepository Owners => _owners ??= new OwnerRepository(_context);

        public IPokemonRepository Pokemon => _pokemon ??= new PokemonRepository(_context);

        public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);

        public IReviewerRepository Reviewers => _reviewers ??= new ReviewerRepository(_context);

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var existing))
                return (IGenericRepository<TEntity>)existing;

            var repository = new GenericRepository<TEntity>(_context);
            _repositories[typeof(TEntity)] = repository;

            return repository;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}
