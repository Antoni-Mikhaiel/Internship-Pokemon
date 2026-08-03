using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace PokemonReviewApp.Middleware
{
    /// <summary>
    /// Catches everything that escapes the MVC pipeline and turns it into an
    /// RFC 7807 problem+json response, so the API never leaks a stack trace and
    /// never answers with an unmapped status code.
    /// </summary>
    /// <remarks>
    /// This is the net under the Result pattern, not a replacement for it. Expected
    /// failures — a missing category, a duplicate name — come back as a
    /// <see cref="Common.Result"/> and never reach here; what lands here is the
    /// genuinely unforeseen, and it is logged as such.
    /// </remarks>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            // The caller hung up mid-request. There is nobody left to answer, and the
            // socket is already gone, so writing a body would just throw again.
            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request {Method} {Path} was aborted by the client. TraceId: {TraceId}",
                    context.Request.Method, context.Request.Path, traceId);
                return;
            }

            var (statusCode, title) = MapToResponse(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method, context.Request.Path, traceId);
            }
            else
            {
                _logger.LogWarning(exception, "Request {Method} {Path} failed with {StatusCode}. TraceId: {TraceId}",
                    context.Request.Method, context.Request.Path, statusCode, traceId);
            }

            // Once bytes are on the wire the status line is fixed and Clear() would throw,
            // so all we can do is log and let the connection tear down.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "Response for TraceId {TraceId} had already started; the problem details body could not be written.",
                    traceId);
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            // Reuse the framework's factory so these responses carry the same
            // "type" links and traceId as the ones MVC produces on its own.
            var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                context,
                statusCode: statusCode,
                title: title,
                detail: _environment.IsDevelopment() ? exception.ToString() : null);

            problemDetails.Instance = context.Request.Path;

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }

        // Ordering matters: the compiler matches these top to bottom, so every derived type
        // has to sit above its base (DbUpdateConcurrencyException before DbUpdateException,
        // ArgumentNullException before ArgumentException).
        private static (int StatusCode, string Title) MapToResponse(Exception exception) => exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
            // A rejected or expired bearer token. The description is deliberately vague:
            // telling a caller exactly why validation failed helps an attacker more than it
            // helps a client.
            SecurityTokenException => (StatusCodes.Status401Unauthorized, "The access token is not valid."),
            ArgumentException => (StatusCodes.Status400BadRequest, "The request was not valid."),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "This operation is not implemented."),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "The record was modified by another request. Reload it and try again."),
            DbUpdateException => (StatusCodes.Status500InternalServerError,
                "The changes could not be saved to the database."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };
    }

    public static class GlobalExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Register first in the pipeline so it wraps every downstream component.
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
