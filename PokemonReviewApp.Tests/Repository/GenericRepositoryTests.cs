using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Models;
using PokemonReviewApp.Repository;
using PokemonReviewApp.Tests.TestSupport;
using Xunit;

namespace PokemonReviewApp.Tests.Repository
{
    /// <summary>
    /// Exercises <see cref="GenericRepository{TEntity}"/> through a type that has no
    /// hand-written repository, so what is under test is only the shared behaviour.
    /// </summary>
    public class GenericRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_ReturnsTheMatchingEntity()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            dbContext.Categories.Add(new Category { Name = "Water" });
            await dbContext.SaveChangesAsync();

            var id = await dbContext.Categories.Select(c => c.Id).FirstAsync();

            var result = await repository.GetByIdAsync(id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Water");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNullForAnUnknownId()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            var result = await repository.GetByIdAsync(4242);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_AnswersForBothIdAndPredicate()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            dbContext.Categories.Add(new Category { Name = "Fire" });
            await dbContext.SaveChangesAsync();

            var id = await dbContext.Categories.Select(c => c.Id).FirstAsync();

            (await repository.ExistsAsync(id)).Should().BeTrue();
            (await repository.ExistsAsync(id + 1000)).Should().BeFalse();
            (await repository.ExistsAsync(c => c.Name == "Fire")).Should().BeTrue();
            (await repository.ExistsAsync(c => c.Name == "Ghost")).Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_DoesNotPersistUntilSaveChanges()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            await repository.AddAsync(new Category { Name = "Poison" });

            (await dbContext.Categories.CountAsync()).Should().Be(0);

            await dbContext.SaveChangesAsync();

            (await dbContext.Categories.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task RemoveRange_DeletesEveryEntityInOneSave()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            dbContext.Categories.AddRange(
                new Category { Name = "Grass" },
                new Category { Name = "Rock" },
                new Category { Name = "Ice" });
            await dbContext.SaveChangesAsync();

            // Tracked, because these rows are about to be deleted: a detached copy would
            // collide with the instance the context is already tracking.
            var all = await repository.GetAllAsync(tracked: true);
            repository.RemoveRange(all);
            await dbContext.SaveChangesAsync();

            (await dbContext.Categories.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task FindAsync_AppliesThePredicate()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new GenericRepository<Category>(dbContext);

            dbContext.Categories.AddRange(
                new Category { Name = "Bug" },
                new Category { Name = "Bugbear" },
                new Category { Name = "Dragon" });
            await dbContext.SaveChangesAsync();

            var result = await repository.FindAsync(c => c.Name.StartsWith("Bug"));

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CountAsync_CountsEveryRow()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new GenericRepository<Pokemon>(dbContext);

            (await repository.CountAsync()).Should().Be(10);
        }
    }
}
