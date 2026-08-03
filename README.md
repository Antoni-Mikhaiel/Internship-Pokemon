# Internship-Pokemon

An ASP.NET Core 6 Web API over a small pokemon/review domain, used to work through four
topics end to end: **ASP.NET Core Identity**, **generic repository + unit of work**, the
**Result pattern**, and **global exception handling**.

## Running it

```bash
dotnet restore
dotnet run --project PokemonReviewApp
```

In `Development` the app migrates the SQLite database on start and seeds the `Admin` and
`User` roles, so a fresh clone works with no extra steps. Swagger UI is at `/swagger`, and
its **Authorize** button takes a token from `POST /api/account/login`.

Anywhere other than Development, apply migrations deliberately:

```bash
dotnet ef database update --project PokemonReviewApp
```

Seed the sample domain data with `dotnet run --project PokemonReviewApp seeddata`.

### Configuration

`Jwt:Key` is intentionally not in `appsettings.json`. Development reads a throwaway key
from `appsettings.Development.json`; every other environment must supply its own:

```bash
dotnet user-secrets set "Jwt:Key" "<at least 32 characters>" --project PokemonReviewApp
# or
export Jwt__Key="<at least 32 characters>"
```

Startup validates the `Jwt` section with data annotations, so a missing or too-short key
fails immediately rather than at the first login.

A first administrator is seeded when `Identity:AdminUser:UserName` and `:Password` are
set. `appsettings.Development.json` sets `admin` / `Admin123!` for local use.

## The four topics

### 1. ASP.NET Core Identity

`DataContext` derives from `IdentityDbContext<AppUser, IdentityRole, string>`, so users,
roles and claims live in the same database — and the same transaction — as the domain
tables. Registration is `AddIdentityCore` rather than `AddIdentity`: the full builder
installs cookie authentication as the default scheme, which would fight the JWT scheme.

| Piece | File |
| --- | --- |
| User entity and role constants | [Models/AppUser.cs](PokemonReviewApp/Models/AppUser.cs) |
| Register / login / me | [Controllers/AccountController.cs](PokemonReviewApp/Controllers/AccountController.cs) |
| Identity wrapped behind `Result` | [Services/AccountService.cs](PokemonReviewApp/Services/AccountService.cs) |
| JWT construction | [Services/TokenService.cs](PokemonReviewApp/Services/TokenService.cs) |
| Role and admin seeding | [Data/IdentitySeeder.cs](PokemonReviewApp/Data/IdentitySeeder.cs) |

Authorisation is layered per verb: `GET` is anonymous, `POST`/`PUT` need a token, and
`DELETE` needs the `Admin` role. Roles are referenced through `AppRoles` constants so a
typo in an `[Authorize]` attribute fails the build instead of silently locking everyone out.

Two deliberate choices worth noting:

- An unknown username and a wrong password return the **same** error, so the endpoint
  cannot be used to enumerate who has an account.
- `ClockSkew` is `TimeSpan.Zero`. The default five-minute grace period would keep expired
  tokens working past the lifetime the token itself advertises.

### 2. Generic repository + unit of work

[`IGenericRepository<TEntity>`](PokemonReviewApp/Interfaces/IGenericRepository.cs) holds the
CRUD half of every repository. The entity-specific interfaces inherit from it and add only
what is genuinely specific — `GetPokemonByCategoryAsync`, `GetRatingAsync`, and so on.

No repository method calls `SaveChanges`. That belongs to
[`IUnitOfWork`](PokemonReviewApp/Interfaces/IUnitOfWork.cs), which hands out repositories
sharing one `DataContext` and exposes a single `SaveChangesAsync`. Deleting a pokemon and
its reviews is therefore one commit rather than two that can half-fail — the previous code
saved them separately and could leave orphaned reviews behind.

Reads are untracked by default. Methods that feed a delete or update take `tracked: true`,
because handing a detached copy to `Remove` throws if the context already tracks that row.
Making that explicit turned a latent bug into a compile-time decision.

### 3. Result pattern

Expected failures travel back as values; exceptions stay for the genuinely unexpected.

- [`Error`](PokemonReviewApp/Common/Error.cs) — a code, a description, and an `ErrorType`
  the domain uses instead of an HTTP status. `ValidationError` carries several at once,
  which is what Identity produces when a password breaks more than one rule.
- [`Result` / `Result<T>`](PokemonReviewApp/Common/Result.cs) — the outcome, with guards
  that refuse to construct a success carrying an error or a failure carrying none.
- [`ResultExtensions`](PokemonReviewApp/Common/ResultExtensions.cs) — `Map`, `Ensure` and
  `Bind`, so a handler reads as one expression rather than a ladder of null checks.
- [`DomainErrors`](PokemonReviewApp/Common/DomainErrors.cs) — every failure in one place,
  so clients can switch on a stable code while the wording stays free to change.
- [`ApiControllerBase`](PokemonReviewApp/Controllers/ApiControllerBase.cs) — the single
  place an `ErrorType` becomes a status code.

```csharp
var result = Result
    .Create(
        await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken),
        DomainErrors.Category.NotFound(categoryId))
    .Map(_mapper.Map<CategoryDto>);

return ToActionResult(result);
```

Every failure comes back as RFC 7807 `problem+json` with the code in an `errorCode`
extension:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "A category named 'Electric' already exists.",
  "status": 409,
  "instance": "/api/category",
  "errorCode": "Category.DuplicateName"
}
```

| `ErrorType` | Status |
| --- | --- |
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |

### 4. Global exception handling

[`GlobalExceptionHandlingMiddleware`](PokemonReviewApp/Middleware/GlobalExceptionHandlingMiddleware.cs)
sits first in the pipeline and converts anything that escapes MVC into the same
`problem+json` shape, so the API never leaks a stack trace outside Development and never
answers with an unmapped status code. It is the net **under** the Result pattern, not a
replacement for it — what reaches it is by definition unforeseen, and is logged as such.

It also handles three cases that are easy to miss:

- A client that hangs up mid-request is logged at information level, not as an error —
  there is nobody left to answer and the socket is already gone.
- If the response has already started, the status line is fixed and `Clear()` would throw,
  so it logs and lets the connection tear down.
- Exception mapping is ordered so derived types sit above their bases
  (`DbUpdateConcurrencyException` before `DbUpdateException`).

## Tests

```bash
dotnet test
```

40 tests across four suites:

- `Common/ResultTests` — the Result and Error invariants.
- `Repository/GenericRepositoryTests` — shared CRUD behaviour, exercised through an entity
  with no hand-written repository.
- `Repository/UnitOfWorkTests` — repository caching, and that work staged across several
  repositories commits once.
- `Repository/PokemonRepositorySqliteTests` — aggregates and query translation against
  **real SQLite**, because the in-memory provider evaluates LINQ in .NET and will happily
  average a `decimal` that SQLite rejects. That divergence hid a live 500 on
  `GET /api/pokemon/{id}/rating` until the endpoint was exercised against a running server.
- `Controller/PokemonControllerTests` — status-code and commit-count behaviour, with the
  unit of work faked.
