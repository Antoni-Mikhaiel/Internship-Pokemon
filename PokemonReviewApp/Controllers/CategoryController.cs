using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Common;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    // Reading the catalogue is open to anyone; changing it needs a token, and deleting
    // needs an administrator. Each action opts out of this default explicitly.
    [Authorize]
    public class CategoryController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<CategoryDto>>(categories));
        }

        [HttpGet("{categoryId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int categoryId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken),
                    DomainErrors.Category.NotFound(categoryId))
                .Map(_mapper.Map<CategoryDto>);

            return ToActionResult(result);
        }

        [HttpGet("{categoryId:int}/pokemon")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PokemonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonByCategory(int categoryId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Categories.ExistsAsync(categoryId, cancellationToken))
                return Problem(DomainErrors.Category.NotFound(categoryId));

            var pokemon = await _unitOfWork.Categories.GetPokemonByCategoryAsync(categoryId, cancellationToken);

            return Ok(_mapper.Map<List<PokemonDto>>(pokemon));
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CategoryDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Categories.GetByNameAsync(request.Name, cancellationToken) is not null)
                return Problem(DomainErrors.Category.DuplicateName(request.Name));

            var category = _mapper.Map<Category>(request);

            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<CategoryDto>(category);

            return CreatedAtAction(nameof(GetCategory), new { categoryId = created.Id }, created);
        }

        [HttpPut("{categoryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(
            int categoryId,
            [FromBody] CategoryDto request,
            CancellationToken cancellationToken)
        {
            if (categoryId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(categoryId)));

            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);

            if (category is null)
                return Problem(DomainErrors.Category.NotFound(categoryId));

            // Map onto the tracked entity rather than attaching a fresh one, so columns the
            // DTO does not carry keep their current values instead of being nulled out.
            _mapper.Map(request, category);

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{categoryId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);

            if (category is null)
                return Problem(DomainErrors.Category.NotFound(categoryId));

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
