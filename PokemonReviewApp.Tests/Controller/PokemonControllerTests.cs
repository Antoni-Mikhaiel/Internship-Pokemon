using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Controllers;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;
using PokemonReviewApp.Tests.TestSupport;
using Xunit;

namespace PokemonReviewApp.Tests.Controller
{
    public class PokemonControllerTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPokemonRepository _pokemonRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public PokemonControllerTests()
        {
            _pokemonRepository = A.Fake<IPokemonRepository>();
            _reviewRepository = A.Fake<IReviewRepository>();
            _mapper = A.Fake<IMapper>();

            _unitOfWork = A.Fake<IUnitOfWork>();
            A.CallTo(() => _unitOfWork.Pokemon).Returns(_pokemonRepository);
            A.CallTo(() => _unitOfWork.Reviews).Returns(_reviewRepository);
        }

        private PokemonController CreateController()
        {
            // ApiControllerBase renders failures through the problem details factory, which
            // only exists once MVC has built the controller. Supply both by hand.
            return new PokemonController(_unitOfWork, _mapper)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                ProblemDetailsFactory = new TestProblemDetailsFactory()
            };
        }

        [Fact]
        public async Task GetPokemons_ReturnsOk()
        {
            var pokemon = new List<Pokemon> { new Pokemon { Id = 1, Name = "Pikachu" } };
            A.CallTo(() => _pokemonRepository.GetAllAsync(A<bool>._, A<CancellationToken>._)).Returns(pokemon);
            A.CallTo(() => _mapper.Map<List<PokemonDto>>(pokemon))
                .Returns(new List<PokemonDto> { new PokemonDto { Id = 1, Name = "Pikachu" } });

            var result = await CreateController().GetPokemons(CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPokemon_ReturnsOkWhenItExists()
        {
            var pokemon = new Pokemon { Id = 1, Name = "Pikachu" };
            A.CallTo(() => _pokemonRepository.GetByIdAsync(1, A<CancellationToken>._)).Returns(pokemon);
            A.CallTo(() => _mapper.Map<PokemonDto>(pokemon))
                .Returns(new PokemonDto { Id = 1, Name = "Pikachu" });

            var result = await CreateController().GetPokemon(1, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPokemon_ReturnsProblemDetailsWith404WhenMissing()
        {
            A.CallTo(() => _pokemonRepository.GetByIdAsync(99, A<CancellationToken>._))
                .Returns(Task.FromResult<Pokemon?>(null));

            var result = await CreateController().GetPokemon(99, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);

            var problem = objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
            problem.Extensions["errorCode"].Should().Be("Pokemon.NotFound");
        }

        [Fact]
        public async Task CreatePokemon_ReturnsConflictWhenTheNameIsTaken()
        {
            var request = new PokemonDto { Name = "Pikachu" };
            A.CallTo(() => _pokemonRepository.GetByNameAsync("Pikachu", A<CancellationToken>._))
                .Returns(new Pokemon { Id = 1, Name = "Pikachu" });

            var result = await CreateController().CreatePokemon(1, 2, request, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }

        [Fact]
        public async Task CreatePokemon_Returns201AndCommitsOnceWhenTheRequestIsGood()
        {
            var request = new PokemonDto { Id = 0, Name = "Snorlax" };
            var pokemon = new Pokemon { Id = 5, Name = "Snorlax" };

            A.CallTo(() => _pokemonRepository.GetByNameAsync("Snorlax", A<CancellationToken>._))
                .Returns(Task.FromResult<Pokemon?>(null));
            A.CallTo(() => _unitOfWork.Owners).Returns(A.Fake<IOwnerRepository>());
            A.CallTo(() => _unitOfWork.Owners.ExistsAsync(1, A<CancellationToken>._)).Returns(true);
            A.CallTo(() => _unitOfWork.Categories).Returns(A.Fake<ICategoryRepository>());
            A.CallTo(() => _unitOfWork.Categories.ExistsAsync(2, A<CancellationToken>._)).Returns(true);
            A.CallTo(() => _mapper.Map<Pokemon>(request)).Returns(pokemon);
            A.CallTo(() => _mapper.Map<PokemonDto>(pokemon))
                .Returns(new PokemonDto { Id = 5, Name = "Snorlax" });

            var result = await CreateController().CreatePokemon(1, 2, request, CancellationToken.None);

            result.Should().BeOfType<CreatedAtActionResult>();

            // The pokemon and its join rows land in one commit, not three.
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task UpdatePokemon_RejectsAnIdThatDisagreesWithTheBody()
        {
            var request = new PokemonDto { Id = 2, Name = "Pikachu" };

            var result = await CreateController().UpdatePokemon(1, request, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task DeletePokemon_RemovesTheReviewsInTheSameCommit()
        {
            var pokemon = new Pokemon { Id = 1, Name = "Pikachu" };
            var reviews = new List<Review> { new Review { Id = 1, Title = "Great" } };

            A.CallTo(() => _pokemonRepository.GetByIdAsync(1, A<CancellationToken>._)).Returns(pokemon);
            A.CallTo(() => _reviewRepository.GetByPokemonAsync(1, true, A<CancellationToken>._)).Returns(reviews);

            var result = await CreateController().DeletePokemon(1, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
            A.CallTo(() => _reviewRepository.RemoveRange(reviews)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _pokemonRepository.Remove(pokemon)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
