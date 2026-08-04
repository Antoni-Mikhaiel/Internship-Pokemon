using System;
using FluentAssertions;
using PokemonReviewApp.Common;
using Xunit;

namespace PokemonReviewApp.Tests.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_CarriesNoError()
        {
            var result = Result.Success();

            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().Be(Error.None);
        }

        [Fact]
        public void Failure_CarriesTheError()
        {
            var error = Error.NotFound("Pokemon.NotFound", "No pokemon with id 1 exists.");

            var result = Result.Failure<string>(error);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
            result.Error.Type.Should().Be(ErrorType.NotFound);
        }

        [Fact]
        public void Value_ThrowsOnAFailedResult()
        {
            var result = Result.Failure<string>(Error.Failure("Some.Code", "Something broke."));

            var accessValue = () => result.Value;

            accessValue.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Create_TurnsNullIntoTheSuppliedFailure()
        {
            var notFound = DomainErrors.Category.NotFound(7);

            Result.Create((string?)null, notFound).Error.Should().Be(notFound);
            Result.Create("present", notFound).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Create_FallsBackToNullValueWhenNoErrorIsGiven()
        {
            Result.Create((string?)null).Error.Should().Be(Error.NullValue);
        }

        [Fact]
        public void ImplicitConversion_WrapsAValueAsSuccess()
        {
            Result<int> result = 42;

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(42);
        }

        [Fact]
        public void Map_TransformsSuccessAndPassesFailureThrough()
        {
            Result.Success(21).Map(value => value * 2).Value.Should().Be(42);

            var error = DomainErrors.Pokemon.NotFound(3);
            var mapped = Result.Failure<int>(error).Map(value => value * 2);

            mapped.IsFailure.Should().BeTrue();
            mapped.Error.Should().Be(error);
        }

        [Fact]
        public void Ensure_FailsWhenThePredicateRejectsTheValue()
        {
            var error = Error.Validation("Rating.OutOfRange", "Rating must be between 1 and 10.");

            Result.Success(5).Ensure(rating => rating <= 10, error).IsSuccess.Should().BeTrue();
            Result.Success(50).Ensure(rating => rating <= 10, error).Error.Should().Be(error);
        }

        [Fact]
        public void ConstructingAnInconsistentResultThrows()
        {
            // Guards against the two states that would make a result lie about itself.
            var successWithError = () => Result.Failure(Error.None);
            successWithError.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ValidationError_KeepsEveryUnderlyingFailure()
        {
            var error = new ValidationError(new[]
            {
                Error.Validation("Identity.PasswordTooShort", "Passwords must be at least 8 characters."),
                Error.Validation("Identity.PasswordRequiresDigit", "Passwords must have at least one digit.")
            });

            error.Type.Should().Be(ErrorType.Validation);
            error.Errors.Should().HaveCount(2);
        }
    }
}
