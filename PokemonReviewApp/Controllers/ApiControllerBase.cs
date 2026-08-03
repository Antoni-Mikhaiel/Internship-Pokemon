using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Common;

namespace PokemonReviewApp.Controllers
{
    /// <summary>
    /// The single place where a <see cref="Result"/> becomes an HTTP response. Controllers
    /// derive from this so an <see cref="ErrorType"/> maps to the same status code and the
    /// same problem+json shape everywhere — including the shape the global exception
    /// handler produces for the failures nobody saw coming.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>200 with the value, or the mapped problem response.</summary>
        protected IActionResult ToActionResult<TValue>(Result<TValue> result) =>
            result.IsSuccess ? Ok(result.Value) : Problem(result.Error);

        /// <summary>204, or the mapped problem response.</summary>
        protected IActionResult ToNoContent(Result result) =>
            result.IsSuccess ? NoContent() : Problem(result.Error);

        /// <summary>
        /// Renders an <see cref="Error"/> as RFC 7807 problem+json. The error code travels in an
        /// <c>errorCode</c> extension so clients can branch on it without parsing prose.
        /// </summary>
        protected IActionResult Problem(Error error)
        {
            var statusCode = ToStatusCode(error.Type);

            if (error is ValidationError validationError)
            {
                // ModelState is the vehicle MVC already uses for field-level errors, so the
                // response looks identical to one produced by [ApiController] model binding.
                foreach (var item in validationError.Errors)
                    ModelState.AddModelError(item.Code, item.Description);

                return ValidationProblem(new ValidationProblemDetails(ModelState)
                {
                    Status = statusCode,
                    Title = error.Description,
                    Instance = HttpContext.Request.Path
                });
            }

            var problemDetails = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: statusCode,
                title: error.Description,
                instance: HttpContext.Request.Path);

            problemDetails.Extensions["errorCode"] = error.Code;

            return new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        private static int ToStatusCode(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
