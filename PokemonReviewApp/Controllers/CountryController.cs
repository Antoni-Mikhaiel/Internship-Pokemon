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
    public class CountryController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CountryController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CountryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
        {
            var countries = await _unitOfWork.Countries.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<CountryDto>>(countries));
        }

        [HttpGet("{countryId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCountry(int countryId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken),
                    DomainErrors.Country.NotFound(countryId))
                .Map(_mapper.Map<CountryDto>);

            return ToActionResult(result);
        }

        [HttpGet("owners/{ownerId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCountryOfAnOwner(int ownerId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
                return Problem(DomainErrors.Owner.NotFound(ownerId));

            var result = Result
                .Create(
                    await _unitOfWork.Countries.GetByOwnerAsync(ownerId, cancellationToken),
                    DomainErrors.Country.NotFound(ownerId))
                .Map(_mapper.Map<CountryDto>);

            return ToActionResult(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCountry(
            [FromBody] CountryDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Countries.GetByNameAsync(request.Name, cancellationToken) is not null)
                return Problem(DomainErrors.Country.DuplicateName(request.Name));

            var country = _mapper.Map<Country>(request);

            await _unitOfWork.Countries.AddAsync(country, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<CountryDto>(country);

            return CreatedAtAction(nameof(GetCountry), new { countryId = created.Id }, created);
        }

        [HttpPut("{countryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCountry(
            int countryId,
            [FromBody] CountryDto request,
            CancellationToken cancellationToken)
        {
            if (countryId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(countryId)));

            var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);

            if (country is null)
                return Problem(DomainErrors.Country.NotFound(countryId));

            _mapper.Map(request, country);

            _unitOfWork.Countries.Update(country);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{countryId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCountry(int countryId, CancellationToken cancellationToken)
        {
            var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);

            if (country is null)
                return Problem(DomainErrors.Country.NotFound(countryId));

            _unitOfWork.Countries.Remove(country);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
