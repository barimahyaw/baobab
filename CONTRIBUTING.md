# Contributing to Baobab.SharedKernel

Thanks for considering a contribution — bug reports, documentation fixes, and
code contributions are all welcome.

## Quick Start

1. Fork the repository and create your branch from `master`
2. `dotnet restore` and `dotnet build Baobab.sln`
3. Make your change, following the coding standards below
4. Add or update tests where it makes sense
5. Open a pull request with a clear description of what changed and why

## Reporting Bugs

Before opening an issue:
- Search existing issues to avoid duplicates
- Confirm the bug reproduces on the latest `master`
- Include a minimal repro: what you did, what you expected, what happened,
  and your .NET/package versions

## Suggesting Features

Open an issue describing the problem you're trying to solve before writing
code for a large change — it's easier to agree on an approach up front than
to rework a finished PR. Small, focused improvements (a new value object, a
new specification helper) don't need this step.

## Coding Standards

- **Nullable reference types** are enabled; avoid `!` unless you've actually
  established non-null-ness.
- **Async methods** end in `Async`.
- **Clean Architecture dependency direction**: `Domain <- Application <-
  Persistence <- Infrastructure <- Presentation`. Don't add a reference that
  points the wrong way.
- **Result pattern, not exceptions, for business-logic failures.** Throw only
  for genuinely exceptional/programmer-error conditions.
- **IDs are `Guid`s created via `Guid.CreateVersion7()`**, not `Guid.NewGuid()`
  — this keeps them time-ordered/index-friendly. `Ulid` should not reappear
  in this codebase.

```csharp
// Good — Result pattern, domain event, Guid v7
public Result AddItem(ProductId productId, Money unitPrice, int quantity)
{
    if (Status != OrderStatus.Draft)
        return Result.Fail(Errors.OrderErrors.CannotModifyConfirmedOrder);

    var item = OrderItem.Create(Guid.CreateVersion7(), productId, unitPrice, quantity);
    _items.Add(item);

    RaiseDomainEvent(new OrderItemAddedDomainEvent(Id, productId, quantity));
    return Result.Success();
}
```

## Pull Request Checklist

- [ ] `dotnet build Baobab.sln` succeeds with no new warnings
- [ ] Existing tests still pass, and new tests cover new behavior
- [ ] Public API changes are reflected in the relevant `docs/` page
- [ ] CHANGELOG.md has an entry under `[Unreleased]`

## Code of Conduct

Be respectful. Disagree about code, not about people. Harassment of any kind
isn't tolerated — if something comes up, open an issue or, for anything
sensitive, use GitHub's private reporting (see [SECURITY.md](./SECURITY.md)
for the mechanism; the same channel works for conduct concerns).

## Thank You

Every contribution — a typo fix, a test, a new value object — makes this a
better foundation for the next project built on it.
