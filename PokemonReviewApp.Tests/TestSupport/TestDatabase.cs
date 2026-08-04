using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Tests.TestSupport
{
    /// <summary>
    /// Builds a throwaway in-memory <see cref="DataContext"/>. Each call gets its own
    /// database name so tests cannot see each other's rows however they are ordered.
    /// </summary>
    internal static class TestDatabase
    {
        internal static async Task<DataContext> CreateAsync(bool seed = true)
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);
            context.Database.EnsureCreated();

            if (seed)
                await SeedAsync(context);

            return context;
        }

        private static async Task SeedAsync(DataContext context)
        {
            for (var i = 1; i <= 10; i++)
            {
                context.Pokemon.Add(new Pokemon
                {
                    Name = i == 1 ? "Pikachu" : $"Pikachu {i}",
                    BirthDate = new DateTime(1903, 1, 1),
                    PokemonCategories = new List<PokemonCategory>
                    {
                        new PokemonCategory { Category = new Category { Name = "Electric" } }
                    },
                    Reviews = new List<Review>
                    {
                        new Review
                        {
                            Title = $"Pikachu {i} A",
                            Text = "Pikachu is the best pokemon, because it is electric",
                            Rating = 5,
                            Reviewer = new Reviewer { FirstName = "Teddy", LastName = "Smith" }
                        },
                        new Review
                        {
                            Title = $"Pikachu {i} B",
                            Text = "Pikachu is the best at killing rocks",
                            Rating = 5,
                            Reviewer = new Reviewer { FirstName = "Taylor", LastName = "Jones" }
                        },
                        new Review
                        {
                            Title = $"Pikachu {i} C",
                            Text = "Pikachu, pikachu, pikachu",
                            Rating = 2,
                            Reviewer = new Reviewer { FirstName = "Jessica", LastName = "McGregor" }
                        }
                    }
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
