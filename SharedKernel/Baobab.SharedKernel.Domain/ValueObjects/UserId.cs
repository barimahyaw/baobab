using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public class UserId(Ulid id) : ValueObject
{
    public Ulid Id { get; private set; } = id;

    public static Result Validate(Ulid id)
    {
        if (id == Ulid.Empty || id == default)
            return Result.Fail(Errors.UserErrors.UserIdEmpty);

        return Result.Success();
    }

    public static UserId Create(Ulid id)
        => new(id);

    public static implicit operator Ulid(UserId self) => self.Id;
    public static implicit operator UserId(Ulid id) => new(id);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;
    }
}
