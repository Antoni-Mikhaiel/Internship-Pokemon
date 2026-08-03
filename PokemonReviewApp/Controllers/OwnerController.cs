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
    public class OwnerController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OwnerController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<OwnerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOwners(CancellationToken cancellationToken)
        {
            var owners = await _unitOfWork.Owners.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<OwnerDto>>(owners));
        }

        [HttpGet("{ownerId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OwnerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOwner(int ownerId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Owners.GetByIdAsync(ownerId, cancellationToken),
                    DomainErrors.Owner.NotFound(ownerId))
                .Map(_mapper.Map<OwnerDto>);

            return ToActionResult(result);
        }

        [HttpGet("{ownerId:int}/pokemon")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PokemonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonByOwner(int ownerId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
                return Problem(DomainErrors.Owner.NotFound(ownerId));

            var pokemon = await _unitOfWork.Owners.GetPokemonByOwnerAsync(ownerId, cancellationToken);

            return Ok(_mapper.Map<List<PokemonDto>>(pokemon));
        }

        [HttpPost]
        [ProducesResponseType(typeof(OwnerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateOwner(
            [FromQuery] int countryId,
            [FromBody] OwnerDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Owners.GetByLastNameAsync(request.LastName, cancellationToken) is not null)
                return Problem(DomainErrors.Owner.DuplicateName(request.LastName));

            // The country has to be a tracked entity, not the no-tracking copy the read
            // helpers return, or EF would try to insert a second row for it.
            var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);

            if (country is null)
                return Problem(DomainErrors.Country.NotFound(countryId));

            var owner = _mapper.Map<Owner>(request);
            owner.Country = country;

            await _unitOfWork.Owners.AddAsync(owner, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<OwnerDto>(owner);

            return CreatedAtAction(nameof(GetOwner), new { ownerId = created.Id }, created);
        }

        [HttpPut("{ownerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOwner(
            int ownerId,
            [FromBody] OwnerDto request,
            CancellationToken cancellationToken)
        {
            if (ownerId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(ownerId)));

            var owner = await _unitOfWork.Owners.GetByIdAsync(ownerId, cancellationToken);

            if (owner is null)
                return Problem(DomainErrors.Owner.NotFound(ownerId));

            _mapper.Map(request, owner);

            _unitOfWork.Owners.Update(owner);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{ownerId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOwner(int ownerId, CancellationToken cancellationToken)
        {
            var owner = await _unitOfWork.Owners.GetByIdAsync(ownerId, cancellationToken);

            if (owner is null)
                return Problem(DomainErrors.Owner.NotFound(ownerId));

            _unitOfWork.Owners.Remove(owner);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
