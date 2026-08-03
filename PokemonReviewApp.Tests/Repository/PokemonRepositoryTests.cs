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
    public class PokemonRepositoryTests
    {
        [Fact]
        public async Task GetByNameAsync_ReturnsPokemon()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new PokemonRepository(dbContext);

            var result = await repository.GetByNameAsync("Pikachu");

            result.Should().NotBeNull();
            result.Should().BeOfType<Pokemon>();
        }

        [Fact]
        public async Task GetByNameAsync_IgnoresCaseAndSurroundingWhitespace()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new PokemonRepository(dbContext);

            var result = await repository.GetByNameAsync("  pIkAcHu  ");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Pikachu");
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNullWhenNothingMatches()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new PokemonRepository(dbContext);

            var result = await repository.GetByNameAsync("Snorlax");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetRatingAsync_ReturnsAverageOfReviewRatings()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new PokemonRepository(dbContext);
            var pokemonId = await dbContext.Pokemon.Select(p => p.Id).FirstAsync();

            var result = await repository.GetRatingAsync(pokemonId);

            // Ratings are 5, 5 and 2 — the decimal cast is what keeps this off an integer 4.
            result.Should().Be(4m);
        }

        [Fact]
        public async Task GetRatingAsync_ReturnsZeroWhenThereAreNoReviews()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new PokemonRepository(dbContext);

            dbContext.Pokemon.Add(new Pokemon { Name = "Ditto" });
            await dbContext.SaveChangesAsync();

            var pokemonId = await dbContext.Pokemon.Select(p => p.Id).FirstAsync();

            var result = await repository.GetRatingAsync(pokemonId);

            result.Should().Be(0m);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEveryPokemonOrderedById()
        {
            var dbContext = await TestDatabase.CreateAsync();
            var repository = new PokemonRepository(dbContext);

            var result = await repository.GetAllAsync();

            result.Should().HaveCount(10);
            result.Select(p => p.Id).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task AddWithOwnerAndCategoryAsync_StagesPokemonAndBothJoinRows()
        {
            var dbContext = await TestDatabase.CreateAsync(seed: false);
            var repository = new PokemonRepository(dbContext);

            var owner = new Owner
            {
                FirstName = "Ash",
                LastName = "Ketchum",
                Gym = "Pallet",
                Country = new Country { Name = "Kanto" }
            };
            var category = new Category { Name = "Electric" };

            dbContext.Owners.Add(owner);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            var pokemon = new Pokemon { Name = "Pikachu" };

            await repository.AddWithOwnerAndCategoryAsync(pokemon, owner.Id, category.Id);

            // Nothing is persisted until the caller saves — that is the unit of work's job.
            (await dbContext.Pokemon.CountAsync()).Should().Be(0);

            await dbContext.SaveChangesAsync();

            (await dbContext.Pokemon.CountAsync()).Should().Be(1);
            (await dbContext.PokemonOwners.CountAsync()).Should().Be(1);
            (await dbContext.PokemonCategories.CountAsync()).Should().Be(1);
        }
    }
}
