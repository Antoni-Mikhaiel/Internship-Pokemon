using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Common;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Authorize]
    public class PokemonController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PokemonController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PokemonDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPokemons(CancellationToken cancellationToken)
        {
            var pokemon = await _unitOfWork.Pokemon.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<PokemonDto>>(pokemon));
        }

        [HttpGet("{pokeId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PokemonDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemon(int pokeId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Pokemon.GetByIdAsync(pokeId, cancellationToken),
                    DomainErrors.Pokemon.NotFound(pokeId))
                .Map(_mapper.Map<PokemonDto>);

            return ToActionResult(result);
        }

        [HttpGet("{pokeId:int}/rating")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonRating(int pokeId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Pokemon.ExistsAsync(pokeId, cancellationToken))
                return Problem(DomainErrors.Pokemon.NotFound(pokeId));

            return Ok(await _unitOfWork.Pokemon.GetRatingAsync(pokeId, cancellationToken));
        }

        [HttpPost]
        [ProducesResponseType(typeof(PokemonDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePokemon(
            [FromQuery] int ownerId,
            [FromQuery] int catId,
            [FromBody] PokemonDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Pokemon.GetByNameAsync(request.Name, cancellationToken) is not null)
                return Problem(DomainErrors.Pokemon.DuplicateName(request.Name));

            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
                return Problem(DomainErrors.Owner.NotFound(ownerId));

            if (!await _unitOfWork.Categories.ExistsAsync(catId, cancellationToken))
                return Problem(DomainErrors.Category.NotFound(catId));

            var pokemon = _mapper.Map<Pokemon>(request);

            // The pokemon and its two join rows are staged together and committed by the one
            // SaveChangesAsync below, so a failure cannot leave an ownerless pokemon behind.
            await _unitOfWork.Pokemon.AddWithOwnerAndCategoryAsync(pokemon, ownerId, catId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<PokemonDto>(pokemon);

            return CreatedAtAction(nameof(GetPokemon), new { pokeId = created.Id }, created);
        }

        [HttpPut("{pokeId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePokemon(
            int pokeId,
            [FromBody] PokemonDto request,
            CancellationToken cancellationToken)
        {
            if (pokeId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(pokeId)));

            var pokemon = await _unitOfWork.Pokemon.GetByIdAsync(pokeId, cancellationToken);

            if (pokemon is null)
                return Problem(DomainErrors.Pokemon.NotFound(pokeId));

            _mapper.Map(request, pokemon);

            _unitOfWork.Pokemon.Update(pokemon);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{pokeId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePokemon(int pokeId, CancellationToken cancellationToken)
        {
            var pokemon = await _unitOfWork.Pokemon.GetByIdAsync(pokeId, cancellationToken);

            if (pokemon is null)
                return Problem(DomainErrors.Pokemon.NotFound(pokeId));

            var reviews = await _unitOfWork.Reviews.GetByPokemonAsync(
                pokeId, tracked: true, cancellationToken);

            // Reviews and the pokemon go in one commit. The old code saved them separately,
            // which could leave orphaned reviews if the second save failed.
            _unitOfWork.Reviews.RemoveRange(reviews);
            _unitOfWork.Pokemon.Remove(pokemon);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
