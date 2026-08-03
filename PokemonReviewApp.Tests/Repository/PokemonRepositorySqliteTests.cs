using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Models;
using PokemonReviewApp.Repository;
using Xunit;

namespace PokemonReviewApp.Tests.Repository
{
    /// <summary>
    /// The same repository, exercised against real SQLite rather than the in-memory
    /// provider. The in-memory provider evaluates LINQ in .NET and happily averages a
    /// decimal; SQLite refuses to, so a query that passes there can still fail in
    /// production. Aggregates get checked against the provider the app actually ships with.
    /// </summary>
    public class PokemonRepositorySqliteTests : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private DataContext _context = null!;

        public async Task InitializeAsync()
        {
            // A shared in-memory SQLite database lives exactly as long as this connection.
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new DataContext(options);
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task<int> SeedPokemonWithRatingsAsync(params int[] ratings)
        {
            var pokemon = new Pokemon
            {
                Name = "Pikachu",
                BirthDate = new DateTime(1996, 2, 27),
                Reviews = ratings.Select((rating, index) => new Review
                {
                    Title = $"Review {index}",
                    Text = "Text",
                    Rating = rating,
                    Reviewer = new Reviewer { FirstName = "Teddy", LastName = $"Smith {index}" }
                }).ToList()
            };

            _context.Pokemon.Add(pokemon);
            await _context.SaveChangesAsync();

            return pokemon.Id;
        }

        [Fact]
        public async Task GetRatingAsync_AveragesOnSqliteWithoutFallingOverOnDecimals()
        {
            var pokemonId = await SeedPokemonWithRatingsAsync(5, 5, 2);
            var repository = new PokemonRepository(_context);

            var result = await repository.GetRatingAsync(pokemonId);

            result.Should().Be(4m);
        }

        [Fact]
        public async Task GetRatingAsync_KeepsTheFractionalPart()
        {
            var pokemonId = await SeedPokemonWithRatingsAsync(4, 5);
            var repository = new PokemonRepository(_context);

            var result = await repository.GetRatingAsync(pokemonId);

            // Integer division would report 4 here.
            result.Should().Be(4.5m);
        }

        [Fact]
        public async Task GetRatingAsync_ReturnsZeroWhenThereAreNoReviews()
        {
            var pokemonId = await SeedPokemonWithRatingsAsync();
            var repository = new PokemonRepository(_context);

            var result = await repository.GetRatingAsync(pokemonId);

            result.Should().Be(0m);
        }

        [Fact]
        public async Task GetByNameAsync_TranslatesTrimAndUpperToSql()
        {
            await SeedPokemonWithRatingsAsync(5);
            var repository = new PokemonRepository(_context);

            var result = await repository.GetByNameAsync("  pIkAcHu ");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Pikachu");
        }

        [Fact]
        public async Task ExistsAsync_TranslatesTheEfPropertyKeyLookupToSql()
        {
            var pokemonId = await SeedPokemonWithRatingsAsync(5);
            var repository = new PokemonRepository(_context);

            (await repository.ExistsAsync(pokemonId)).Should().BeTrue();
            (await repository.ExistsAsync(pokemonId + 500)).Should().BeFalse();
        }

        [Fact]
        public async Task AddWithOwnerAndCategoryAsync_SatisfiesTheRealForeignKeys()
        {
            var repository = new PokemonRepository(_context);

            var owner = new Owner
            {
                FirstName = "Ash",
                LastName = "Ketchum",
                Gym = "Pallet",
                Country = new Country { Name = "Kanto" }
            };
            var category = new Category { Name = "Electric" };

            _context.Owners.Add(owner);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var pokemon = new Pokemon { Name = "Pikachu", BirthDate = new DateTime(1996, 2, 27) };

            await repository.AddWithOwnerAndCategoryAsync(pokemon, owner.Id, category.Id);
            await _context.SaveChangesAsync();

            var owned = await _context.PokemonOwners.SingleAsync();
            owned.PokemonId.Should().Be(pokemon.Id);
            owned.OwnerId.Should().Be(owner.Id);

            var categorised = await _context.PokemonCategories.SingleAsync();
            categorised.PokemonId.Should().Be(pokemon.Id);
            categorised.CategoryId.Should().Be(category.Id);
        }
    }
}
