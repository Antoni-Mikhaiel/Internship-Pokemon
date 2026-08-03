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
    public class UnitOfWorkTests
    {
        [Fact]
        public async Task Repositories_AreCachedPerInstance()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var unitOfWork = new UnitOfWork(dbContext);

            unitOfWork.Categories.Should().BeSameAs(unitOfWork.Categories);
            unitOfWork.Repository<Category>().Should().BeSameAs(unitOfWork.Repository<Category>());
        }

        [Fact]
        public async Task SaveChangesAsync_CommitsWorkStagedAcrossSeveralRepositories()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var unitOfWork = new UnitOfWork(dbContext);

            await unitOfWork.Categories.AddAsync(new Category { Name = "Electric" });
            await unitOfWork.Countries.AddAsync(new Country { Name = "Kanto" });
            await unitOfWork.Pokemon.AddAsync(new Pokemon { Name = "Pikachu" });

            // Nothing has touched the database yet: three repositories, one pending commit.
            (await dbContext.Categories.CountAsync()).Should().Be(0);
            (await dbContext.Countries.CountAsync()).Should().Be(0);
            (await dbContext.Pokemon.CountAsync()).Should().Be(0);

            var affected = await unitOfWork.SaveChangesAsync();

            affected.Should().Be(3);
            (await dbContext.Categories.CountAsync()).Should().Be(1);
            (await dbContext.Countries.CountAsync()).Should().Be(1);
            (await dbContext.Pokemon.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task DeletingAPokemonAndItsReviewsIsASingleCommit()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var unitOfWork = new UnitOfWork(dbContext);

            var pokemonId = await dbContext.Pokemon.Select(p => p.Id).FirstAsync();
            var pokemon = await unitOfWork.Pokemon.GetByIdAsync(pokemonId);
            var reviews = await unitOfWork.Reviews.GetByPokemonAsync(pokemonId, tracked: true);

            reviews.Should().HaveCount(3);

            unitOfWork.Reviews.RemoveRange(reviews);
            unitOfWork.Pokemon.Remove(pokemon!);

            await unitOfWork.SaveChangesAsync();

            (await dbContext.Pokemon.AnyAsync(p => p.Id == pokemonId)).Should().BeFalse();
            (await dbContext.Reviews.AnyAsync(r => r.Pokemon.Id == pokemonId)).Should().BeFalse();
        }
    }
}
