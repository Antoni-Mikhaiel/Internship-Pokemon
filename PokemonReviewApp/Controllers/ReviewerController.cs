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
    public class ReviewerController : ApiControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReviewerController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ReviewerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviewers(CancellationToken cancellationToken)
        {
            var reviewers = await _unitOfWork.Reviewers.GetAllAsync(cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<ReviewerDto>>(reviewers));
        }

        [HttpGet("{reviewerId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReviewerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewer(int reviewerId, CancellationToken cancellationToken)
        {
            var result = Result
                .Create(
                    await _unitOfWork.Reviewers.GetWithReviewsAsync(reviewerId, cancellationToken),
                    DomainErrors.Reviewer.NotFound(reviewerId))
                .Map(_mapper.Map<ReviewerDto>);

            return ToActionResult(result);
        }

        [HttpGet("{reviewerId:int}/reviews")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewsByAReviewer(int reviewerId, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.Reviewers.ExistsAsync(reviewerId, cancellationToken))
                return Problem(DomainErrors.Reviewer.NotFound(reviewerId));

            var reviews = await _unitOfWork.Reviewers.GetReviewsByReviewerAsync(reviewerId, cancellationToken: cancellationToken);

            return Ok(_mapper.Map<List<ReviewDto>>(reviews));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ReviewerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReviewer(
            [FromBody] ReviewerDto request,
            CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Reviewers.GetByLastNameAsync(request.LastName, cancellationToken) is not null)
                return Problem(DomainErrors.Reviewer.DuplicateName(request.LastName));

            var reviewer = _mapper.Map<Reviewer>(request);

            await _unitOfWork.Reviewers.AddAsync(reviewer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = _mapper.Map<ReviewerDto>(reviewer);

            return CreatedAtAction(nameof(GetReviewer), new { reviewerId = created.Id }, created);
        }

        [HttpPut("{reviewerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReviewer(
            int reviewerId,
            [FromBody] ReviewerDto request,
            CancellationToken cancellationToken)
        {
            if (reviewerId != request.Id)
                return Problem(DomainErrors.General.IdMismatch(nameof(reviewerId)));

            var reviewer = await _unitOfWork.Reviewers.GetByIdAsync(reviewerId, cancellationToken);

            if (reviewer is null)
                return Problem(DomainErrors.Reviewer.NotFound(reviewerId));

            _mapper.Map(request, reviewer);

            _unitOfWork.Reviewers.Update(reviewer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{reviewerId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReviewer(int reviewerId, CancellationToken cancellationToken)
        {
            var reviewer = await _unitOfWork.Reviewers.GetByIdAsync(reviewerId, cancellationToken);

            if (reviewer is null)
                return Problem(DomainErrors.Reviewer.NotFound(reviewerId));

            _unitOfWork.Reviewers.Remove(reviewer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
