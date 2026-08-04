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
    public class ReviewController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReviewController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviews(CancellationToken cancellationToken)
        {
            var reviews = await _unitOfWork.Reviews.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<ReviewDto>>(reviews));
        }

        [HttpGet("{reviewId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReview(int reviewId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken),
                    DomainErrors.Review.NotFound(reviewId))
                .Map(_mapper.Map<ReviewDto>);

            return ToActionResult(result);
        }

        [HttpGet("pokemon/{pokeId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewsForAPokemon(int pokeId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Pokemon.ExistsAsync(pokeId, cancellationToken))
                return Problem(DomainErrors.Pokemon.NotFound(pokeId));

            var reviews = await _unitOfWork.Reviews.GetByPokemonAsync(pokeId, cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<ReviewDto>>(reviews));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReview(
            [FromQuery] int reviewerId,
            [FromQuery] int pokeId,
            [FromBody] ReviewDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Reviews.GetByTitleAsync(request.Title, cancellationToken) is not null)
                return Problem(DomainErrors.Review.DuplicateTitle(request.Title));

            // Both must be tracked entities so EF wires up the foreign keys instead of
            // inserting duplicate pokemon and reviewer rows.
            var pokemon = await _unitOfWork.Pokemon.GetByIdAsync(pokeId, cancellationToken);

            if (pokemon is null)
                return Problem(DomainErrors.Pokemon.NotFound(pokeId));

            var reviewer = await _unitOfWork.Reviewers.GetByIdAsync(reviewerId, cancellationToken);

            if (reviewer is null)
                return Problem(DomainErrors.Reviewer.NotFound(reviewerId));

            var review = _mapper.Map<Review>(request);
            review.Pokemon = pokemon;
            review.Reviewer = reviewer;

            await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<ReviewDto>(review);

            return CreatedAtAction(nameof(GetReview), new { reviewId = created.Id }, created);
        }

        [HttpPut("{reviewId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReview(
            int reviewId,
            [FromBody] ReviewDto request,
            CancellationToken cancellationToken)
        {
            if (reviewId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(reviewId)));

            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);

            if (review is null)
                return Problem(DomainErrors.Review.NotFound(reviewId));

            _mapper.Map(request, review);

            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{reviewId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReview(int reviewId, CancellationToken cancellationToken)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);

            if (review is null)
                return Problem(DomainErrors.Review.NotFound(reviewId));

            _unitOfWork.Reviews.Remove(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("reviewer/{reviewerId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReviewsByReviewer(
            int reviewerId,
            CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Reviewers.ExistsAsync(reviewerId, cancellationToken))
                return Problem(DomainErrors.Reviewer.NotFound(reviewerId));

            var reviews = await _unitOfWork.Reviewers.GetReviewsByReviewerAsync(
                reviewerId, tracked: true, cancellationToken);

            _unitOfWork.Reviews.RemoveRange(reviews);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
